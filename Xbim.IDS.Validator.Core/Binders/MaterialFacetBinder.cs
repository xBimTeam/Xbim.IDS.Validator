using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Helpers;
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
            throw new NotImplementedException();
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
