using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;
using Xbim.IO.Xml.BsConf;

namespace Xbim.IDS.Validator.Core
{
    public class IdsFacetBinder
    {
        private readonly IModel model;

        public IdsFacetBinder(IModel model)
        {
            this.model = model;
        }

        public Expression Bind(Expression baseExpression, IfcTypeFacet ifcFacet)
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
            string ifcTypeName = GetIfcTypeName(ifcFacet);

            var ifcTypeMetadata = model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
            expression = BindIfcType(ifcFacet, expression, ifcTypeMetadata);
            if(ifcFacet.PredefinedType != null)
                expression = BindPredefinedType(ifcFacet, expression, ifcTypeMetadata);
            return expression;
        }

        private Expression BindIfcType(IfcTypeFacet ifcFacet, Expression expression, ExpressType expressType)
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

            return BindEqualsAttribute(expression, expressType, ifcAttributePropInfo, ifcAttributeValue);
        }

        private static Expression BindEqualsAttribute(Expression expression, ExpressType expressType, PropertyInfo ifcAttributePropInfo, string ifcAttributeValue)
        {


            // IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(expressType.Type);


            // ent => ...
            ParameterExpression ifcTypeParam = Expression.Parameter(expressType.Type, "ent");

            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);

            var propType = ifcAttributePropInfo.PropertyType;
            var underlyingField = (Nullable.GetUnderlyingType(propType) ?? propType);

            // Unwrap simple navigation objects
            Expression queryValue;
            if (underlyingField == typeof(IfcLabel))
            {
                var val = new IfcLabel(ifcAttributeValue);
                queryValue = Expression.Constant(val, typeof(IfcLabel));

                // Check Value Field
                nameProperty = Expression.Property(nameProperty, nameof(IfcValue.Value));
            }
            else if (underlyingField.IsEnum)
            {
                // Use ToString rather than convert Predefined to correct Enum type.
                nameProperty = Expression.Call(nameProperty, "ToString", typeArguments: null, arguments: null);
                queryValue = Expression.Constant(ifcAttributeValue.ToUpperInvariant());
            }
            else
            {
                queryValue = Expression.Constant(ifcAttributeValue);
            }

            // Binding Equals
            var e1 = Expression.Equal(nameProperty, queryValue);

            var filterExpression = Expression.Lambda(e1, ifcTypeParam);

            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
        }

        private static string GetIfcTypeName(IfcTypeFacet ifcFacet)
        {
            return (ifcFacet.IfcType.AcceptedValues?.Single() as ExactConstraint).Value ?? "IfcObject";
        }

        private static string GetPredefinedType(IfcTypeFacet ifcFacet)
        {
            if (ifcFacet.PredefinedType?.AcceptedValues?.Any() != true)
            {
                return string.Empty;
            }
            return (ifcFacet.PredefinedType.AcceptedValues?.FirstOrDefault() as ExactConstraint).Value;
        }

    }
}
