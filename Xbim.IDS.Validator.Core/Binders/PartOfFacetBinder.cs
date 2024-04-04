using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Binders
{
    public class PartOfFacetBinder : FacetBinderBase<PartOfFacet>
    {
        private readonly ILogger<PartOfFacetBinder> logger;

        public PartOfFacetBinder(BinderContext binderContext, ILogger<PartOfFacetBinder> logger) : base(binderContext, logger)
        {
            this.logger = logger;
        }

        public override Expression BindSelectionExpression(Expression baseExpression, PartOfFacet facet)
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
                throw new InvalidOperationException($"PartOf Facet '{facet?.EntityRelation}' {facet?.EntityType} is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the EntityRelation Type defined by the facet
            
            var expressType = Model.Metadata.ExpressType(facet.EntityRelation.ToUpperInvariant());
            if(expressType == null)
            {
                logger.LogWarning("Unexpected EntityRelation: {ifcTypes} for schema {ifcSchema}", expressType, Model.SchemaVersion);
                throw new InvalidOperationException($"Invalid EntityRelation '{expressType}' for {Model.SchemaVersion}");
            }

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                expression = BindIfcExpressType(expression, expressType, false);
                return BindPartOfSelection(expression, facet);
            }

            throw new NotSupportedException("PartOf Selection must be the first expression in the graph");
        }

        public override Expression BindWhereExpression(Expression baseExpression, PartOfFacet facet)
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
                throw new InvalidOperationException($"PartOf Facet '{facet?.EntityRelation}' {facet?.EntityType} is not valid");
            }


            var expression = baseExpression;

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                throw new NotSupportedException("Expected a selection expression before applying filters");
            }

            // Handle expressions where already bound to entities.
            if (TypeHelper.IsCollection(expression.Type, out Type elementType))
            {
                // Apply the PartOf filter
                if (typeof(IIfcObjectDefinition).IsAssignableFrom(elementType))
                {
                    // Objects and Types classified by HasAssociations

                    expression = BindPartOfFilter(expression, facet);
                    return expression;
                }
                else
                {
                    // Not supported, return nothing

                    logger.LogWarning("Cannot filter by PartOf Relationship on {collectionType} items", elementType.Name);
                    return BindNotFound(expression, elementType);

                }

            }

            throw new NotSupportedException("Cannot filter classifications on this type " + elementType.Name);

        }

        public override void ValidateEntity(IPersistEntity item, PartOfFacet facet, Cardinality cardinality, IdsValidationResult result)
        {
            
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            var ctx = CreateValidationContext(cardinality, facet);

            var candidates = GetParts(item, facet);

            if (candidates.Any())
            {

                foreach (var part in candidates)
                {
                    var partType = part.GetType().Name;
                    if(ctx.ExpectationMode != Cardinality.Prohibited)
                    {
                        result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.EntityType!, partType, "Part found", part));
                    }
                    else
                    {
                        result.Fail(ValidationMessage.Failure(ctx, fn => fn.EntityType!, partType, "Expected no match", item));
                    }
           
                }
            }
            else
            {
                if (ctx.ExpectationMode != Cardinality.Prohibited)
                {
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.EntityType!, null, "No parts matching", item));
                }
            }
        }


        private IEnumerable<IIfcObjectDefinition> GetParts(IPersistEntity item, PartOfFacet facet)
        {
            if (item is IIfcObjectDefinition obj)
            {
                // TODO: handle prohibited. 
                var parts = IfcRelationsExtensions.GetPartsForEntity(obj, facet);

                //if (obj is IIfcObject o && o.IsTypedBy!.Any())
                //{
                //    materials = materials.Union(GetMaterials(o.IsTypedBy.First().RelatingType, materialFacet));
                //}
                return parts;
            }
            else
            {
                return Enumerable.Empty<IIfcObjectDefinition>();
            }
        }

        private Expression BindPartOfSelection(Expression expression, PartOfFacet partOfFacet)
        {
            if (partOfFacet is null)
            {
                throw new ArgumentNullException(nameof(partOfFacet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            //if (partOfFacet?.Value?.AcceptedValues?.Any() == false)
            //{
            //    return expression;
            //}


            var partOfExpr = Expression.Constant(partOfFacet, typeof(PartOfFacet));

            // Expression we're building:
            // var relationships = model.Instances.OfType<{IfcRel.....}>();
            // var entities = IfcExtensions.GetRelatedIfcObjects(relationships, partOfFacet);

            var propsMethod = ExpressionHelperMethods.EnumerableIfcPartofRelatedMethod;

            return Expression.Call(null, propsMethod, new[] { expression, partOfExpr });
        }

        /// <summary>
        /// Selects the entities matching the PartOf filter from a set of ObjectsDefinitions
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="facet"></param>
        /// <returns>The <see cref="Expression"/> with PartOf filters applied</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private Expression BindPartOfFilter(Expression expression, PartOfFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }



            var facetExpr = Expression.Constant(facet, typeof(PartOfFacet));

            // Expression we're building:
            // var entities = model.Instances.OfType<IfcObjectDefinition>();
            // var filteredresult =  IfcExtensions.WhereHasPartOfRelationship(entities, facet);

            var propsMethod = ExpressionHelperMethods.EnumerableWhereObjectPartOfMethod;

            return Expression.Call(null, propsMethod, new[] { expression, facetExpr });

        }
    }
}
