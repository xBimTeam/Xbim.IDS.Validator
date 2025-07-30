using IdsLib.IfcSchema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
#if XbimV6
using Xbim.Ifc4x3;  // To provide ToIfc4() extension method
#endif
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;


namespace Xbim.IDS.Validator.Core.Binders
{
    public class AttributeFacetBinder : FacetBinderBase<AttributeFacet>, ISupportOptions
    {
        protected readonly ILogger<AttributeFacetBinder> logger;
        protected readonly IValueMapper valueMapper;
        private VerificationOptions _options = new VerificationOptions();

        public AttributeFacetBinder(ILogger<AttributeFacetBinder> logger, IValueMapper valueMapper) : base(logger)
        {
            this.logger = logger;
            this.valueMapper = valueMapper;
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

            if (!attrFacet.IsValid() && Model.SchemaVersion != Xbim.Common.Step21.XbimSchemaVersion.Cobie2X4)
            {
                // IsValid checks against a known list of all IFC Attributes
                throw new InvalidOperationException($"Attribute Facet '{attrFacet?.AttributeName}' is not valid");
            }

            var expression = baseExpression;

            if (attrFacet.AttributeName?.IsSingleExact(out var attributeName) == true)
            {
                // When an Ifc Type facet has not yet been specified, find correct IFC type(s) for this AttributeName
                // using the lookup that IDSLib provides

                // Test for raw Model.Instances:
                if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
                {
                    // Work out the set of all types this attribute could apply to.
                    // This is schema dependent. We want to get the highest common roots
                    IEnumerable<string> rootTypes;

                    switch (Model.SchemaVersion)
                    {
                        case Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3:
                            rootTypes = SchemaInfo.SchemaIfc2x3.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                            break;
#if XbimV6
                        case Xbim.Common.Step21.XbimSchemaVersion.Ifc4x3:
                            rootTypes = SchemaInfo.SchemaIfc4x3.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                            break;
#endif
                        //case Xbim.Common.Step21.XbimSchemaVersion.Cobie2X4:
                        //    //rootTypes = SchemaInfo.SchemaIfc4.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                        //    break;
                        // TODO: Support COBieExpress. E.g. CreatedBy => CobieReferencedObject, SerialNumber = COBieComponent

                        case Xbim.Common.Step21.XbimSchemaVersion.Ifc4:
                        case Xbim.Common.Step21.XbimSchemaVersion.Ifc4x1:
                            rootTypes = SchemaInfo.SchemaIfc4.GetAttributeClasses((string)attributeName, onlyTopClasses: true);
                            break;

                        default:
                            throw new NotImplementedException($"Unsupported Schema {Model.SchemaVersion}");

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
                if(attrFacet.AttributeName is null || attrFacet.AttributeName.IsEmpty() == true)
                {
                    logger.LogWarning("AttributeName is Required");
                    return BindNotFound(expression);
                }
                // TODO: Should support Enum, Regex, Range. E.g. Where Name or Description = 'Foo'
                
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

        public override void ValidateEntity(IPersistEntity item, AttributeFacet af, Cardinality cardinality, IdsValidationResult result)
        {
            if (af is null)
            {
                throw new ArgumentNullException(nameof(af));
            }
            var ctx = CreateValidationContext(cardinality, af);

            var candidates = GetMatchingAttributes(item, af.AttributeName, _options?.AllowDerivedAttributes ?? false);

            FixDataType(af.AttributeValue, candidates.FirstOrDefault().Value);
            if (candidates.Any())
            {


                foreach (var pair in candidates)
                {
                    var attrName = pair.Key;
                    var attrvalue = pair.Value;
                    if (IsIfc2x3Model() && attrvalue is Xbim.Ifc2x3.MeasureResource.IfcValue ifc2x3Value)
                    {
                        attrvalue = ifc2x3Value.ToIfc4();
                    }
#if XbimV6
                    else if (IsIfc4x3Model() && attrvalue is Xbim.Ifc4x3.MeasureResource.IfcValue ifc4x3Value)
                    {
                        attrvalue = ifc4x3Value.ToIfc4();
                    }
#endif

                    if (af.AttributeValue != null)
                    {
                        // Unwrap the value from IfcValues etc.
                        attrvalue = MapValue(attrvalue, valueMapper);
                        if (IsTypeAppropriateForConstraint(af.AttributeValue, attrvalue) && af.AttributeValue.ExpectationIsSatisifedBy(attrvalue, ctx, logger))
                            result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.AttributeValue!, attrvalue, "Attribute value OK", item));
                        else
                        {
                            switch (cardinality)
                            {
                                case Cardinality.Expected:
                                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.AttributeValue!, attrvalue, "No attribute value matched", item));
                                    break;

                                case Cardinality.Prohibited:
                                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.AttributeValue!, attrvalue, "Matched prohibited attribute", item));
                                    break;

                                case Cardinality.Optional:
                                    if (attrvalue is string s && s == string.Empty)
                                    {
                                        // Empty strings on Optional Attributes are expected to fail as per 'fail-an_optional_attribute_fails_if_empty'
                                        result.Fail(ValidationMessage.Failure(ctx, fn => fn.AttributeValue!, attrvalue, "Empty attribute found", item));
                                    }
                                    else if(attrvalue == null)
                                    {
                                        // A null value is otherwise fine for an optional attribute.
                                        result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.AttributeValue!, attrvalue, "Attribute empty", item));
                                    }
                                    else
                                    {
                                        // else a value was supplied but it doesn't satisfy the constraint.
                                        result.Fail(ValidationMessage.Failure(ctx, fn => fn.AttributeValue!, attrvalue, "No matching attribute value", item));
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Value not specified - just check presence or otherwise of a Value
                        bool isPopulated = IsValueRelevant(attrvalue);
                        var valueExpected = cardinality == Cardinality.Expected;
                        // Name meets requirement if it has a value and is Required. Treat unknown logical as no value
                        if (isPopulated)
                        {
                            if (valueExpected)
                                result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.AttributeName!, attrName, "Attribute value populated", item));
                            else
                                result.Fail(ValidationMessage.Failure(ctx, fn => fn.AttributeName!, attrName, "Attribute value prohibited", item));
                        }
                        else
                        {
                            if (valueExpected)
                                result.Fail(ValidationMessage.Failure(ctx, fn => fn.AttributeName!, null, "Attribute value blank", item));
                            else
                                result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.AttributeName!, null, "Attribute value not set", item));
                        }
                    }

                }
            }
            else
            {
                // Not a valid attribute. Always fails. 
                result.Fail(ValidationMessage.Failure(ctx, fn => fn.AttributeName!, null, "No valid attribute", item));
            }
        }

        private Expression BindAttributeSelection(Expression expression, ExpressType expressType,
            string ifcAttributeName, ValueConstraint constraint)
        {
            var props = GetAllProperties(expressType, _options?.AllowDerivedAttributes ?? false);
            var propertyMeta = props.FirstOrDefault(p => p.Name == ifcAttributeName);
            if (propertyMeta == null)
            {
                throw new InvalidOperationException($"Property '{ifcAttributeName}' not found on '{expressType.Name}'");
            }
            if (propertyMeta.EnumerableType != null)
            {
                return BindAnyAttributeSelection(expression, constraint, valueMapper, propertyMeta.PropertyInfo);
            }
            return BindAttributeSelection(expression, constraint, valueMapper, propertyMeta.PropertyInfo);
        }

        /// <summary>
        /// Binds an Expression checking an entity's attribute(s) satisifies the constraint
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="ifcAttributePropInfos"></param>
        /// <param name="valueMapper"></param>
        /// <param name="constraint"></param>
        /// <returns></returns>
        private static Expression BindAttributeSelection(Expression expression,
            ValueConstraint constraint, IValueMapper valueMapper, params PropertyInfo[] ifcAttributePropInfos)
        {
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            var constraints = constraint?.AcceptedValues;

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

           
            // Build IEnumerable<TEntity>().Where(ent => ValueConstraintExtensions.SatisfiesConstraint(constraint, ent.[AttributeName]))

            // build IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            Expression querybody;
            if (constraints?.Any() == true)
            {
                // Ensure Attribute satisfies Value constraint
                var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));
                var valueMapperExpr = Expression.Constant(valueMapper, typeof(IValueMapper));

                // build t => ValueConstraintExtensions.SatisfiesConstraint(constraint, t.[AttributeName])
                querybody = BuildAttributeQuery(ifcAttributePropInfos, ifcTypeParam, constraintExpr, valueMapperExpr);

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

        /// <summary>
        /// Binds an expression to a Enumerable collection, checking if Any of the elements match constraint
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="constraint"></param>
        /// <param name="valueMapper"></param>
        /// <param name="ifcAttributePropInfos"></param>
        /// <returns></returns>
        private static Expression BindAnyAttributeSelection(Expression expression,
            ValueConstraint constraint, IValueMapper valueMapper, params PropertyInfo[] ifcAttributePropInfos)
        {
            // Build IEnumerable<TEntity>().Where(ent => ent.[AttributeName].Any(p => ValueConstraintExtensions.SatisfiesConstraint(constraint, p)));
            // vs    IEnumerable<TEntity>().Where(ent => ValueConstraintExtensions.SatisfiesConstraint(constraint, ent.[AttributeName]))

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            List<IValueConstraintComponent>? constraints = constraint?.AcceptedValues;

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            // build IEnumerable.Where<TEntity>(...)
            var whereMethod = ExpressionHelperMethods.EnumerableWhereGeneric.MakeGenericMethod(collectionType);

            // build lambda param 'ent => ...'
            ParameterExpression ifcTypeParam = Expression.Parameter(collectionType, "ent");

            Expression querybody;
            if (constraints?.Any() == true)
            {
                // constraint Constant
                var constraintExpr = Expression.Constant(constraint, typeof(ValueConstraint));
                var valueMapperExpr = Expression.Constant(valueMapper, typeof(IValueMapper));

                // build ent => ent.[AttributeName].Any(p => ValueConstraintExtensions.SatisfiesConstraint(constraint, p))
                querybody = BuildAnyAttributeQuery(ifcAttributePropInfos, ifcTypeParam, constraintExpr, valueMapperExpr);

            }
            else
            {
                // If no constraints or nothing specified (null) just check not null

                // TODO: Where(t => t.[Property].Any())

                querybody = BuildAttributeNotNull(ifcAttributePropInfos, ifcTypeParam);
            }

            // Build Lambda expression for filter predicate (Func<T,bool>)
            var filterExpression = Expression.Lambda(querybody, ifcTypeParam);

            // Bind Lambda to Where method
            return Expression.Call(null, whereMethod, new[] { expression, filterExpression });
        }

        internal static Expression BuildAttributeQuery(PropertyInfo[] ifcAttributePropInfo, ParameterExpression ifcTypeParam, ConstantExpression constraintExpr, ConstantExpression valueMapExpr)
        {
            Expression body = Expression.Constant(false);   // default false
            foreach (var ifcAttribute in ifcAttributePropInfo)
            {
                Expression nameProperty = Expression.Property(ifcTypeParam, ifcAttribute);

                // build params, & unwrap Type

                var valueExpr = Expression.Convert(nameProperty, typeof(object));

                // build: t => ValueConstraintExtensions.SatisfiesConstraint(constraint, t.[AttributeName], valueMapper)
                Expression querybody = Expression.Call(null, ExpressionHelperMethods.IdsSatisifiesConstraintMethod, constraintExpr, valueExpr, valueMapExpr);
                // Join into Or statement for multiple attributes - includes parenthesis
                body = (body is ConstantExpression) ? querybody : Expression.OrElse(body, querybody);
            }

            return body;
        }

        internal static Expression BuildAnyAttributeQuery(PropertyInfo[] ifcAttributePropInfo, ParameterExpression ifcTypeParam, ConstantExpression constraintExpr, ConstantExpression valueMapExpr)
        {
            // build ent => ent.[AttributeName].Any(p => ValueConstraintExtensions.SatisfiesConstraint(constraint, p)) || {another Attr}

            Expression body = Expression.Constant(false);   // default false
            foreach(var ifcAttribute in ifcAttributePropInfo)
            {

                var propType = TypeHelper.GetImplementedIEnumerableType(ifcAttribute.PropertyType);
                
                // build lambda param 'p => ...'
                ParameterExpression attrParam = Expression.Parameter(propType, "p");

                // ent.[AttributeName]
                Expression collectionProperty = Expression.Property(ifcTypeParam, ifcAttribute);

                MethodInfo anyMethod = ExpressionHelperMethods.EnumerableAnyGeneric.MakeGenericMethod(propType);
               

                // build: p => ValueConstraintExtensions.SatisfiesConstraint(constraint, p)
                Expression querybody = Expression.Call(null, ExpressionHelperMethods.IdsSatisifiesConstraintMethod, constraintExpr, attrParam, valueMapExpr);

                // Build Lambda expression for filter predicate (Func<T,bool>)
                var filterExpression = Expression.Lambda(querybody, attrParam);


                // Call Any(this collection, filterPredicate)
                Expression anyQuery = Expression.Call(null, anyMethod, collectionProperty, filterExpression);
                // Join into Or statement for multiple attributes - includes parenthesis
                body = (body is ConstantExpression) ? anyQuery : Expression.OrElse(body, anyQuery);
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

        public void SetOptions(VerificationOptions options)
        {
            _options = options;
        }
    }
}
