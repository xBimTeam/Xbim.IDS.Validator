using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.UtilityResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    public class IdsFacetBinder
    {
        private readonly IModel model;

        public IdsFacetBinder(IModel model)
        {
            this.model = model;
        }

        public ExpressType GetExpressType(IfcTypeFacet ifcFacet)
        {
            string ifcTypeName = GetIfcTypeName(ifcFacet);
            return model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
        }

        /// <summary>
        /// Build initial expression for IFC Type
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="ifcFacet"></param>
        /// <param name="expressType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Expression Bind(Expression baseExpression, IfcTypeFacet ifcFacet, ExpressType expressType)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (ifcFacet is null)
            {
                throw new ArgumentNullException(nameof(ifcFacet));
            }

            if (!ifcFacet.IsValid())
            {
                throw new InvalidOperationException("IfcTypeFacet is not valid");
            }

            var expression = baseExpression;

            // Bind IfcType string to model.Instances.OfType(typeName);
            //string ifcTypeName = GetIfcTypeName(ifcFacet);


            // Exclude invalid schema items (including un-rooted resources like IfcLabel)
            if(expressType == null || expressType.Properties.Count == 0)
            {
                throw new InvalidOperationException($"Invalid IFC Type '{expressType?.Name}'");
            }
            expression = BindIfcType(expression, expressType);
            if(ifcFacet.PredefinedType != null)
                expression = BindPredefinedType(ifcFacet, expression, expressType);
            return expression;
        }

        public Expression Bind(Expression baseExpression, AttributeFacet attrFacet, ExpressType expressType)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (attrFacet is null)
            {
                throw new ArgumentNullException(nameof(attrFacet));
            }

            if (!attrFacet.IsValid())
            {
                // IsValid checks against a know list of all IFC Attributes
                throw new InvalidOperationException($"Attribute Facet '{attrFacet?.AttributeName}' is not valid");
            }

            var expression = baseExpression;

            // Exclude invalid schema items (including un-rooted resources like IfcLabel)
            if (expressType == null || expressType.Properties.Count == 0)
            {
                throw new InvalidOperationException($"Invalid IFC Type '{expressType?.Name}'");
            }

            expression = BindEqualsAttribute(expression, expressType, attrFacet.AttributeName.SingleValue(), attrFacet?.AttributeValue.SingleValue());
            return expression;
        }

        private Expression BindIfcType(Expression expression, ExpressType expressType)
        {
            var ofTypeMethod = ExpressionHelperMethods.EntityCollectionOfType;
            
            var entityTypeName = Expression.Constant(expressType.Name, typeof(string));
            var activate = Expression.Constant(true, typeof(bool));
            // call OfType("IfcWall", true)
            expression = Expression.Call(expression, ofTypeMethod, entityTypeName, activate);   // TODO: switch to Generic sig
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(expressType.Type), expression);

            return expression;
        }

       
        private Expression BindPredefinedType(IfcTypeFacet ifcFacet, Expression expression, ExpressType expressType)
        {
            var predefinedType = (ifcFacet?.PredefinedType?.AcceptedValues?.FirstOrDefault() as ExactConstraint)?.Value;
            if (string.IsNullOrEmpty(predefinedType)) return expression;

            var propertyMeta = expressType.Properties.First(p => p.Value.Name == "PredefinedType").Value;
            var ifcAttributePropInfo = propertyMeta.PropertyInfo;
            var ifcAttributeValue = GetPredefinedType(ifcFacet);
            // TODO: Check IfcObject.ObjectType when USERDEFINED - Or Expression

            return BindEqualsAttribute(expression, expressType, ifcAttributePropInfo, ifcAttributeValue);
        }


        private static Expression BindEqualsAttribute(Expression expression, ExpressType expressType,
            string ifcAttributeName, string ifcAttributeValue)
        {

            var propertyMeta = expressType.Properties.First(p => p.Value.Name == ifcAttributeName).Value;
            if(propertyMeta == null)
            {
                throw new InvalidOperationException($"Property '{ifcAttributeName} not found on '{expressType.Name}'");
            }
            if(propertyMeta.EnumerableType != null)
            {
                throw new NotSupportedException("Cannot filter on collection properties");
            }
            return BindEqualsAttribute(expression, expressType, propertyMeta.PropertyInfo, ifcAttributeValue);
        }

        private static Expression BindEqualsAttribute(Expression expression, ExpressType expressType, 
            PropertyInfo ifcAttributePropInfo, string ifcAttributeValue)
        {

            // IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(expressType.Type);

            // ent => ...
            ParameterExpression ifcTypeParam = Expression.Parameter(expressType.Type, "ent");

            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);

            var propType = ifcAttributePropInfo.PropertyType;
            var isNullWrapped = TypeHelper.IsNullable(propType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(propType) : propType;
            
            // Unwrap simple navigation objects
            Expression queryValue;
            if(TypeHelper.IsCollection(underlyingType))
            {
                throw new NotSupportedException("Collections not supported");
            }
            else if (underlyingType == typeof(IfcLabel))
            {
                var val = new IfcLabel(ifcAttributeValue);
                queryValue = Expression.Constant(val, typeof(IfcLabel));
            }
            else if (underlyingType == typeof(IfcText))
            {
                var val = new IfcText(ifcAttributeValue);
                queryValue = Expression.Constant(val, typeof(IfcText));
            }
            else if (underlyingType == typeof(IfcGloballyUniqueId))
            {
                var val = new IfcGloballyUniqueId(ifcAttributeValue);
                queryValue = Expression.Constant(val, typeof(IfcGloballyUniqueId));
            }
            else if (underlyingType.IsEnum)
            {
                // Use ToString rather than convert Predefined to correct Enum type.
                nameProperty = Expression.Call(nameProperty, "ToString", typeArguments: null, arguments: null);
                queryValue = Expression.Constant(ifcAttributeValue.ToUpperInvariant());
            }
            else if (underlyingType == typeof(string))
            {
                queryValue = Expression.Constant(ifcAttributeValue);
            }
            else
            {
                throw new NotImplementedException($"Filtering on Ifc type {underlyingType.Name} not implemented");
            }
            if (isNullWrapped && !TypeHelper.IsNullable(queryValue.Type))
            {
                queryValue = Expression.Convert(queryValue, TypeHelper.ToNullable(queryValue.Type));
            }

            // Binding Equals
            var equalityExpression = Expression.Equal(nameProperty, queryValue);

            var filterExpression = Expression.Lambda(equalityExpression, ifcTypeParam);

            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
        }

        private static string GetIfcTypeName(IfcTypeFacet ifcFacet)
        {
            return ifcFacet.IfcType.SingleValue() ?? "IfcObject";
        }

        private static string GetPredefinedType(IfcTypeFacet ifcFacet)
        {
            if (ifcFacet.PredefinedType?.AcceptedValues?.Any() != true)
            {
                return string.Empty;
            }
            return ifcFacet.PredefinedType.SingleValue();
        }

    }
}
