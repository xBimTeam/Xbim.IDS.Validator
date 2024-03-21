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

namespace Xbim.IDS.Validator.Core.Binders
{
    public class IfcClassificationFacetBinder : FacetBinderBase<IfcClassificationFacet>
    {
        private readonly ILogger<IfcClassificationFacetBinder> logger;

        public IfcClassificationFacetBinder(BinderContext context, ILogger<IfcClassificationFacetBinder> logger) : base(context)
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

        public override void ValidateEntity(IPersistEntity item, IfcClassificationFacet facet, RequirementCardinalityOptions requirement, IdsValidationResult result)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            var ctx = CreateValidationContext(requirement, facet);

            var candidates = GetClassifications2(item);

            if (candidates.Any())
            {
                bool? success = false;
                foreach (var classification in candidates)
                {
                    if (IsMatchingClassification(classification, facet, ctx, result))
                    {
                        success = true;
                        break;
                    }
                }
                if (success == true)
                {
                    // Downgrade the Failures status as they were a false -ve if we later found a match
                    foreach (var message in result.Messages.Where(m => m.Status == ValidationStatus.Fail))
                    {
                        message.Status = ValidationStatus.Inconclusive;
                    }
                }
            }
            else
            {

                if (facet.ClassificationSystem != null)
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.ClassificationSystem!, null, "No classifications system matching", item));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Identification!, null, "No classifications matching", item));
                }
            }
        }


        private bool IsMatchingClassification(IIfcClassificationSelect classification, IfcClassificationFacet facet, ValidationContext<IfcClassificationFacet> ctx, IdsValidationResult result)
        {
            if (classification is null)
            {
                return false;
            }

            if (facet.Identification != null)
            {
                var identifications = classification.GetClassificationIdentifiers(logger);
                var isSatisfied = false;
                foreach (var id in identifications)
                {
                    if (facet.Identification.IsSatisfiedBy(id, true))
                    {
                        isSatisfied = true;
                        result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.Identification!, id, "Classification Identifier found", classification));
                        break;
                    }
                }
                if (!isSatisfied)
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Identification!, null, "No classifications matching", classification));
                    return false;
                }
            }

            if (facet.ClassificationSystem != null)
            {
                var system = classification.GetSystemName(logger);
                var isSatisfied = facet.ClassificationSystem.IsSatisfiedBy(system, true);
                if (isSatisfied)
                {
                    result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.ClassificationSystem!, system, "Classification System found", classification));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.ClassificationSystem!, system, "No classification system matching", classification));
                }
                return isSatisfied;
            }
            // else match any classification
            var message = ValidationMessage.Success(ctx, fn => fn.Identification!, null, "Classification found", classification);
            result.Messages.Add(message);
            return message.Status == ValidationStatus.Pass; // Accounts for prohibited as well as required
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


        private IEnumerable<IIfcClassificationSelect> GetClassifications2([NotNull] IPersistEntity item, bool isFirstPass = true)
        {
            if (item is IIfcObjectDefinition obj)
            {
                foreach (var cl in obj.HasAssociations.OfType<IIfcRelAssociatesClassification>().Select(r => r.RelatingClassification))
                {
                    yield return cl;
                }
                if (item is IIfcObject o)
                {
                    foreach (var type in o.IsTypedBy)
                    {
                        foreach (var cl in type.RelatingType.HasAssociations.OfType<IIfcRelAssociatesClassification>().Select(r => r.RelatingClassification))
                        {
                            yield return cl;
                        }
                    }
                }
            }
            else if (item is IIfcMaterialDefinition m)
            {
                // Edge-case where spec supports classification of materials
                foreach (var cl in m.HasExternalReferences.OfType<IIfcExternalReferenceRelationship>().Select(r => r.RelatingReference).OfType<IIfcClassificationReference>())
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
    }
}
