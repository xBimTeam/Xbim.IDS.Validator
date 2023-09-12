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
using Xbim.Ifc4.Kernel;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{

    public class PsetFacetBinder : FacetBinderBase<IfcPropertyFacet>
    {
        private readonly ILogger<PsetFacetBinder> logger;

        public PsetFacetBinder(BinderContext context, ILogger<PsetFacetBinder> logger) : base(context)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Binds an IFC property filter to an expression, where propertoes are IFC Pset and Quantity fields
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
                // IsValid checks against a known list of all IFC Attributes
                throw new InvalidOperationException($"IFC Property Facet '{psetFacet?.PropertySetName}'.{psetFacet?.PropertyName} is not valid");
            }


            var expression = baseExpression;
            // When an Ifc Type has not yet been specified, we start with the RelDefinesByProperties

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                expression = BindIfcExpressType(expression, Model.Metadata.ExpressType(typeof(IfcRelDefinesByProperties)), false);
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
                // IsValid checks against a known list of all IFC Attributes
                throw new InvalidOperationException($"IFC Property Facet '{facet?.PropertySetName}'.{facet?.PropertyName} is not valid");
            }


            var expression = baseExpression;

            if (expression.Type.IsInterface && typeof(IEntityCollection).IsAssignableFrom(expression.Type))
            {
                throw new NotSupportedException("Expected a selection expression before applying filters");
            }

            // Get underlying collection type
            var collectionType = TypeHelper.GetImplementedIEnumerableType(expression.Type);
            var expressType = Model.Metadata.ExpressType(collectionType);
            if (!ExpressTypeIsValid(expressType))
            {
                throw new InvalidOperationException($"Invalid IFC Type '{expression.Type.Name}'");
            }

            expression = BindPropertyFilter(expression, facet);
            return expression;
        }

        public override void ValidateEntity(IPersistEntity item, IfcPropertyFacet facet, RequirementCardinalityOptions requirement, IdsValidationResult result)
        {
            var ctx = CreateValidationContext(requirement, facet);
            var psets = GetPropertySetsMatching(item.EntityLabel, facet.PropertySetName, logger);
            if (psets.Any())
            {
                bool? success = null;
                bool? failure = null;
                foreach (var pset in psets)
                {
                    if (facet.PropertySetName?.IsEmpty() ?? true == false)
                    {
                        // If a constraint was defined acknowledge it, but otherwise this is not yet 'success'
                        result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertySetName!, pset.Name, "Pset Matched", pset));
                    }
                    var props = GetPropertiesMatching<IIfcSimpleProperty>(item.EntityLabel, pset.Name, facet.PropertyName);
                    var quants = GetQuantitiesMatching(item.EntityLabel, pset.Name, facet.PropertyName);
                    if (props.Any() || quants.Any())
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
                                object? value = UnwrapValue(propValue);
                                bool isPopulated = IsValueRelevant(value);
                                if (isPopulated)
                                {
                                    satisfiedProp = true;
                                }

                                if (ValueSatifiesConstraint(facet, value))
                                {
                                    satisfiedValue = true;
                                    if (ValidateMeasure(ctx, result, propValue, facet.Measure))
                                    {
                                        // We found a match
                                        break;
                                    }
                                }
                            }
                            if (satisfiedProp)
                            {
                                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertyName!, prop.Name, $"Property provided in {pset.Name}", prop));
                                success = true;
                            }
                            else
                            {
                                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, prop.Name, $"No property matching in {pset.Name}", prop));
                                failure = true;
                            }

                            var vals = string.Join(',', values);
                            if (satisfiedValue)
                            {
                                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertyValue!, vals, $"Value matched in {pset.Name}_{prop.Name}", prop));
                                success = true;
                            }
                            else
                            {
                                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, vals, $"Invalid Value in {pset.Name}_{prop.Name}", prop));
                                failure = true;
                            }

                        }
                        foreach (var quant in quants)
                        {
                            var propValue = UnwrapQuantity(quant);
                            object? value = UnwrapValue(propValue);
                            bool isPopulated = IsValueRelevant(value);

                            if (isPopulated)
                            {
                                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertyName!, quant.Name, $"Quantity provided in {pset.Name}", quant));
                                success = true;
                            }
                            else
                            {
                                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, quant.Name, $"No quantity matching in {pset.Name}", quant));
                                failure = true;
                            }
                            ValidateMeasure(ctx, result, propValue, facet.Measure);


                            if (ValueSatifiesConstraint(facet, value))
                            {
                                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.PropertyValue!, value, $"Value matched in {pset.Name}_{quant.Name}", propValue));
                                success = true;
                            }
                            else
                            {
                                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyValue!, value, $"Invalid Value in {pset.Name}_{quant.Name}", propValue));
                                failure = true;
                            }
                        }
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
                            result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, "No properties matching in psets", pset));
                            failure = true;
                        }
                    }
                }
                // If no matching value found after all the psets checked, mark as failed
                if(success == default && failure == default)
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, "No properties matching", item));
                }
            }

            else
            {
                if(facet.PropertyName?.IsEmpty() == false)
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertyName!, null, "No Property matching", item));
                }
                else
                {
                    result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.PropertySetName!, null, "No Psets matching", item));
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

            MethodInfo propsMethod;
            if (typeof(IIfcObject).IsAssignableFrom(collectionType))
            {
                propsMethod = ExpressionHelperMethods.EnumerableObjectWhereAssociatedWithProperty;

            }
            else if (typeof(IIfcTypeObject).IsAssignableFrom(collectionType))
            {
                propsMethod = ExpressionHelperMethods.EnumerableTypeWhereAssociatedWithProperty;
            }
            else
            {
                logger.LogWarning("Property sets can only be filtered on Types and Objects, but this is {collectionType} ", collectionType.Name);
                // Not applicable
                return BindNotFound(expression, collectionType);
            }
            ConstantExpression psetFacetExpr = Expression.Constant(facet, typeof(IfcPropertyFacet));

            return Expression.Call(null, propsMethod, new[] { expression, psetFacetExpr });


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
        /// <typeparam name="T"></typeparam>
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


        private IEnumerable<IIfcPropertySetDefinition> GetPropertySetsMatching(int entityLabel, ValueConstraint psetConstraint, ILogger? logger = null)
        {
            var entity = Model.Instances[entityLabel];

            // handle edgecase where Pset is not specified. Pset is required but this
            // allows us to just in time wild-card the psetname, enabling properties to be matched
            if(psetConstraint.IsNullOrEmpty())
            {
                psetConstraint = new ValueConstraint(NetTypeName.String);
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



        private bool ValueSatifiesConstraint(IfcPropertyFacet pf, object? value)
        {
            if (pf.PropertyValue != null)
            {
                value = ApplyWorkarounds(value, pf.PropertyValue);
                if (IsTypeAppropriateForConstraint(pf.PropertyValue, value) && pf.PropertyValue.IsSatisfiedBy(value, logger))
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


        protected bool ValidateMeasure(ValidationContext<IfcPropertyFacet> ctx, IdsValidationResult result, IIfcValue propValue, string expectedMeasure)
        {
            if (propValue is null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(expectedMeasure)) return true;

            string measure = propValue.GetType().Name;

            if (measure.Equals(expectedMeasure, StringComparison.InvariantCultureIgnoreCase))
            {
                result.Messages.Add(ValidationMessage.Success(ctx, fn => fn.Measure!, measure, "Measure matches", propValue));
                return true;
            }
            else
            {
                result.Messages.Add(ValidationMessage.Failure(ctx, fn => fn.Measure!, measure, "Invalid Measure", propValue));
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
    }
}
