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
using Xbim.Ifc4.Kernel;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{
    public class IfcClassificationFacetBinder : FacetBinderBase<IfcClassificationFacet>
    {
        public IfcClassificationFacetBinder(BinderContext context) : base(context.Model)
        {
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
                expression = BindIfcExpressType(expression, Model.Metadata.ExpressType(nameof(IfcRelAssociatesClassification).ToUpperInvariant()));
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

                    // TODO: log
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

        public override void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, IfcClassificationFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            var ctx = CreateValidationContext(requirement, facet);

            var candidates = GetClassifications(item, facet);

            if (candidates.Any())
            {

                foreach (var classification in candidates)
                {
                    IIfcValue? classificationName = default;
                    if(classification is IIfcClassification c)
                    {
                        classificationName = c.Name;

                        result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.ClassificationSystem!, classificationName, "Classification System found", classification));
                    }
                    else if(classification is IIfcClassificationReference cr)
                    {
                        classificationName = cr.Identification ?? default;
                        result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.Identification!, classificationName, "Classification found", classification));
                    }
                    else
                    {
                        result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Identification!, "", "No matching classification found", classification));
                    }

                }
            }
            else
            {
                if(facet.Identification != null)
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Identification!, null, "No classifications matching", item));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.ClassificationSystem!, null, "No classifications system matching", item));
                }
            }
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

        private IEnumerable<IIfcClassificationSelect> GetClassifications([NotNull]IPersistEntity item, IfcClassificationFacet facet, bool isFirstPass = true)
        {
            if (item is IIfcObjectDefinition obj)
            {
                // Classifications override per system. 
                // We don't want to filter by Identification om first pass. Instance classifications override Types, so an non-matching instance value should trump a matching type value
                var systemFacet = new IfcClassificationFacet()
                {
                    ClassificationSystem = facet.ClassificationSystem,
                    IncludeSubClasses = facet.IncludeSubClasses
                };

                var filterFacet = (isFirstPass && item is IIfcObject) ? systemFacet : facet;
                
                
                var classifications = obj.HasAssociations.OfType<IIfcRelAssociatesClassification>().GetClassificationReferences(filterFacet);

               
                if (obj is IIfcObject o && o.IsTypedBy!.Any())
                {
                    // Fall back to type
                    var systems = classifications.Select(c => GetSystem(c)).Where(s=> s != null).Distinct();
                    var typeClassifications = GetClassifications(o.IsTypedBy.First().RelatingType, facet, isFirstPass: false)
                        .Where(tc => systems?.Contains(GetSystem(tc)) != true); // Except where we already have one from thay system
                    classifications = classifications.Union(typeClassifications);
                }
              

                // re-apply the Value criteria skipped earlier
                if(isFirstPass && facet.Identification?.HasAnyAcceptedValue() == true)
                {
                    classifications = classifications.Where(c => c is IIfcClassification ||
                        c is IIfcClassificationReference r && r.HasMatchingIdentificationAncestor(facet));
                }


                return classifications;
            }
            else if(item is IIfcMaterialDefinition m)
            {
                // Edge-case where spec supports classification of materials
                return m.HasExternalReferences.OfType<IIfcExternalReferenceRelationship>().Select(e => e.RelatingReference).OfType<IIfcClassificationReference>()
                    .Where(cr => facet.Identification?.IsSatisfiedBy(cr.Identification?.Value, true)== true);
            }
            else
            {
                return Enumerable.Empty<IIfcClassificationReference>();
            }
        }

        private IIfcClassification? GetSystem(IIfcClassificationSelect classification)
        {
            do
            {
                if (classification is IIfcClassificationReference r)
                {
                    var parent = r.ReferencedSource;
                    if (parent is IIfcClassification pc)
                        classification = pc;
                    else if (parent is IIfcClassificationReference prc)
                        classification = prc;
                    else
                        break;
                }
                else if (classification is IIfcClassification cl)
                {
                    return cl;
                }
                else
                {
                    return default;
                }
            } while (true);

            return default;

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
