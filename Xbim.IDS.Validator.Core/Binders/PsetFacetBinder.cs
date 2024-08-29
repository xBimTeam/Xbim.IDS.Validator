using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Common;
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

    public class PsetFacetBinder : FacetBinderBase<IfcPropertyFacet>
    {
        private readonly ILogger<PsetFacetBinder> logger;

        public PsetFacetBinder(ILogger<PsetFacetBinder> logger) : base(logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Binds an IFC property filter to an expression, where properties are IFC Pset and Quantity fields
        /// </summary>
        /// <remarks>e.g Where(p=> p.RelatingPropertyDefinition... )</remarks>
        /// <param name="baseExpression"></param>
        /// <param name="psetFacet"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override Expression BindSelectionExpression(Expression baseExpression, IfcPropertyFacet psetFacet)
        {
            if (baseExpression is null)
            {
                throw new ArgumentNullException(nameof(baseExpression));
            }

            if (psetFacet is null)
            {
                throw new ArgumentNullException(nameof(psetFacet));
            }

            if (!psetFacet.IsValid())
            {
                // IsValid checks for mandatory fields
                throw new InvalidOperationException($"IFC Property Facet '{psetFacet?.PropertySetName}'.{psetFacet?.PropertyName} is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the RelDefinesByProperties

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                expression = BindIfcExpressType(expression, Model.Metadata.GetExpressType(typeof(IIfcRelDefinesByProperties)), false);
                expression = BindPropertySelection(expression, psetFacet);
                return expression;
            }

            throw new NotSupportedException("Selection of Psets must be the first expression in the graph");
        }

        public override Expression BindWhereExpression(Expression baseExpression, IfcPropertyFacet facet)
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
                // IsValid checks for mandatory fields
                throw new InvalidOperationException($"IFC Property Facet '{facet?.PropertySetName}'.{facet?.PropertyName} is not valid");
            }


            var expression = baseExpression;

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                throw new NotSupportedException("Expected a selection expression before applying filters");
            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = Model.Metadata.GetExpressType(collectionType);
            if (!ExpressTypeIsValid(expressType))
            {
                throw new InvalidOperationException($"Invalid IFC Type '{collectionType.Name}'");
            }

            expression = BindPropertyFilter(expression, facet);
            return expression;
        }

        public override void ValidateEntity(IPersistEntity item, IfcPropertyFacet facet, Cardinality cardinality, IdsValidationResult result)
        {
            var ctx = CreateValidationContext(cardinality, facet);
            var psets = GetPropertySetsMatching(item.EntityLabel, facet.PropertySetName, logger);
            if (psets.Any())
            {
                bool? success = null;
                bool? failure = null;
                foreach (var pset in psets)
                {

                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertySetName!, pset.Name, "PropertySet Matched", pset));
                    
                    var props = GetPropertiesMatching<IIfcSimpleProperty>(item.EntityLabel, pset.Name, facet.PropertyName);
                    var quants = GetQuantitiesMatching(item.EntityLabel, pset.Name, facet.PropertyName);
                    var predefinedProps = GetPredefinedPropertiesMatching(item.EntityLabel, pset.Name);
                    if (props.Any() || quants.Any() || predefinedProps.Any())
                    {
                        CheckProperties(facet, cardinality, result, ctx, ref success, ref failure, pset, props);
                        CheckQuants(facet, cardinality, result, ctx, ref success, ref failure, pset, quants);
                        CheckPredefined(facet, cardinality, result, ctx, ref success, ref failure, pset, predefinedProps);
                    }
                    else
                    {
                        // Our treatment of Wildcard PsetNames is a deviation from the the standard
                        // The official spec says if we find the property in one matching pset but not another 
                        // it's a fail.  PSetName is required.
                        // See fail-all_matching_property_sets_must_satisfy_requirements_2_3.ids
                        // So this means that a user cannot verify 
                        // a Property without identifying a unique Pset name. But many examples exist where
                        // requirement is that an element has, say, a SerialNo - but we don't specify where.
                        // so IsWildCard is a fudge enabling the user's intenton of "I don't know/care"
                        if (facet.PropertySetName.IsNullOrEmpty() || facet.PropertySetName.IsWildcard())
                        {
                            continue;
                            // we found a propertyset but it has no matching property. Another pset may though
                        }
                        else 
                        { 
                            if(cardinality == Cardinality.Expected)
                            {
                                result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, $"No matching property in PropertySet '{pset.Name}'", pset));
                                failure = true;
                            }
                            
                        }
                    }
                }
                // If no matching prop found after all the psets checked, mark as failed
                if(success == default && failure == default)
                {
                    switch (cardinality)
                    {
                        case Cardinality.Expected:
                            {
                                result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, "No properties matching", item));
                                break;
                            }

                        case Cardinality.Optional:
                        case Cardinality.Prohibited:
                            {
                                result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyName!, null, $"Property not found", item));
                                break;
                            }
                    }
                }
            }
            else // No matching PropertySets found for this item
            {
                
                switch (cardinality)
                {
                    case Cardinality.Expected:
                        {
                            result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertySetName!, null, "No PropertySet matching", item));
                            break;
                        }

                    case Cardinality.Optional:
                    case Cardinality.Prohibited:
                        {
                            result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertySetName!, null, $"PropertySet not found", item));
                            break;
                        }
                }
            }


        }

        private void CheckProperties(IfcPropertyFacet facet, Cardinality cardinality, IdsValidationResult result, ValidationContext<IfcPropertyFacet> ctx, ref bool? success, ref bool? failure, IIfcPropertySetDefinition pset, IEnumerable<IIfcSimpleProperty> props)
        {
            foreach (var prop in props)
            {
                // Except for SingleValues, other SimpleProperties have multiple values we check against
                // We just need one match to consider requirement satisfied
                IEnumerable<IIfcValue> values = ExtractPropertyValues(prop).ToList();
                var satisfiedValue = false;
                var satisfiedProp = false;

                foreach (var propValue in values)
                {
                    object? value = GetNormalisedValue(propValue);
                    bool isPopulated = IsValueRelevant(value);
                    var valueExpected = cardinality != Cardinality.Prohibited || !facet.PropertyValue.IsNullOrEmpty();
                    if (isPopulated == valueExpected)
                    {
                        satisfiedProp = true;
                    }

                    if (ValueSatifiesConstraint(facet, value, ctx))
                    {
                        satisfiedValue = true;
                        if (ValidateDataType(ctx, result, propValue, facet.DataType))
                        {
                            // We found a match
                            break;
                        }
                    }
                }
                // TODO: Refactor
                if (satisfiedProp)
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyName!, prop.Name, $"Property provided in {pset.Name}", prop));
                    success = true;
                }
                else
                {
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, prop.Name, $"No property matching in {pset.Name}", prop));
                    failure = true;
                }

                var vals = string.Join(',', values.Select(v => GetNormalisedValue(v)));
                if (satisfiedValue)
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyValue!, vals, $"Value matched in {pset.Name}.{prop.Name}", prop));
                    success = true;
                }
                else
                {
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, vals, $"Invalid Value in {pset.Name}.{prop.Name}", prop));
                    failure = true;
                }

            }
        }

        private void CheckQuants(IfcPropertyFacet facet, Cardinality cardinality, IdsValidationResult result, ValidationContext<IfcPropertyFacet> ctx, ref bool? success, ref bool? failure, IIfcPropertySetDefinition pset, IEnumerable<IIfcPhysicalQuantity> quants)
        {
            foreach (var quant in quants)
            {
                var propValue = UnwrapQuantity(quant);
                object? value = GetNormalisedValue(propValue);
                bool isPopulated = IsValueRelevant(value);
                var valueExpected = cardinality != Cardinality.Prohibited || !facet.PropertyValue.IsNullOrEmpty();


                // TODO: No test cases for Quantity values
                if (isPopulated == valueExpected)
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyName!, quant.Name, $"Quantity provided in {pset.Name}", quant));
                    success = true;
                }
                else
                {
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, quant.Name, $"No quantity matching in {pset.Name}", quant));
                    failure = true;
                }
                ValidateDataType(ctx, result, propValue, facet.DataType);


                if (ValueSatifiesConstraint(facet, value, ctx))
                {
                    result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyValue!, value, $"Quantity matched in {pset.Name}.{quant.Name}", propValue));
                    success = true;
                }
                else
                {
                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, value, $"Invalid quantity in {pset.Name}.{quant.Name}", propValue));
                    failure = true;
                }
            }
        }

        // Check PredefinedPropertySets, using similar approach to AttributeFacets
        private void CheckPredefined(IfcPropertyFacet facet, Cardinality cardinality, IdsValidationResult result, ValidationContext<IfcPropertyFacet> ctx, ref bool? success, ref bool? failure, IIfcPropertySetDefinition pset, IEnumerable<IIfcPreDefinedPropertySet> predefined)
        {
            foreach (var predef in predefined)
            {
                
                var candidates = GetMatchingAttributes(predef, facet.PropertyName);
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

                    if (facet.PropertyValue != null)
                    {
                        attrvalue = HandleBoolConventions(attrvalue);
                        // Unpack Ifc Values
                        if (attrvalue is IIfcValue v)
                        {
                            attrvalue = v.Value;
                        }
                        if (attrvalue is Enum e)
                        {
                            attrvalue = e.ToString();
                        }
                        if (IsTypeAppropriateForConstraint(facet.PropertyValue, attrvalue) && facet.PropertyValue.ExpectationIsSatisifedBy(attrvalue, ctx, logger))
                        {
                            result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyValue!, attrvalue, "Predefined Property value matched", predef));
                            success = true;
                        }
                        else
                        {
                            switch (cardinality)
                            {
                                case Cardinality.Expected:
                                    result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, attrvalue, "No Predefined Property value matched", predef));
                                    failure = true;
                                    break;

                                case Cardinality.Prohibited:
                                    result.MarkSatisified(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, attrvalue, "No matching Predefined Property value", predef));
                                    success = true;
                                    break;

                                case Cardinality.Optional:
                                    if (attrvalue is string s && s == string.Empty)
                                    {
                                        result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, attrvalue, "Empty Predefined Property found", predef));
                                        failure = true;
                                    }
                                    else
                                    {
                                        result.MarkSatisified(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, attrvalue, "No matching attribute value", predef));
                                        success = true;
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
                            {
                                result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyName!, attrName, "Predefined Property value populated", predef));
                                success = true;
                            }
                            else
                            {
                                result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, attrName, "Predefined Property value prohibited", predef));
                                failure = true;
                            }
                        }
                        else
                        {
                            if (valueExpected)
                            {
                                result.Fail(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, "Predefined Property value blank", predef));
                                failure = true;
                            }
                            else
                            {
                                result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.PropertyName!, null, "Predefined Property value not set", predef));
                                success = true;
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<IIfcValue> ExtractPropertyValues(IIfcSimpleProperty prop)
        {
            switch (prop)
            {
                case IIfcPropertySingleValue single:
                    yield return single.NominalValue;
                    break;

                case IIfcPropertyListValue list:
                    foreach (var item in list.ListValues)
                        yield return item;
                    break;

                case IIfcPropertyEnumeratedValue en:
                    foreach (var item in en.EnumerationValues)
                        yield return item;
                    break;

                case IIfcPropertyTableValue tab:
                    foreach (var item in tab.DefinedValues)
                        yield return item;
                    foreach (var item in tab.DefiningValues)
                        yield return item;
                    break;

                case IIfcPropertyBoundedValue bound:
                    yield return bound.LowerBoundValue;
                    yield return bound.UpperBoundValue;
                    yield return bound.SetPointValue;
                    break;
            }
        }

        private static Expression BindPropertySelection(Expression expression, IfcPropertyFacet psetFacet)
        {
            if (psetFacet is null)
            {
                throw new ArgumentNullException(nameof(psetFacet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);

            // call .Cast<EntityType>()
            expression = Expression.Call(null, ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(collectionType), expression);

            if (psetFacet?.PropertySetName?.AcceptedValues?.Any() == false ||
                psetFacet?.PropertyName?.AcceptedValues?.Any() == false)
            {
                return expression;
            }

            var facetExpr = Expression.Constant(psetFacet, typeof(IfcPropertyFacet));
            // Expression we're building
            // var psetRelDefines = model.Instances.OfType<IIfcRelDefinesByProperties>();
            // var entities = IfcExtensions.GetIfcObjectsWithProperties(psetRelDefines, facet);

            var propsMethod = ExpressionHelperMethods.EnumerableIfcObjectsWithProperties;

            return Expression.Call(null, propsMethod, new[] { expression, facetExpr });

          
        }


        private Expression BindPropertyFilter(Expression expression, IfcPropertyFacet facet)
        {
            if (facet is null)
            {
                throw new ArgumentNullException(nameof(facet));
            }
            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            if (collectionType == null)
            {
                logger.LogWarning("Expected an enumerable collection but found {expressionType}", expression.Type.Name);
                return expression;
            }

            ConstantExpression psetFacetExpr = Expression.Constant(facet, typeof(IfcPropertyFacet));

            MethodInfo objectsMethod = ExpressionHelperMethods.EnumerableObjectWhereAssociatedWithProperty;
            MethodInfo typesMethod = ExpressionHelperMethods.EnumerableTypeWhereAssociatedWithProperty;
            if (typeof(IIfcObject).IsAssignableFrom(collectionType))
            {
                return Expression.Call(null, objectsMethod, new[] { expression, psetFacetExpr });
            }
            else if (typeof(IIfcTypeObject).IsAssignableFrom(collectionType))
            {
                return Expression.Call(null, typesMethod, new[] { expression, psetFacetExpr });
            }
            else if (typeof(IIfcObjectDefinition).IsAssignableFrom(collectionType))
            {
                // We could have a mixture of Objects and Types. (e.g. when starting IfcRelDefinesByProperties).
                // So have to cast and check objects and Types separately and concat the results.
                // Use case, all elements with IsExternal=True which are also LoadBearing, could be a mix of Objects and Types

                Expression objectsExpr = Expression.Call(null, ExpressionHelperMethods.EnumerableOfTypeGeneric.MakeGenericMethod(typeof(IIfcObject)), expression);
                Expression typesExpr = Expression.Call(null, ExpressionHelperMethods.EnumerableOfTypeGeneric.MakeGenericMethod(typeof(IIfcTypeObject)), expression);

                MethodCallExpression filteredObjects = Expression.Call(null, objectsMethod, new[] { objectsExpr, psetFacetExpr });
                MethodCallExpression filteredTypes = Expression.Call(null, typesMethod, new[] { typesExpr, psetFacetExpr });

                return BindConcat(filteredObjects, filteredTypes);
            }
            else
            {
                // Edge case - if we have Materials etc
                // TODO: implement IfcMaterial.HasProperties.
                logger.LogWarning("Cannot apply property filter on this type: {collectionType} ", collectionType.Name);
                throw new NotImplementedException($"Cannot apply property filter on this type: {collectionType}");
                
                // return BindNotFound(expression, collectionType);
            }

            


        }

        /// <summary>
        /// Finds all Properties in a pset meeting a constraint
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="constraint"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private IEnumerable<T> GetPropertiesMatching<T>(int entityLabel, string psetName, ValueConstraint constraint, ILogger? logger = null) where T : IIfcProperty
        {
            if(constraint == null)
                return Enumerable.Empty<T>();
            var entity = Model.Instances[entityLabel];
            if (entity is IIfcTypeObject type)
            {
                var typeProperties = type.HasPropertySets.OfType<IIfcPropertySet>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.HasProperties.Where(ps => constraint.IsSatisfiedBy(ps.Name.Value, true, logger))
                        .OfType<T>());
                return typeProperties;


            }
            else if (entity is IIfcObject obj)
            {
                var entityProperties = obj.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcPropertySet)p.RelatingPropertyDefinition)
                        .HasProperties.Where(ps => constraint.IsSatisfiedBy(ps.Name.Value, true, logger))
                        .OfType<T>());

                //var x = obj.IsDefinedBy
                //    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySetDefinition ps && ps.Name == psetName)
                //    .Select(p => (p.RelatingPropertyDefinition));

                if (obj.IsTypedBy?.Any() == true)
                {
                    // Inherit extra properties from Type - Deduping on name
                    entityProperties = entityProperties
                        .Union(GetPropertiesMatching<T>(obj.IsTypedBy.First().RelatingType.EntityLabel, psetName, constraint, logger), new PropertyEqualityComparer<T>());
                }
                return entityProperties;


            }
            else
            {
                return Enumerable.Empty<T>();
            }
        }

        /// <summary>
        /// Finds all Quantities in a pset meeting a constraint
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="nameConstraint"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private IEnumerable<IIfcPhysicalQuantity> GetQuantitiesMatching(int entityLabel, string psetName, ValueConstraint nameConstraint, ILogger? logger = null)
        {
            var entity = Model.Instances[entityLabel];
            if (entity is IIfcTypeObject type)
            {
                var typeProperties = type.HasPropertySets.OfType<IIfcElementQuantity>()
                    .Where(p => p.Name == psetName)
                    .SelectMany(p => p.Quantities.Where(ps => nameConstraint.IsSatisfiedBy(ps.Name.Value, true, logger)));
                return typeProperties;


            }
            else if (entity is IIfcObject obj)
            {
                var entityProperties = obj.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcElementQuantity ps && ps.Name == psetName)
                    .SelectMany(p => ((IIfcElementQuantity)p.RelatingPropertyDefinition)
                        .Quantities.Where(ps => nameConstraint.IsSatisfiedBy(ps.Name.Value, true, logger)));


                if (obj.IsTypedBy?.Any() == true)
                {
                    // Inherit extra properties from Type - Deduping on Name
                    entityProperties = entityProperties
                        .Union(GetQuantitiesMatching(obj.IsTypedBy.First().RelatingType.EntityLabel, psetName, nameConstraint, logger), new QuantityEqualityComparer());
                }
                return entityProperties;


            }
            else
            {
                return Enumerable.Empty<IIfcPhysicalQuantity>();
            }
        }

        /// <summary>
        /// Finds all Quantities in a pset meeting a constraint
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <param name="psetName"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private IEnumerable<IIfcPreDefinedPropertySet> GetPredefinedPropertiesMatching(int entityLabel, string psetName, ILogger? logger = null)
        {
            var entity = Model.Instances[entityLabel];
            if (entity is IIfcTypeObject type)
            {
                var typeProperties = type.HasPropertySets.OfType<IIfcPreDefinedPropertySet>()
                    .Where(p => p.Name == psetName);
                   // .SelectMany(p => p.Quantities.Where(ps => nameConstraint.IsSatisfiedBy(ps.Name.Value, true, logger)));
                return typeProperties;


            }
            else if (entity is IIfcObject obj)
            {
                var entityProperties = obj.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcPreDefinedPropertySet ps && ps.Name == psetName)
                    .Select(p => (IIfcPreDefinedPropertySet)p.RelatingPropertyDefinition);
                     //   .Quantities.Where(ps => nameConstraint.IsSatisfiedBy(ps.Name.Value, true, logger)));


                if (obj.IsTypedBy?.Any() == true)
                {
                    // Inherit extra properties from Type - Deduping on Name
                    entityProperties = entityProperties
                        .Union(GetPredefinedPropertiesMatching(obj.IsTypedBy.First().RelatingType.EntityLabel, psetName, logger), new PropertySetEqualityComparer<IIfcPreDefinedPropertySet>());
                }
                return entityProperties;


            }
            else
            {
                return Enumerable.Empty<IIfcPreDefinedPropertySet>();
            }
        }


        private IEnumerable<IIfcPropertySetDefinition> GetPropertySetsMatching(int entityLabel, ValueConstraint psetConstraint, ILogger? logger = null)
        {
            var entity = Model.Instances[entityLabel];

            // handle edgecase where Pset is not specified. Pset is required but this
            // allows us to just in time wild-card the psetname, enabling properties to be matched
            if(psetConstraint.IsNullOrEmpty())
            {
                psetConstraint = new ValueConstraint();
                psetConstraint.AddAccepted(new PatternConstraint(".*"));
            }
            if (entity is IIfcTypeObject type)
            {
                var typeProperties = type.HasPropertySets.OfType<IIfcPropertySetDefinition>()
                    .Where(p => psetConstraint?.IsSatisfiedBy(p.Name.ToString(), true, logger) == true);
                return typeProperties;

            }
            else if (entity is IIfcObject obj)
            {

                var entityProperties = obj.IsDefinedBy
                    .Where(t => t.RelatingPropertyDefinition is IIfcPropertySetDefinition ps && psetConstraint?.IsSatisfiedBy(ps.Name.ToString(), true, logger) == true)
                    .Select(p => (IIfcPropertySetDefinition)p.RelatingPropertyDefinition);


                if (obj.IsTypedBy?.Any() == true)
                {
                    // Inherit extra properties from Type
                    entityProperties = entityProperties.Concat(GetPropertySetsMatching(obj.IsTypedBy.First().RelatingType.EntityLabel, psetConstraint, logger));
                }

                return entityProperties;
            }
            else
            {
                return Enumerable.Empty<IIfcPropertySet>();
            }
        }



        private bool ValueSatifiesConstraint(IfcPropertyFacet pf, object? value, ValidationContext<IfcPropertyFacet> ctx)
        {
            if (pf.PropertyValue != null)
            {
                if (IsTypeAppropriateForConstraint(pf.PropertyValue, value) && pf.PropertyValue.ExpectationIsSatisifedBy(value, ctx, logger))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }


        protected bool ValidateDataType(ValidationContext<IfcPropertyFacet> ctx, IdsValidationResult result, IIfcValue propValue, string expectedDataType)
        {
            if (propValue is null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(expectedDataType)) return true;

            string measure = propValue.GetType().Name.ToUpperInvariant();

            if (measure.Equals(expectedDataType, StringComparison.InvariantCultureIgnoreCase))
            {
                result.MarkSatisified(ValidationMessage.Success(ctx, fn => fn.DataType!, measure, "DataType matched", propValue));
                return true;
            }
            else
            {
                result.Fail(ValidationMessage.Failure(ctx, fn => fn.DataType!, measure, "Invalid DataType", propValue));
                return false;
            }

        }



        private class QuantityEqualityComparer : IEqualityComparer<IIfcPhysicalQuantity>
        {
            public bool Equals(IIfcPhysicalQuantity? x, IIfcPhysicalQuantity? y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode([DisallowNull] IIfcPhysicalQuantity obj)
            {
                return (obj.Name, obj.Description).GetHashCode();
            }
        }

        private class PropertyEqualityComparer<T> : IEqualityComparer<T> where T : IIfcProperty
        {
            public bool Equals(T x, T y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode([DisallowNull] T obj)
            {
                return (obj.Name, obj.Description).GetHashCode();
            }
        }

        private class PropertySetEqualityComparer<T> : IEqualityComparer<T> where T : IIfcPropertySetDefinition
        {
            public bool Equals(T x, T y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode([DisallowNull] T obj)
            {
                return (obj.Name, obj.Description).GetHashCode();
            }
        }
    }
}
