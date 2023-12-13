using IdsLib.IfcSchema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4x3;  // To provide ToIfc4() extension method
using Xbim.InformationSpecifications;


namespace Xbim.IDS.Validator.Core.Binders
{
    public class AttributeFacetBinder : FacetBinderBase<AttributeFacet>
    {
        private readonly ILogger<AttributeFacetBinder> logger;

        public AttributeFacetBinder(BinderContext context, ILogger<AttributeFacetBinder> logger) : base(context, logger)
        {
            this.logger = logger;
        }


        /// <summary>
        /// Binds an IFC attribute filter to an expression, where Attributes are built-in IFC schema fields
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
                // IsValid checks against a known list of all IFC Attributes
                throw new InvalidOperationException($"Attribute Facet '{attrFacet?.AttributeName}' is not valid");
            }

            var expression = baseExpression;

            if (attrFacet.AttributeName.IsSingleExact(out var attributeName))
            {
                // When an Ifc Type facet has not yet been specified, find correct IFC type(s) for this AttributeName
                // using the lookup that IDSLib provides

                // Test for raw Model.Instances:
                if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
                {
                    IEnumerable<string> rootTypes;
                    if (IsIfc2x3Model())
                    {
                        rootTypes = SchemaInfo.SchemaIfc2x3.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                    }
                    else if(IsIfc4x3Model())
                    {
                        rootTypes = SchemaInfo.SchemaIfc4x3.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                    }
                    else
                    {
                        rootTypes = SchemaInfo.SchemaIfc4.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                    }
                    

                    return BindIfcTypeForAttributes(expression, rootTypes, attrFacet, (string)attributeName);


                }
                else
                {
                    // We know the Collection type, so can bind the Attribute predicate as long as it is a valid IFC type
                    // i.e. Apply straight forward predicate to the expression.

                    var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
                    var expressType = Model.Metadata.ExpressType(collectionType);
                    if (!ExpressTypeIsValid(expressType))
                    {
                        throw new InvalidOperationException($"Invalid IFC Type '{expression.Type.Name}'");
                    }

                    expression = BindAttributeSelection(expression, expressType, (string)attributeName,
                        attrFacet?.AttributeValue);
                    return expression;
                }
                

            }
            else
            {
                // TODO: Should support Enum. E.g. Where Name or Description = 'Foo'
                // Not sure why we'd want to pick attributes with a regex, range, or even an enum?
                throw new NotImplementedException("Complex AttributeName constraints are not supported");
            }

        }

        /// <summary>
        /// Selects the relevant types and applies the appropriate Attribute predicate to each before concatenating the resuls 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="rootTypes"></param>
        /// <param name="attrFacet"></param>
        /// <param name="attributeName"></param>
        /// <returns>An expression applying the IfcType filters with relevant Attribute predicate, cast to the highest common type</returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal Expression BindIfcTypeForAttributes(Expression expression, IEnumerable<string> rootTypes, AttributeFacet attrFacet, string attributeName)
        {
            IEnumerable<ExpressType> expressTypes = base.GetExpressTypes(rootTypes);
            if (!ExpressTypesAreValid(expressTypes))
            {
                var types = string.Join(',', rootTypes);
                throw new InvalidOperationException($"Invalid IFC Types '{types}'");
            }

            var baseExpression = expression;
            bool doConcat = false;
            foreach (var expressType in expressTypes)
            {

                var rightExpr = BindIfcExpressType(baseExpression, expressType, true);

                rightExpr = BindAttributeSelection(rightExpr, expressType, (string)attributeName,
                    attrFacet?.AttributeValue);


                // Concat to main expression.
                if (doConcat)
                {
                    expression = BindConcat(expression, rightExpr);
                }
                else
                {
                    expression = rightExpr;
                    doConcat = true;
                }
            }

            return expression;
        }

        public override Expression BindWhereExpression(Expression baseExpression, AttributeFacet attrFacet)
        {
            // We can use use straight forward selection as we're not traversing any relationships
            return BindSelectionExpression(baseExpression, attrFacet);
        }

        
        public override void ValidateEntity(IPersistEntity item, AttributeFacet af, RequirementCardinalityOptions requirement, IdsValidationResult result)
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
                if (IsIfc2x3Model() && attrvalue is Xbim.Ifc2x3.MeasureResource.IfcValue ifc2x3Value)
                {
                    attrvalue = ifc2x3Value.ToIfc4();
                }
                else if (IsIfc4x3Model() && attrvalue is Xbim.Ifc4x3.MeasureResource.IfcValue ifc4x3Value)
                {
                    attrvalue = ifc4x3Value.ToIfc4();
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

        private IDictionary<string, object?> GetAttributes(IPersistEntity entity, AttributeFacet facet)
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
                        if (!(value == null && IsEnum(facet.AttributeName)))
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
            return BindAttributeSelection(expression, constraint, propertyMeta.PropertyInfo);
        }


        /// <summary>
        /// Binds a Expression check an entity's attribute(s) satisifiy a constraint
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="ifcAttributePropInfos"></param>
        /// <param name="constraint"></param>
        /// <returns></returns>
        private static Expression BindAttributeSelection(Expression expression,
            ValueConstraint constraint, params PropertyInfo[] ifcAttributePropInfos)
        {
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            var constraints = constraint?.AcceptedValues;

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

           
            // Build IEnumerable<TEntity>().Where(t => ValueConstraintExtensions.SatisfiesConstraint(constraint, t.[AttributeName]))

            // build IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            Expression querybody;
            if (constraints?.Any() == true)
            {
                // Ensure Attribute satisfies Value constraint
                var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));

                // build t => ValueConstraintExtensions.SatisfiesConstraint(constraint, t.[AttributeName])
                querybody = BuildAttributeQuery(ifcAttributePropInfos, ifcTypeParam, constraintExpr);

            }
            else
            {
                // If no constraints or nothing specified (null) just check not null
                
                querybody = BuildAttributeNotNull(ifcAttributePropInfos, ifcTypeParam);
            }

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);

            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
        }

        internal static Expression BuildAttributeQuery(PropertyInfo[] ifcAttributePropInfo, ParameterExpression ifcTypeParam, ConstantExpression constraintExpr)
        {
            Expression body = Expression.Constant(false);   // default false
            foreach(var ifcAttribute in ifcAttributePropInfo)
            {
                Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttribute);

                // build params, & unwrap Type

                var valueExpr = Expression.Convert(nameProperty, typeof(object));

                // build: t => ValueConstraintExtensions.SatisfiesConstraint(constraint, t.[AttributeName])
                Expression querybody = Expression.Call(null, ExpressionHelperMethods.IdsSatisifiesConstraintMethod, constraintExpr, valueExpr);
                // Join into Or statement for multiple attributes - includes parenthesis
                body = (body is ConstantExpression) ? querybody : Expression.OrElse(body, querybody);
            }
            
            return body;
        }

        internal static Expression BuildAttributeNotNull(PropertyInfo[] ifcAttributePropInfo, ParameterExpression ifcTypeParam)
        {
            var nullExpr = Expression.Constant(null, typeof(object));
            Expression body = Expression.Constant(false); // default false
            foreach (var ifcAttribute in ifcAttributePropInfo)
            {
                Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttribute);

                UnaryExpression valueExpr = Expression.Convert(nameProperty, typeof(object));

                // build: t =>  t.[AttributeName] != null
                Expression querybody = Expression.NotEqual(valueExpr, nullExpr);
                // Join into Or statement for multiple attributes - includes parenthesis

                body = (body is ConstantExpression) ? querybody: Expression.OrElse(body, querybody);

            }

            return body;
        }
    }
}
