using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.UtilityResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.InformationSpecifications.Helpers;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Class to dynamically bind IDS Facets to IModel 
    /// <see cref="IQueryable{T}"/> and <see cref="IEnumerable{T}"/> <see cref="Expression"/>s
    /// enabling late-bound querying and filtering of Entities
    /// </summary>
    public class IdsFacetBinder
    {
        private readonly IModel model;

        public IdsFacetBinder(IModel model)
        {
            this.model = model;
        }

        private ExpressType GetExpressType(IfcTypeFacet ifcFacet)
        {
            string ifcTypeName = GetIfcTypeName(ifcFacet);
            return model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
        }

        private IEnumerable<ExpressType> GetExpressTypes(IfcTypeFacet ifcFacet)
        {
            if(ifcFacet?.IfcType?.AcceptedValues?.Any() == false)
            {
                yield break;
            }
            foreach(var ifcTypeConstraint in ifcFacet!.IfcType!.AcceptedValues ?? default)
            {
                switch (ifcTypeConstraint)
                {
                    case ExactConstraint e:
                        string ifcTypeName = e.Value;
                        yield return model.Metadata.ExpressType(ifcTypeName.ToUpperInvariant());
                        break;
                    case RangeConstraint r:
                    case PatternConstraint p:
                    case StructureConstraint s:

                    default:
                        throw new NotImplementedException(ifcTypeConstraint.GetType().Name);
                }
            }
            
        }

        /// <summary>
        /// Binds an <see cref="IFacet"/> to an Expression bound to filter on IModel.Instances
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Expression BindFilters(Expression baseExpression, IFacet facet)
        {
            switch (facet)
            {
                case IfcTypeFacet f:
                    return BindFilterExpression(baseExpression, f);

                case AttributeFacet af:
                    return BindFilterExpression(baseExpression, af);

                case IfcPropertyFacet pf:
                    // TODO:
                    return baseExpression;

                case IfcClassificationFacet af:
                    // TODO: 
                    return baseExpression;

                case DocumentFacet df:
                    // TODO: 
                    return baseExpression;

                case IfcRelationFacet rf:
                    // TODO: 
                    return baseExpression;

                case PartOfFacet pf:
                    // TODO: 
                    return baseExpression;

                case MaterialFacet mf:
                    // TODO: 
                    return baseExpression;

                default:
                    throw new NotImplementedException($"Facet not implemented: '{facet.GetType().Name}'");
            }
        }

        /// <summary>
        /// Builds root expression for an IFC Type
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="ifcFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Expression BindFilterExpression(Expression baseExpression, IfcTypeFacet ifcFacet)
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

            var expressTypes = GetExpressTypes(ifcFacet);
            ValidateExpressTypes(expressTypes);

            var expression = baseExpression;
            bool doConcat = false;
            foreach(var expressType in expressTypes)
            {
                var leftExpr = baseExpression;
                leftExpr = BindIfcType(leftExpr, expressType);
                if (ifcFacet.PredefinedType != null)
                    leftExpr = BindPredefinedTypeFilter(ifcFacet, leftExpr, expressType);


                // Union to main expression.
                if(doConcat)
                {
                    expression = BindConcat(expression, leftExpr);
                }
                else
                {
                    expression = leftExpr;
                    doConcat = true;
                }
            }
                
            return expression;
            

            
        }

        private Expression BindConcat(Expression expression, Expression leftExpr)
        {
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(typeof(IIfcRoot)), expression);

            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableConcatGeneric.MakeGenericMethod(typeof(IIfcRoot)), expression, leftExpr);
            return expression;
        }

        /// <summary>
        /// Binds an IFC attribute filter to an expression, where Attributes are built in IFC schema fields
        /// </summary>
        /// <remarks>e.g Where(p=> p.GlobalId == "someGuid")</remarks>
        /// <param name="baseExpression"></param>
        /// <param name="attrFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Expression BindFilterExpression(Expression baseExpression, AttributeFacet attrFacet)
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
            // When an Ifc Type has not yet been specified, find correct root type for this Attribute
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {

                // TODO: Use correct IFC Schema
                var rootTypes = SchemaInfo.SchemaIfc4.GetAttributeClasses(attrFacet.AttributeName.SingleValue(), onlyTopClasses: true);

                IfcTypeFacet ifcFacet = new IfcTypeFacet
                {
                    IfcType = new ValueConstraint(NetTypeName.String)
                };
                foreach (var root in rootTypes)
                {
                    ifcFacet.IfcType.AcceptedValues?.Add(new ExactConstraint(root));
                }
                expression = BindFilterExpression(expression, ifcFacet);
            }
            
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = model.Metadata.ExpressType(collectionType);
            ValidateExpressType(expressType);

            expression = BindEqualsAttributeFilter(expression, expressType, attrFacet.AttributeName.SingleValue(), attrFacet?.AttributeValue.SingleValue());
            return expression;
        }

        private Expression BindIfcType(Expression expression, ExpressType expressType)
        {
            var ofTypeMethod = ExpressionHelperMethods.EntityCollectionOfType;
            
            var entityTypeName = Expression.Constant(expressType.Name, typeof(string));
            var activate = Expression.Constant(true, typeof(bool));
            // call .OfType("IfcWall", true)
            expression = Expression.Call(expression, ofTypeMethod, entityTypeName, activate);   // TODO: switch to Generic sig

            // TODO: Is this required?
            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(expressType.Type), expression);

            return expression;
        }

        private static void ValidateExpressType(ExpressType expressType)
        {
            // Exclude invalid schema items (including un-rooted entity types like IfcLabel)
            if (expressType == null || expressType.Properties.Count == 0)
            {
                throw new InvalidOperationException($"Invalid IFC Type '{expressType?.Name}'");
            }
        }

        private static void ValidateExpressTypes(IEnumerable<ExpressType> expressTypes)
        {
            foreach(var expressType in expressTypes)
            {
                ValidateExpressType(expressType);
            }
        }

        private Expression BindPredefinedTypeFilter(IfcTypeFacet ifcFacet, Expression expression, ExpressType expressType)
        {
            var predefinedType = (ifcFacet?.PredefinedType?.AcceptedValues?.FirstOrDefault() as ExactConstraint)?.Value;
            if (string.IsNullOrEmpty(predefinedType)) return expression;

            var propertyMeta = expressType.Properties.First(p => p.Value.Name == "PredefinedType").Value;
            var ifcAttributePropInfo = propertyMeta.PropertyInfo;
            var ifcAttributeValue = GetPredefinedType(ifcFacet);

            // TODO: Check IfcObject.ObjectType when USERDEFINED - Or Expression

            return BindEqualsAttributeFilter(expression, ifcAttributePropInfo, ifcAttributeValue);
        }


        private static Expression BindEqualsAttributeFilter(Expression expression, ExpressType expressType,
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
            return BindEqualsAttributeFilter(expression, propertyMeta.PropertyInfo, ifcAttributeValue);
        }

        private static Expression BindEqualsAttributeFilter(Expression expression,
            PropertyInfo ifcAttributePropInfo, string ifcAttributeValue)
        {

            

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            // IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // ent => ...
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);

            var propType = ifcAttributePropInfo.PropertyType;
            var isNullWrapped = TypeHelper.IsNullable(propType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(propType) : propType;
            
            Expression queryValue;

            if(TypeHelper.IsCollection(underlyingType))
            {
                throw new NotSupportedException("Collections not supported");
            }
            // Wrap simple navigation objects to use built-in equality operators
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
            // TODO: Other primitives
            else if (underlyingType.IsEnum)
            {
                // HACK: Use ToString rather than convert Predefined to correct Enum type.
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
            // Wrap when comparing to Nullable
            if (isNullWrapped && !TypeHelper.IsNullable(queryValue.Type))
            {
                queryValue = Expression.Convert(queryValue, TypeHelper.ToNullable(queryValue.Type));
            }

            // Binding Equals(x,y)
            var equalityExpression = Expression.Equal(nameProperty, queryValue);

            // Create Lambda expression for filter predicate (Func<T,bool>)
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

        // For Selections on instances we don't need to use expressions. When filtering we will
        /// <summary>
        /// Gets a specific property for an entity, matching a psetName and property name
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public IIfcValue GetProperty(int entityLabel, string psetName, string propName)
        {
            var entity = model.Instances[entityLabel];

            IIfcPropertySingleValue? psetValue;
            if (entity is IIfcTypeObject type)
            {
                psetValue = type.HasPropertySets.OfType<IIfcPropertySet>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    .FirstOrDefault();
            }
            else if(entity is IIfcObject)
            {
                psetValue = model.Instances.OfType<IIfcRelDefinesByProperties>()
                    .Where(r => r.RelatedObjects.Any(o => o.EntityLabel == entityLabel))
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcPropertySet)p.RelatingPropertyDefinition)
                        .HasProperties.Where(ps => ps.Name == propName)
                        .OfType<IIfcPropertySingleValue>())
                    .FirstOrDefault();
            }
            else
            {
                return null;
            }

            return psetValue?.NominalValue;

        }
    }
}
