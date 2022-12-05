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

        public override Expression BindFilterExpression(Expression baseExpression, MaterialFacet facet)
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
            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = Model.Metadata.ExpressType(collectionType);
            ValidateExpressType(expressType);

            expression = BindEqualMaterialFilter(expression, facet);
            return expression;
        }

        public override void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, MaterialFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }

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
                        result.Messages.Add(ValidationMessage.Success(facet, fn => fn.Value!, materialName, "Material found", material));
                    }
                    else
                    {
                        result.Messages.Add(ValidationMessage.Failure(facet, fn => fn.Value!, materialName, "No matching material found", material));
                    }
                }
            }
            else
            {
                result.Messages.Add(ValidationMessage.Failure(facet, fn => fn.Value!, null, "No materials matching", item));
            }
        }

        private IEnumerable<IIfcMaterial> GetMaterials(IPersistEntity item, MaterialFacet materialFacet)
        {
            if(item is IIfcObjectDefinition obj)
            {

                
                IEnumerable<IIfcMaterial> materials = obj.HasAssociations.OfType<IIfcRelAssociatesMaterial>().Select(r => r.RelatingMaterial).OfType<IIfcMaterial>().Where(m => materialFacet?.Value?.IsSatisfiedBy(m.Name, true) == true);
                IEnumerable<IIfcMaterial> materials2 = obj.HasAssociations.OfType<IIfcRelAssociatesMaterial>().Select(r => r.RelatingMaterial).OfType<IIfcMaterialList>().SelectMany(l => l.Materials.Where(m => materialFacet?.Value?.IsSatisfiedBy(m.Name, true) == true));
                IEnumerable<IIfcMaterial> materials3 = obj.HasAssociations.OfType<IIfcRelAssociatesMaterial>().Select(r => r.RelatingMaterial).OfType<IIfcMaterialLayerSetUsage>().SelectMany(ls => ls.ForLayerSet.MaterialLayers.Select(ml => ml.Material).Where(m => materialFacet?.Value?.IsSatisfiedBy(m.Name, true) == true));

                materials = materials.Union(materials2).Union(materials3);


                return materials;
            }
            else
            {
                return Enumerable.Empty<IIfcMaterial>();
            }
        }

        private Expression BindEqualMaterialFilter(Expression expression, MaterialFacet materialFacet)
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
    }
}
