using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.ProductExtension;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{
    public class MaterialFacetBinder : FacetBinderBase<MaterialFacet>
    {
        public MaterialFacetBinder(IModel model) : base(model)
        {
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
            
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {
                expression = BindIfcExpressType(expression, Model.Metadata.ExpressType(nameof(IfcRelAssociatesMaterial).ToUpperInvariant()));
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

            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {
                throw new NotSupportedException("Expected a selection expression before applying filters");
            }


            if (TypeHelper.IsCollection(expression.Type, out Type elementType))
            {
                // Apply the Classification filter
                if (elementType.IsAssignableTo(typeof(IIfcObjectDefinition)))
                {
                    // Objects and Types classified by HasAssociations

                    expression = BindMaterialFilter(expression, facet);
                    return expression;
                }
                else
                {
                    // Not supported, return nothing

                    // TODO: log
                    return BindNotFound(expression, elementType);

                }

            }

            throw new NotSupportedException("Cannot filter materials on this type " + elementType.Name);
        }

        public override void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, MaterialFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            var ctx = CreateValidationContext(requirement, facet);

            var candidates = GetMaterials(item, facet);

            if (candidates.Any())
            {

                foreach (var material in candidates)
                {
                    var materialName = material.Name;
                    bool isPopulated = IsValueRelevant(materialName);
                    // Name meets requirement if it has a value and is Required.
                    if (isPopulated)
                    {
                        result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.Value!, materialName, "Material found", material));
                    }
                    else
                    {
                        result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Value!, materialName, "No matching material found", material));
                    }
                }
            }
            else
            {
                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Value!, null, "No materials matching", item));
            }
        }

        private IEnumerable<IIfcMaterial> GetMaterials(IPersistEntity item, MaterialFacet materialFacet)
        {
            if(item is IIfcObjectDefinition obj)
            {
                var materials = IfcExtensions.GetMaterialsForEntity(obj, materialFacet);

                if (obj is IIfcObject o && o.IsTypedBy!.Any())
                {
                    materials = materials.Union(GetMaterials(o.IsTypedBy.First().RelatingType, materialFacet));
                }
                return materials;
            }
            else
            {
                return Enumerable.Empty<IIfcMaterial>();
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
