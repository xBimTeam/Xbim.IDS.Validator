using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
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
                var materials =  GetMaterials(materialFacet, obj);

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

        private IEnumerable<IIfcMaterial> GetMaterials(MaterialFacet materialFacet, IIfcObjectDefinition obj)
        {
            if (obj.Material is IIfcMaterial material && MaterialMatches(material, materialFacet)) return new[] { material };
            if (obj.Material is IIfcMaterialList list) return list.Materials.Where(m => MaterialMatches(m, materialFacet));
            
            if (obj.Material is IIfcMaterialLayerSet layerSet) return 
                    layerSet.MaterialLayers.Select(ml => ml.Material).Where(m => MaterialMatches(m, materialFacet))
                    .Union(layerSet.MaterialLayers.Where(ml => MaterialMatches(ml, materialFacet)).Select(l => l.Material));
            if (obj.Material is IIfcMaterialLayerSetUsage layerusage) return layerusage.ForLayerSet.MaterialLayers.Select(ml => ml.Material).Where(m => MaterialMatches(m, materialFacet));
            
            if (obj.Material is IIfcMaterialProfile profile && MaterialMatches(profile.Material, materialFacet)) return new[] { profile.Material };
            if (obj.Material is IIfcMaterialProfileSet profileSet) return
                    profileSet.MaterialProfiles.Where(m => MaterialMatches(m, materialFacet)).Select(mc => mc.Material)
                    .Union(profileSet.MaterialProfiles.Select(mc => mc.Material).Where(m => MaterialMatches(m, materialFacet)));
            
            if (obj.Material is IIfcMaterialConstituent constituent && MaterialMatches(constituent, materialFacet)) return new[] { constituent.Material };
            if (obj.Material is IIfcMaterialConstituentSet constituentSet) return 
                    constituentSet.MaterialConstituents.Where(m => MaterialMatches(m, materialFacet)).Select(mc => mc.Material)
                    .Union(constituentSet.MaterialConstituents.Select(mc => mc.Material).Where(m => MaterialMatches(m, materialFacet)));

            return Enumerable.Empty<IIfcMaterial>();
        }

        private bool MaterialMatches(IIfcMaterial material, MaterialFacet facet)
        {
            return facet.Value?.IsSatisfiedBy(material.Name.Value, true) == true || facet.Value?.IsSatisfiedBy(material.Category?.Value, true) == true;
        }

        private bool MaterialMatches(IIfcMaterialConstituent constituent, MaterialFacet facet)
        {
            return facet.Value?.IsSatisfiedBy(constituent.Name?.Value, true) == true || facet.Value?.IsSatisfiedBy(constituent.Category?.Value, true) == true;
        }

        private bool MaterialMatches(IIfcMaterialLayer layer, MaterialFacet facet)
        {
            return facet.Value?.IsSatisfiedBy(layer.Name?.Value, true) == true || facet.Value?.IsSatisfiedBy(layer.Category?.Value, true) == true;
        }

        private bool MaterialMatches(IIfcMaterialProfile profile, MaterialFacet facet)
        {
            return facet.Value?.IsSatisfiedBy(profile.Name?.Value, true) == true || facet.Value?.IsSatisfiedBy(profile.Category?.Value, true) == true;
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
