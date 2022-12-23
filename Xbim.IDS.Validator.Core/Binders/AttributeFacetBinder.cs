using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
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
        public override Expression BindSelectionExpression(Expression baseExpression, AttributeFacet attrFacet)
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
            
            if(attrFacet.AttributeName.IsSingleExact(out var attributeName))
            {
                // When an Ifc Type facet has not yet been specified, find correct root type(s) for this AttributeName
                // using the lookup that XIDS provides

                if (expression.Type.IsInterface && expression.Type.IsAssignableTo(typeof(IEntityCollection)))
                {
                    string[] rootTypes;
                    if (IsIfc2x3Model())
                    {
                        rootTypes = SchemaInfo.SchemaIfc2x3.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                    }
                    else
                    {
                        rootTypes = SchemaInfo.SchemaIfc4.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                    }

                    expression = base.BindIfcExpressTypes(expression, rootTypes);

                    
                }
                var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
                var expressType = Model.Metadata.ExpressType(collectionType);
                if(!ExpressTypeIsValid(expressType, collectionType?.Name))
                {
                    throw new InvalidOperationException($"Invalid IFC Type '{expression.Type.Name}'");
                }

                expression = BindAttributeSelection(expression, expressType, (string)attributeName,
                    attrFacet?.AttributeValue);
                return expression;

            }
            else
            {
                // Not sure why we'd want to pick attributes with a regex, range, or even an enum?
                throw new NotSupportedException("Complex AttributeName constraints are not supported");
            }
            
        }

        public override Expression BindWhereExpression(Expression baseExpression, AttributeFacet attrFacet)
        {
            // We can use use straight forward selection as we're not traversing any relationships
            return BindSelectionExpression(baseExpression, attrFacet);
        }

        public override void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, AttributeFacet af)
        {
            if (af is null)
            {
                throw new ArgumentNullException(nameof(af));
            }
            var ctx = CreateValidationContext(requirement, af);

            var candidates = GetAttributes(item, af);

            foreach (var pair in candidates)
            {
                var attrName = pair.Key;
                var attrvalue = pair.Value;
                if(IsIfc2x3Model() && attrvalue is Xbim.Ifc2x3.MeasureResource.IfcValue ifc2x3Value)
                {
                    attrvalue = ifc2x3Value.ToIfc4();
                }
                bool isPopulated = IsValueRelevant(attrvalue);
                // Name meets requirement if it has a value and is Required. Treat unknown logical as no value
                if (isPopulated)
                {
                    result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.AttributeName!, attrName, "Was populated", item));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.AttributeName!, attrName, "No attribute matched", item));
                }

                attrvalue = HandleBoolConventions(attrvalue);
                // Unpack Ifc Values
                if (attrvalue is IIfcValue v)
                {
                    attrvalue = v.Value;
                }
                if (af.AttributeValue != null)
                {
                    attrvalue = ApplyWorkarounds(attrvalue, af.AttributeValue);
                    if (IsTypeAppropriateForConstraint(af.AttributeValue, attrvalue) && af.AttributeValue.IsSatisfiedBy(attrvalue, logger))
                        result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.AttributeValue!, attrvalue, "Was populated", item));
                    else
                        result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.AttributeValue!, attrvalue, "No attribute value matched", item));
                }

            }
        }

        private IDictionary<string,object?> GetAttributes(IPersistEntity entity, AttributeFacet facet)
        {
            var results = new Dictionary<string, object?>();

            if (facet.AttributeName?.AcceptedValues?.Any() != true)
                return results;

            var expressType = Model.Metadata.ExpressType(entity);

            if (facet.AttributeName.IsSingleExact(out string? attrName))
            {
                // Optimise for the typical scenario where one Attribute name is specified exactly
                
                var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == attrName).Value;
                if (propertyMeta == null)
                {
                    results.Add(attrName, default);
                }
                else
                {
                    var ifcAttributePropInfo = propertyMeta.PropertyInfo;
                    var value = ifcAttributePropInfo.GetValue(entity);
                    results.Add(attrName, value);
                }
            }
            else
            {
                // It's an enum, Regex, Range or Structure
                foreach (var prop in expressType.Properties)
                {
                    if (facet?.AttributeName?.IsSatisfiedBy(prop.Value.Name, true) == true)
                    {
                        var value = prop.Value.PropertyInfo.GetValue(entity);
                        if (!(value == null && IsEnum(facet.AttributeName)))   // TODO: should really only filter out enum choices?
                        {
                            results.Add(prop.Value.Name, value);
                        }
                    }
                }
            }

            return results;
        }

        private static bool IsEnum(ValueConstraint constraint)
        {
            return constraint.AcceptedValues.Count(av => av is ExactConstraint) > 1;
        }

        private static Expression BindAttributeSelection(Expression expression, ExpressType expressType,
            string ifcAttributeName, ValueConstraint constraint)
        {

            var propertyMeta = expressType.Properties.FirstOrDefault(p => p.Value.Name == ifcAttributeName).Value;
            if (propertyMeta == null)
            {
                throw new InvalidOperationException($"Property '{ifcAttributeName}' not found on '{expressType.Name}'");
            }
            if (propertyMeta.EnumerableType != null)
            {
                throw new NotSupportedException("Cannot filter on collection properties");
            }
            return BindAttributeSelection(expression, propertyMeta.PropertyInfo, constraint);
        }


        internal static Expression BindAttributeSelection(Expression expression,
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

            // Build IEnumerable<TEntity>().Where(t => Helpers.SatisfiesConstraint(constraint, t.[AttributeName], type))

            // build IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            // build 'ent.AttributeName'
            Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttributePropInfo);
            var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));

            // build params, & unwrap Type
            //var propType = ifcAttributePropInfo.PropertyType;
            
   
            var valueExpr = Expression.Convert(nameProperty, typeof(object));


            Expression querybody = Expression.Call(null, ExpressionHelperMethods.IdsSatisifiesConstraintMethod, constraintExpr, valueExpr );

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);

            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
        }

    

    }
}
