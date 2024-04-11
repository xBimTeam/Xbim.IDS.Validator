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
    public class MaterialFacetBinder : FacetBinderBase<MaterialFacet>
    {
        private readonly ILogger<MaterialFacetBinder> logger;

        public MaterialFacetBinder(BinderContext context, ILogger<MaterialFacetBinder> logger) : base(context, logger)
        {
            this.logger = logger;
        }

        public override Expression BindSelectionExpression(Expression baseExpression, MaterialFacet facet)
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
                throw new InvalidOperationException($"IFC Material Facet '{facet?.Value}' is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the IIfcRelAssociatesMaterial
            
            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                expression = BindIfcExpressType(expression, Model.Metadata.GetExpressType(typeof(IIfcRelAssociatesMaterial)), false);
                return BindMaterialSelection(expression, facet);
            }

            throw new NotSupportedException("Selection of Materials must be the first expression in the graph");
        }


        public override Expression BindWhereExpression(Expression baseExpression, MaterialFacet facet)
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
                throw new InvalidOperationException($"IFC Material Facet '{facet?.Value}' is not valid");
            }


            var expression = baseExpression;

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                throw new NotSupportedException("Expected a selection expression before applying filters");
            }


            if (TypeHelper.IsCollection(expression.Type, out Type elementType))
            {
                // Apply the Classification filter
                if (typeof(IIfcObjectDefinition).IsAssignableFrom(elementType))
                {
                    // Objects and Types

                    expression = BindMaterialFilter(expression, facet);
                    return expression;
                }
                else
                {
                    // Not supported, return nothing

                    logger.LogWarning("Cannot filter by material on {collectionType} items", elementType.Name);
                    return BindNotFound(expression, elementType);

                }

            }

            throw new NotSupportedException("Cannot filter materials on this type " + elementType.Name);
        }

        public override void ValidateEntity(IPersistEntity item, MaterialFacet facet, Cardinality cardinality, IdsValidationResult result)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            var ctx = CreateValidationContext(cardinality, facet);

            var candidates = GetMaterialsValues(item);

            if (candidates.Any())
            {
                bool? success = null;
                if (facet.Value != null)
                {

                    foreach (var material in candidates)
                    {
                        var materialName = material;
                    
                        if (facet.Value.ExpectationIsSatisifedBy(materialName, ctx, logger, true))
                        {
                            result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.Value!, materialName, "Material matched", item));
                            success = true;
                        }
                    }
                }
                else
                {
                    if(cardinality == Cardinality.Prohibited)
                    {
                        result.Fail(ValidationMessage.Failure(ctx, fn => fn.Value!, null, "Material Prohibited", item));
                    }
                    else
                    {
                        result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.Value!, null, "Found a material", item));
                        success = true; // Found a material. Any will do
                    }
                }
                // If no matching value found after all the materials checked, mark as failed
                if (success == default)
                {
                    var materials = string.Join(",", candidates);
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.Value!, materials, "No materials matched", item));
                }
            }
            else
            {
                switch (cardinality)
                {
                    case Cardinality.Expected:
                        {
                            result.Fail(ValidationMessage.Failure(ctx, fn => fn.Value!, null, "No materials found", item));
                            break;
                        }

                    case Cardinality.Optional:
                    case Cardinality.Prohibited:
                        {
                            result.MarkSatisified(ValidationMessage.Failure(ctx, fn => fn.Value!, null, "No Material found", item));
                            break;
                        }
                }
                
            }
        }

        private IEnumerable<string> GetMaterialsValues(IPersistEntity item)
        {
            if(item is IIfcObjectDefinition obj)
            {
                var materials = IfcMaterialsExtensions.GetMaterialNames(obj.Material, logger);

                if (obj is IIfcObject o && o.IsTypedBy!.Any())
                {
                    // TODO: handle multiple Types
                    materials = materials.Union(GetMaterialsValues(o.IsTypedBy.First().RelatingType));
                }
                return materials;
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }

       
        private Expression BindMaterialSelection(Expression expression, MaterialFacet materialFacet)
        {
            if (materialFacet is null)
            {
                throw new ArgumentNullException(nameof(materialFacet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            if (materialFacet?.Value?.AcceptedValues?.Any() == false)
            {
                return expression;
            }


            var materialFacetExpr = Expression.Constant(materialFacet, typeof(MaterialFacet));

            // Expression we're building:
            // var psetRelAssociates = model.Instances.OfType<IIfcRelAssociatesMaterial>();
            // var entities = IfcExtensions.GetIfcObjectsUsingMaterials(psetRelAssociates, materialFacet);

            var propsMethod = ExpressionHelperMethods.EnumerableIfcMaterialSelector;

            return Expression.Call(null, propsMethod, new[] { expression, materialFacetExpr });
        }

        /// <summary>
        /// Selects the entities matching the material filter from a set of ObjectsDefinitions
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="facet"></param>
        /// <returns>The <see cref="Expression"/> with Classification filters applied</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private Expression BindMaterialFilter(Expression expression, MaterialFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }


            if (facet?.Value?.AcceptedValues?.Any() == false)
            {
                return expression;
            }


            var materialFacetExpr = Expression.Constant(facet, typeof(MaterialFacet));

            // Expression we're building:
            // var entities = model.Instances.OfType<IfcObjectDefinition>();
            // var filteredresult = entities.WhereAssociatedWithMaterial(e, facet));
            // or
            // var filteredresult =  IfcExtensions.WhereAssociatedWithMaterial(entities, facet);

            var propsMethod = ExpressionHelperMethods.EnumerableWhereAssociatedWithMaterial;

            return Expression.Call(null, propsMethod, new[] { expression, materialFacetExpr });

        }
    }
}
