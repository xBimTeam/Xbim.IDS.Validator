using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.UtilityResource;
using Xbim.InformationSpecifications;
using Xbim.InformationSpecifications.Helpers;

namespace Xbim.IDS.Validator.Core.Binders
{
    public class AttributeFacetBinder : FacetBinderBase<AttributeFacet>
    {
        public AttributeFacetBinder(IModel model) : base(model)
        {
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
        public override Expression BindFilterExpression(Expression baseExpression, AttributeFacet attrFacet)
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
            // When an Ifc Type facet has not yet been specified, find correct root type(s) for this AttributeName
            // using the lookup that XIDS provides
            if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
            {
                
                // TODO: Use correct IFC Schema based on Model
                var rootTypes = SchemaInfo.SchemaIfc4.GetAttributeClasses(attrFacet.AttributeName.SingleValue(), onlyTopClasses: true);

                expression = base.BindIfcExpressTypes(expression, rootTypes);

            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = Model.Metadata.ExpressType(collectionType);
            ValidateExpressType(expressType);

            expression = BindEqualsAttributeFilter(expression, expressType, attrFacet.AttributeName.SingleValue(), // TODO Check if we ever want to filter multiple Names
                attrFacet?.AttributeValue);
            return expression;
        }

        public override void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, AttributeFacet af)
        {
            if (af is null)
            {
                throw new ArgumentNullException(nameof(af));
            }

            var candidates = GetAttributes(item, af);

            foreach (var pair in candidates)
            {
                var attrName = pair.Key;
                var attrvalue = pair.Value;
                bool isPopulated = IsValueRelevant(attrvalue);
                // Name meets requirement if it has a value and is Required. Treat unknown logical as no value
                if (af.AttributeName.SatisfiesRequirement(requirement, attrName, logger) && (requirement.IsRequired() == isPopulated))
                {
                    result.Messages.Add(ValidationMessage.Success(af, fn => fn.AttributeName!, attrName, "Was populated", item));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(af, fn => fn.AttributeName!, attrName, "No attribute matched", item));
                }

                attrvalue = HandleBoolConventions(attrvalue);
                // Unpack Ifc Values
                if (attrvalue is IIfcValue v)
                {
                    attrvalue = v.Value;
                }
                if (af.AttributeValue != null)
                {
                    attrvalue = ApplyWorkarounds(attrvalue);
                    if (af.AttributeValue.SatisfiesRequirement(requirement, attrvalue, logger))
                        result.Messages.Add(ValidationMessage.Success(af, fn => fn.AttributeValue!, attrvalue, "Was populated", item));
                    else
                        result.Messages.Add(ValidationMessage.Failure(af, fn => fn.AttributeValue!, attrvalue, "No attribute value matched", item));
                }

            }
        }

        public IDictionary<string,object> GetAttributes(IPersistEntity entity, AttributeFacet facet)
        {
            var results = new Dictionary<string, object>();

            var expressType = Model.Metadata.ExpressType(entity);
            foreach(var constraint in facet.AttributeName.AcceptedValues)
            {
                switch(constraint)
                {
                    case ExactConstraint e:
                        var attrName = e.Value;
                        var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == attrName).Value;
                        if (propertyMeta == null)
                        {
                            results.Add(attrName, null);
                        }
                        else
                        {
                            var ifcAttributePropInfo = propertyMeta.PropertyInfo;
                            var value = ifcAttributePropInfo.GetValue(entity);
                            results.Add(attrName, value);
                        }
                        break;

                    case PatternConstraint e:
                        foreach(var prop in expressType.Properties.Values)
                        {
                            
                            if(e.IsSatisfiedBy(prop.Name, facet.AttributeName, true))
                            {
                                var value = prop.PropertyInfo.GetValue(entity);
                                results.Add(prop.Name, value);
                            }
                        }
                        break;

                    default:
                        throw new NotImplementedException(constraint.GetType().Name);
                }
            }
            

            return results;
        }


        internal static Expression BindEqualsAttributeFilter(Expression expression, ExpressType expressType,
            string ifcAttributeName, ValueConstraint constraint)
        {

            var propertyMeta = expressType.Properties.First(p => p.Value.Name == ifcAttributeName).Value;
            if (propertyMeta == null)
            {
                throw new InvalidOperationException($"Property '{ifcAttributeName} not found on '{expressType.Name}'");
            }
            if (propertyMeta.EnumerableType != null)
            {
                throw new NotSupportedException("Cannot filter on collection properties");
            }
            return BindEqualsAttributeFilter(expression, propertyMeta.PropertyInfo, constraint);
        }

        internal static Expression BindEqualsAttributeFilter(Expression expression,
            PropertyInfo ifcAttributePropInfo, ValueConstraint constraint)
        {
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            var constraints = constraint.AcceptedValues;

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            if (constraints.Any() == false)
            {
                return expression;
            }

            // IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            // build 'ent.AttributeName'
            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);

            var propType = ifcAttributePropInfo.PropertyType;
            var isNullWrapped = TypeHelper.IsNullable(propType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(propType) : propType;

            Expression querybody = Expression.Empty();

            bool applyOr = false;
            foreach (var ifcAttributeValue in constraints)
            {
                Expression rightExpr;

                switch (ifcAttributeValue)
                {
                    case ExactConstraint e:

                        string exactValue = e.Value;
                        // Get the Constant
                        rightExpr = BuildAttributeValueConstant(isNullWrapped, underlyingType, exactValue);
                        nameProperty = SetAttributeProperty(nameProperty, underlyingType);
                        // Binding Equals(x,y)
                        rightExpr = Expression.Equal(nameProperty, rightExpr);
                        break;

                    case PatternConstraint p:
                        // Build a query that builds an expression that delegates to XIDS's IsSatisfied regex method.
                        // model.Instances.OfType<IIfcWall>().Where(ent => patternconstraint.IsSatisfiedBy(w.Name.ToString(), <AttributeValue>, true, null));
                        //                                                 instance,         methodIn new[]{rightExpr, constraintExpr,   case, logger }
                        var isSatisfiedMethod = ExpressionHelperMethods.IdsValidationIsSatisifiedMethod;
                        // Get Property: entity.<attribute>
                        rightExpr = BuildAttributeValueRegexPredicate(nameProperty, isNullWrapped, underlyingType, p);
                        var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));
                        var caseInsensitive = Expression.Constant(true, typeof(bool));
                        var loggerExpr = Expression.Constant(null, typeof(ILogger));
                        var instanceExpr = Expression.Constant(p, typeof(PatternConstraint));
                        rightExpr = Expression.Call(instanceExpr, isSatisfiedMethod, new[] { rightExpr, constraintExpr, caseInsensitive, loggerExpr });

                        break;
                    case StructureConstraint s:
                    case RangeConstraint r:
                        throw new NotSupportedException(ifcAttributeValue.GetType().Name);

                    default:
                        throw new NotImplementedException(ifcAttributeValue.GetType().Name);
                }

                // Or the expressions on subsequent iterations.
                if (applyOr)
                {
                    querybody = Expression.Or(querybody, rightExpr);
                }
                else
                {
                    querybody = rightExpr;
                    applyOr = true;
                }
            }

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);

            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });

        }

        private static Expression BuildAttributeValueRegexPredicate(Expression nameProperty, bool isNullWrapped, Type underlyingType, PatternConstraint p)
        {
            Expression queryValue;

            if (TypeHelper.IsCollection(underlyingType))
            {
                throw new NotSupportedException("Collections not supported");
            }
            // Unpack simple objects for string comparisons

            else if (underlyingType == typeof(string))
            {
                queryValue = nameProperty;
            }
            else if (underlyingType == typeof(IfcLabel) ||
                underlyingType == typeof(IfcText) ||
                underlyingType == typeof(IfcGloballyUniqueId) ||
                // TODO: Other primitives
                underlyingType.IsEnum
                )
            {
                // Call ToString on these primitives. HACK to avoid handling Nullables but good enough for Regex. Should got to Value (object)
                queryValue = Expression.Call(nameProperty, nameof(Object.ToString), typeArguments: null, arguments: null);
            }
            else
            {
                throw new NotImplementedException($"Filtering on Ifc type {underlyingType.Name} not implemented");
            }


            return queryValue;
        }

        private static Expression SetAttributeProperty(Expression nameProperty, Type underlyingType)
        {
            if (underlyingType!.IsEnum)
            {
                // HACK: Use ToString rather than convert Predefined to correct Enum type.
                nameProperty = Expression.Call(nameProperty, nameof(Object.ToString), typeArguments: null, arguments: null);
            }

            return nameProperty;
        }

        private static Expression BuildAttributeValueConstant(bool isNullWrapped, Type underlyingType, string ifcAttributeValue)
        {
            Expression queryValue;
            if (TypeHelper.IsCollection(underlyingType))
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
                // And use ToString upstream
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

            return queryValue;
        }




    }
}
