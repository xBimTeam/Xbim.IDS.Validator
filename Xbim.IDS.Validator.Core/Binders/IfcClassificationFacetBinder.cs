using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Binders
{
    public class IfcClassificationFacetBinder : FacetBinderBase<IfcClassificationFacet>
    {
        private readonly ILogger<IfcClassificationFacetBinder> logger;

        public IfcClassificationFacetBinder(ILogger<IfcClassificationFacetBinder> logger) : base(logger)
        {
            this.logger = logger;
        }

        public override Expression BindSelectionExpression(Expression baseExpression, IfcClassificationFacet facet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            if (!facet.IsValid())
            {
                throw new InvalidOperationException($"IFC Classification Facet '{facet?.ClassificationSystem}' is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the IIfcRelAssociatesClassification

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                expression = BindIfcExpressType(expression, Model.Metadata.GetExpressType(typeof(IIfcRelAssociatesClassification)), false);
                // Apply the Classification filter
                expression = BindClassificationSelection(expression, facet);
                return expression;
            }

            throw new NotSupportedException("Selection of Classifications must be the first expression in the graph");
            
        }

        public override Expression BindWhereExpression(Expression baseExpression, IfcClassificationFacet facet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

            if (!facet.IsValid())
            {
                throw new InvalidOperationException($"IFC Classification Facet '{facet?.ClassificationSystem}' is not valid");
            }


            var expression = baseExpression;

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                throw new NotSupportedException("Expected a selection expression before applying filters");
            }

            // Handle expressions where already bound to entities.
            if (TypeHelper.IsCollection(expression.Type, out Type elementType))
            {
                // Apply the Classification filter
                if (typeof(IIfcObjectDefinition).IsAssignableFrom(elementType))
                {
                    // Objects and Types classified by HasAssociations
                    
                    expression = BindClassificationFilter(expression, facet);
                    return expression;
                }
                else
                {
                    // Not supported, return nothing

                    logger.LogWarning("Cannot filter by classification on {collectionType} items", elementType.Name);
                    return BindNotFound(expression, elementType);

                }

            }

            throw new NotSupportedException("Cannot filter classifications on this type " + elementType.Name);

        }



        private static Expression BindSelectManyClassifications(ref Expression expression, Type elementType, Type selectReturnType, string propertyName)
        {
            
            // var x = Model.Instances.OfType("IFCFURNITURE", true).Cast<IIfcFurniture>().SelectMany(o => o.HasAssociations).OfType<IIfcRelAssociatesClassification>();
            // We're building this expression
            //  IEnumerable<IIfcObjectDefinition>   .SelectMany(o => o.HasAssociations).OfType<IfcRelAssociatesClassification>()

            var selectManyMethod = ExpressionHelperMethods.EnumerableSelectManyGeneric.MakeGenericMethod(elementType, selectReturnType);

            // build lambda for Selector: ent => 
            ParameterExpression selectManyParam = Expression.Parameter(elementType, "ent");

            // build 'ent.HasAssociations'
            PropertyInfo? associatesPropInfo = elementType.GetProperty(propertyName);
            var propertyExpression = Expression.Property(selectManyParam, associatesPropInfo);

            // build (ent=> ent.HasAssociations) Lambda
            var selectManyLambda = Expression.Lambda(propertyExpression, selectManyParam);

            // Select<T>() method
            expression = Expression.Call(null, selectManyMethod, expression, selectManyLambda);

            // call .OfType<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableOfTypeGeneric.MakeGenericMethod(typeof(IIfcRelAssociatesClassification)), expression);

            return expression;
        }

        public override void ValidateEntity(IPersistEntity item, IfcClassificationFacet facet, Cardinality cardinality, IdsValidationResult result)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            var ctx = CreateValidationContext(cardinality, facet);

            // Logic is we locate all unique systems and check the classificationreference value (and ancestors) belonging to the system on the instance first, and in the abscence of any
            // then fall back to the type's classificationreference values on the system (and ancestors).

            var systems = GetClassificationSystems(item).Distinct().ToList();
            var entityIsClassified = systems.Any();
            foreach(var system in systems)
            {
                var classResult = SystemClassificationsSatisfies(item, system, facet, result, ctx);
                if (classResult.HasFlag(ClassificationSatisfiedBy.Identifier))
                {
                    // Successfully matched on Instance or Type
                    return;
                }
            }

            // Identify which parameter was provided for messages
            Expression<Func<IfcClassificationFacet, object>> applicableParam = (IfcClassificationFacet f) => facet.ClassificationSystem!;
            if (facet.Identification != null || facet.ClassificationSystem is null)
                applicableParam = (IfcClassificationFacet f) => facet.Identification!;

            if (entityIsClassified)
            {
                // Classified but not satifying requirement
                // - Fail is applicable to all cardinalities since ClassificationsSatisfy accounts for Prohibited
                result.Fail(ValidationMessage.Failure(ctx, applicableParam, null, "No classifications matching", item));
            }
            else
            {
                // Not classified
                switch (cardinality)
                {
                    case Cardinality.Expected:
                        {
                            // No classifications, or none matching
                            result.Fail(ValidationMessage.Failure(ctx, applicableParam, null, "No classifications found", item));
                            break;
                        }

                    case Cardinality.Optional:
                    case Cardinality.Prohibited:
                        {
                            result.MarkSatisified(ValidationMessage.Failure(ctx, applicableParam, null, "No classification found", item));
                            break;
                        }
                }
            }
            
            
        }

        private ClassificationSatisfiedBy SystemClassificationsSatisfies(IPersistEntity entity, IIfcClassification system, IfcClassificationFacet facet, IdsValidationResult result, ValidationContext<IfcClassificationFacet> ctx)
        {
            // Look for a Positive match of the classification in this system. We can't indicate failure until higher level, since other systems may satisfy the requirement
            var matched = ClassificationSatisfiedBy.Nothing;
            if (!facet.ClassificationSystem.IsNullOrEmpty())
            {
                var systemName = system.Name.Value?.ToString();
                if(facet.ClassificationSystem.ExpectationIsSatisifedBy(systemName, ctx, logger, true) == true)
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.ClassificationSystem!, systemName, "Classification System matched", system));
                    matched |= ClassificationSatisfiedBy.System;
                    if (facet.Identification.IsNullOrEmpty())
                    {
                        matched |= ClassificationSatisfiedBy.Identifier;
                    }
                }
                else
                {
                    return matched; // Early exit - System is not relevant
                }
               
            }
            if (!facet.Identification.IsNullOrEmpty())
            {
                // Check instance first
                IEnumerable<IIfcClassificationSelect> matches = GetClassifications(entity, DataSource.Instance, system).ToList();
                matched = CheckIdentifiers(facet, result, ctx, matched, matches);
                if (matches.Any())
                    return matched;
                // Else nothing on the instance - try again with the type
                matches = GetClassifications(entity, DataSource.Type, system);
                matched = CheckIdentifiers(facet, result, ctx, matched, matches);
            }

            if(facet.ClassificationSystem.IsNullOrEmpty() && facet.Identification.IsNullOrEmpty())
            {
                // No constraints - technically invalid IDS.
                result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.ClassificationSystem!, null, "Item classified", system));
                matched |= ClassificationSatisfiedBy.All;
            }

            return matched;
        }

        private ClassificationSatisfiedBy CheckIdentifiers(IfcClassificationFacet facet, IdsValidationResult result, ValidationContext<IfcClassificationFacet> ctx, ClassificationSatisfiedBy matched, IEnumerable<IIfcClassificationSelect> matches)
        {
            foreach (var match in matches)
            {
                var id = match.GetClassificationIdentifiers(logger)
                    .FirstOrDefault(id => facet.Identification.ExpectationIsSatisifedBy(id, ctx, logger, true));
                if (id != null)
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.Identification!, id, "Classification Identifier matched", match));
                    matched |= ClassificationSatisfiedBy.Identifier;
                }
            }

            return matched;
        }


        /// <summary>
        /// Selects the entities matching the classification filter from a set of IfcRelAssociatesClassifications
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="facet"></param>
        /// <returns>The <see cref="Expression"/> with Classification filters applied</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private Expression BindClassificationSelection(Expression expression, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }


            if (facet?.ClassificationSystem?.AcceptedValues?.Any() == false && facet?.Identification?.AcceptedValues?.Any() == false)
            {
                return expression;
            }


            var classificationFacetExpr = Expression.Constant(facet, typeof(IfcClassificationFacet));

            // Expression we're building:
            // var psetRelAssociates = model.Instances.OfType<IfcRelAssociatesClassification>();
            // var entities = IfcExtensions.GetIfcObjectsAssociatedWithClassification(psetRelAssociates, facet);

            var propsMethod = ExpressionHelperMethods.EnumerableIfcClassificationSelector;

            return Expression.Call(null, propsMethod, new[] { expression, classificationFacetExpr });


        }

        /// <summary>
        /// Gets all classification systems associated with this entity
        /// </summary>
        /// <remarks>May return duplicates due to type overrides or multiple associated classifications</remarks>
        /// <param name="item"></param>
        /// <returns></returns>
        private IEnumerable<IIfcClassification> GetClassificationSystems([NotNull] IPersistEntity item)
        {
            if (item is IIfcObjectDefinition obj)
            {
                foreach (var cl in obj.HasAssociations.OfType<IIfcRelAssociatesClassification>().Select(r => r.RelatingClassification))
                {
                    if(cl != null)
                        yield return cl.GetSystem();
                }
                if (item is IIfcObject o)
                {
                    foreach (var type in o.IsTypedBy)
                    {
                        foreach (var cl in type.RelatingType.HasAssociations.OfType<IIfcRelAssociatesClassification>().Select(r => r.RelatingClassification))
                        {
                            if (cl != null)
                                yield return cl.GetSystem();
                        }
                    }
                }
            }
            else if (item is IIfcMaterialDefinition m)
            {

                // Edge-case where spec supports classification of materials
                foreach (var cl in m.HasExternalReferences.OfType<IIfcExternalReferenceRelationship>().Select(r => r.RelatingReference).OfType<IIfcClassificationReference>())
                {
                    if (cl != null)
                        yield return cl.GetSystem();
                }

            }
            else
            {
                yield break;
            }
        }


        private IEnumerable<IIfcClassificationSelect> GetClassifications([NotNull] IPersistEntity item, DataSource dataSource, IIfcClassification system)
        {
            if (item is IIfcObjectDefinition obj)
            {
                if (dataSource.HasFlag(DataSource.Instance))
                {
                    var classifications = obj.HasAssociations.OfType<IIfcRelAssociatesClassification>().Select(r => r.RelatingClassification)
                        .Where(c => c.GetSystem() == system);
                    foreach (var cl in classifications)
                    {
                        yield return cl;
                    }
                }
                if (dataSource.HasFlag(DataSource.Type) && item is IIfcObject o)
                {
                    foreach (var type in o.IsTypedBy)
                    {
                        var classifications = type.RelatingType.HasAssociations.OfType<IIfcRelAssociatesClassification>().Select(r => r.RelatingClassification)
                            .Where(c => c.GetSystem() == system);
                        foreach (var cl in classifications)
                        {
                            yield return cl;
                        }
                    }
                }
            }
            else if (dataSource.HasFlag(DataSource.Instance) && item is IIfcMaterialDefinition material)
            {
                // Edge-case where spec supports classification of materials
                var classifications = material.HasExternalReferences.OfType<IIfcExternalReferenceRelationship>().Select(r => r.RelatingReference).OfType<IIfcClassificationReference>()
                    .Where(c => c.GetSystem() == system);
                foreach (var cl in classifications)
                {
                    yield return cl;
                }
            }
            else
            {
                yield break;
            }
        }

        /// <summary>
        /// Selects the entities matching the classification filter from a set of ObjectsDefinitions
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="facet"></param>
        /// <returns>The <see cref="Expression"/> with Classification filters applied</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private Expression BindClassificationFilter(Expression expression, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }


            if (facet?.ClassificationSystem?.AcceptedValues?.Any() == false && facet?.Identification?.AcceptedValues?.Any() == false)
            {
                return expression;
            }


            var classificationFacetExpr = Expression.Constant(facet, typeof(IfcClassificationFacet));

            // Expression we're building:
            // var entities = model.Instances.OfType<IfcObjectDefinition>();
            // var filteredresult = entities.WhereIsAssociatedWithClassification(e, facet));
            // or
            // var filteredresult =  IfcExtensions.WhereIsAssociatedWithClassification(entities, facet);

            var propsMethod = ExpressionHelperMethods.EnumerableWhereAssociatedWithClassification;

            return Expression.Call(null, propsMethod, new[] { expression, classificationFacetExpr });

        }

        [Flags]
        private enum DataSource 
        {

            /// <summary>
            /// Located on Instance
            /// </summary>
            Instance = 1,
            /// <summary>
            /// Located on Type
            /// </summary>
            Type = 2,
            /// <summary>
            /// Located on instance or Type
            /// </summary>
            All = Instance | Type
        }

        [Flags]
        private enum ClassificationSatisfiedBy
        {
            /// <summary>
            /// No match
            /// </summary>
            Nothing = 0,

            /// <summary>
            ///  Matched system only
            /// </summary>
            System = 1,
            /// <summary>
            /// Matched identifier only
            /// </summary>
            Identifier = 2,
            /// <summary>
            /// Matched both
            /// </summary>
            All = System | Identifier
        }
    }
}
