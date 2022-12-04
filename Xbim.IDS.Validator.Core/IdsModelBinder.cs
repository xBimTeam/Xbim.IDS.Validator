using Microsoft.Extensions.Logging;
using System.Collections;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.InformationSpecifications;


namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Binds an IDS to an <see cref="IModel"/>
    /// </summary>
    public class IdsModelBinder
    {
        private readonly IModel model;
        private IfcQuery ifcQuery;

        private PsetFacetBinder psetFacetBinder;
        private AttributeFacetBinder attrFacetBinder;
        private IfcTypeFacetBinder ifcTypeFacetBinder;

        public IdsModelBinder(IModel model)
        {
            this.model = model;
            ifcQuery = new IfcQuery();
            psetFacetBinder = new PsetFacetBinder(model);
            attrFacetBinder = new AttributeFacetBinder(model);
            ifcTypeFacetBinder = new IfcTypeFacetBinder(model);
        }

        

        /// <summary>
        /// Returns all entities in the model that apply to a specification
        /// </summary>
        /// <param name="facets"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IEnumerable<IPersistEntity> SelectApplicableEntities(Specification spec)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            var facets = spec.Applicability.Facets;
           
            var ifcFacet = facets.OfType<IfcTypeFacet>().FirstOrDefault();
            if(ifcFacet == null)
            {
                throw new InvalidOperationException("Expected a single IfcTypeFacet");
            }
            //var expressType = facetBinder.GetExpressType(ifcFacet);
            var expression = BindFilters(ifcQuery.InstancesExpression, ifcFacet);

            foreach (var facet in facets.Except(new[] { ifcFacet }))
            {
                expression = BindFilters(expression, facet);
            }

            return ifcQuery.Execute(expression, model);
        }

   
        /// <summary>
        /// Validate an IFC entity meets its requirements against the defined Constraints
        /// </summary>
        /// <param name="item"></param>
        /// <param name="facet"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public IdsValidationResult ValidateRequirement(IPersistEntity item, FacetGroup requirement, IFacet facet, ILogger? logger)
        {
            
            var result = new IdsValidationResult()
            {
                ValidationStatus = ValidationStatus.Inconclusive,
                Entity = item
            };
            switch (facet)
            {
                case IfcTypeFacet f:
                    
                    ValidateIfcType(item, requirement, logger, result, f);
                    break;
                    

                case IfcPropertyFacet pf:
                    
                    ValidateProperty(item, requirement, logger, result, pf);
                    break;
                    

                case AttributeFacet af:
                    
                    ValidateAttribute(item, requirement, logger, result, af);
                    break;


                default:
                    logger.LogWarning("Skipping unimplemented validation {type}", facet.GetType().Name);
                    break;
                    //throw new NotImplementedException($"Validation of Facet not implemented: '{facet.GetType().Name}' - {facet.Short()}");
            }
            if(result.Failures.Any())
            {
                result.ValidationStatus = ValidationStatus.Failed;
            }
            else if(result.Successful.Any())
            {
                result.ValidationStatus = ValidationStatus.Success;
            }
            return result;
        }

        private void ValidateAttribute(IPersistEntity item, FacetGroup requirement, ILogger? logger, IdsValidationResult result, AttributeFacet af)
        {
            var candidates = attrFacetBinder.GetAttributes(item, af);

            foreach (var pair in candidates)
            {
                var attrName = pair.Key;
                var attrvalue = pair.Value;
                bool isPopulated = IsValueRelevant(attrvalue);
                // Name meets requirement if it has a value and is Required. Treat unknown logical as no value
                if (af.AttributeName.SatisfiesRequirement(requirement, attrName, logger) && (requirement.IsRequired() == isPopulated))
                {
                    result.Successful.Add("AttributeName " + attrName + " was " + af.AttributeName.Short());
                }
                else
                {
                    result.Failures.Add("AttributeName " + attrName + " was not " + af!.AttributeName.Short() + " - " + af!.AttributeName.ToString());

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
                        result.Successful.Add("AttributeValue " + attrvalue?.ToString() + " was " + af.AttributeValue!.Short());
                    else
                        result.Failures.Add("AttributeValue " + attrvalue?.ToString() + " was not " + af.AttributeValue.ToString() + " - " + af.AttributeValue?.Short());
                }

            }
        }

        private void ValidateProperty(IPersistEntity item, FacetGroup requirement, ILogger? logger, IdsValidationResult result, IfcPropertyFacet pf)
        {
           
            
            var psets = psetFacetBinder.GetPropertySetsMatching(item.EntityLabel, pf.PropertySetName, logger);
            if (psets.Any())
            {
                foreach (var pset in psets)
                {
                    var props = psetFacetBinder.GetPropertiesMatching<IIfcPropertySingleValue>(item.EntityLabel, pset.Name, pf.PropertyName);
                    var quants = psetFacetBinder.GetQuantitiesMatching(item.EntityLabel, pset.Name, pf.PropertyName);
                    if (props.Any() || quants.Any())
                    {
                        foreach (var prop in props)
                        {
                            var propValue = prop.NominalValue;
                            ValidateMeasure(result, propValue, pf.Measure);
                            object? value = UnwrapValue(propValue);
                            bool isPopulated = IsValueRelevant(value);
                            if (requirement.IsRequired() == isPopulated)
                            {
                                result.Successful.Add("PropertyName Present " + pset.Name + "." + prop.Name + " was " + pf!.PropertyName?.Short());
                            }
                            else
                            {
                                result.Failures.Add("PropertyName Absent " + pset.Name + "." + prop.Name + " was not " + pf!.PropertyName?.Short());
                            }

                            ValidatePropertyValue(requirement, logger, result, pf, value);
                        }
                        foreach (var quant in quants)
                        {
                            var propValue = UnwrapQuantity(quant);
                            ValidateMeasure(result, propValue, pf.Measure);
                            object? value = UnwrapValue(propValue);
                            bool isPopulated = IsValueRelevant(value);

                            if (requirement.IsRequired() == isPopulated)
                            {
                                result.Successful.Add("PropertyName Present " + pset.Name + "." + quant.Name + " was " + pf!.PropertyName?.Short());
                            }
                            else
                            {
                                result.Failures.Add("PropertyName Absent " + pset.Name + "." + quant.Name + " was not " + pf!.PropertyName?.Short());
                            }

                            ValidatePropertyValue(requirement, logger, result, pf, value);
                        }
                    }
                    else
                    {
                        result.Failures.Add("PropertyName was not matched " + pf!.PropertyName!.Short() + " - " + pf!.PropertyName.ToString());
                    }
                }
            }

            else
            {
                result.Failures.Add("PropertySet was not matched " + pf!.PropertySetName!.Short() + " - " + pf!.PropertySetName.ToString());
            }

            
        }

        private void ValidateMeasure(IdsValidationResult result, IIfcValue propValue, string? expectedMeasure)
        {
            if (propValue is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(expectedMeasure)) return;

            var measure = model.Metadata.ExpressType(propValue).Name;
            if (measure.Equals(expectedMeasure, StringComparison.InvariantCultureIgnoreCase))
            {
                result.Successful.Add("Measure " + measure + " was " + expectedMeasure);
            }
            else

            {
                result.Failures.Add("Measure " + measure + " was not " + expectedMeasure);
            }
        }

        private bool ValidatePropertyValue(FacetGroup requirement, ILogger? logger, IdsValidationResult result, IfcPropertyFacet pf, object? value)
        {
            if (pf.PropertyValue != null)
            {
                value = ApplyWorkarounds(value);
                if (pf.PropertyValue.SatisfiesRequirement(requirement, value, logger))
                {
                    result.Successful.Add("PropertyValue "  + value?.ToString() + " was " + pf.PropertyValue!.Short());
                    return true;
                }
                else
                {
                    result.Failures.Add("PropertyValue " + value?.ToString() + " was not " + pf.PropertyValue.ToString() + " - " + pf.PropertyValue?.Short());
                    return false;
                }
            }
            return true;

         
        }

        private void ValidateIfcType(IPersistEntity item, FacetGroup requirement, ILogger? logger,  IdsValidationResult result, IfcTypeFacet f)
        {
            var entityType = model.Metadata.ExpressType(item);
            if (entityType == null)
            {
                result.Failures.Add($"Invalid IFC Type '{item.GetType().Name}'");
            }
            var actual = entityType?.Name.ToUpperInvariant();

            if (f?.IfcType?.SatisfiesRequirement(requirement, actual, logger) == true)
            {
                result.Successful.Add("IfcType '" + actual + "' was " + f.IfcType.Short());
            }
            else
            {
                result.Failures.Add("IfcType '" + actual + "' was not " + f?.IfcType?.Short());
            }
            if (f?.PredefinedType?.HasAnyAcceptedValue() == true)
            {
                var preDefValue = ifcTypeFacetBinder.GetPredefinedType(item);
                if (f!.PredefinedType.SatisfiesRequirement(requirement, preDefValue, logger) == true)
                {
                    result.Successful.Add("PredefinedType '" + preDefValue + "' was " + f.PredefinedType.Short());
                }
                else
                {
                    result.Failures.Add("PredefinedType '" + preDefValue + "' was not " + f?.PredefinedType?.Short());
                }
            }
        }

        private object? GetPsetValue(IPersistEntity item, IfcPropertyFacet pf)
        {
            var propValue = psetFacetBinder.GetProperty(item.EntityLabel, pf.PropertySetName.SingleValue(), pf.PropertyName.SingleValue());
            object? value = UnwrapValue(propValue);

            if (value == null)
            {
                // Try Quantities
                var quantity = psetFacetBinder.GetQuantity(item.EntityLabel, pf.PropertySetName.SingleValue(),
                    pf.PropertyName.SingleValue());
                if (quantity != null)
                    value = UnwrapQuantity(quantity)?.Value;
            }

            return value;
        }

        private object? ApplyWorkarounds([MaybeNull]object? value)
        {
            // Workaround for a bug in XIDS Satisfied test where we don't coerce numeric types correctly
            if (value is long l)
                return Convert.ToDouble(l);


            if (value is int i)
                return Convert.ToDouble(i);

            return value;
        }

        private IIfcValue? UnwrapQuantity(IIfcPhysicalQuantity quantity)
        {
            if (quantity is IIfcQuantityCount c)
                return c.CountValue;
            if (quantity is IIfcQuantityArea area)
                return area.AreaValue;
            else if (quantity is IIfcQuantityLength l)
                return l.LengthValue;
            else if (quantity is IIfcQuantityVolume v)
                return v.VolumeValue;
            if (quantity is IIfcQuantityWeight w)
                return w.WeightValue;
            if (quantity is IIfcQuantityTime t)
                return t.TimeValue;
            if (quantity is IIfcPhysicalComplexQuantity comp)
                return default;


            throw new NotImplementedException(quantity.GetType().Name);
        }

        private object UnwrapValue(IIfcValue? value)
        {
            object result = HandleBoolConventions(value);
            if(result is IIfcMeasureValue)
            {
                result = HandleUnitConversion(value);
            }
            if (result is IIfcValue v)
            {
                result = v.Value;
            }

            return result;
        }

        private IIfcValue HandleUnitConversion(IIfcValue value)
        {
            var units = psetFacetBinder.GetUnits() as IfcUnitAssignment;

            if (units == null) return value;

            // TODO: handle 2x3
            if (value is IfcCountMeasure c)
                return c;
            if (value is IfcAreaMeasure area)
            {
                var unit = units.AreaUnit;
                if(unit is IIfcSIUnit si)
                {
                    return new IfcAreaMeasure(area * si.Power);
                }
                return area;
            }
            else if (value is IfcLengthMeasure l)
            {
                var unit = units.LengthUnit;
                if (unit is IIfcSIUnit si)
                {
                    return new IfcLengthMeasure(l * si.Power);
                }
                return l;
            }
            else if (value is IfcVolumeMeasure v)
            {
                var unit = units.VolumeUnit;
                if (unit is IIfcSIUnit si)
                {
                    return new IfcMassMeasure(v * si.Power);
                }
                return v;
            }

            //if (value is IfcMassMeasure w)
            //{
            //    var unit = units.GetUnitFor(w);
            //    if (unit is IIfcSIUnit si)
            //    {
            //        return new IfcMassMeasure(v * si.Power);
            //    }
            //    return w;
            //}

            if (value is IfcTimeMeasure t)
                return t;
            else
                return value;
                //throw new NotImplementedException(value.GetType().Name);
        }

        private static object HandleBoolConventions(object attrvalue)
        {
            if (attrvalue is IExpressBooleanType ifcbool)
            {
                // IDS Specs expect bools to be upper case
                attrvalue = ifcbool.Value.ToString().ToUpperInvariant();
            }

            return attrvalue;
        }

        private static bool IsValueRelevant(object? value)
        {
            if (value == null) return false;
            if (value is IfcSimpleValue sv && string.IsNullOrEmpty(sv.Value?.ToString())) return false;
            if (value is string str && string.IsNullOrEmpty(str)) return false;
            if (value is IList list && list.Count == 0) return false;

            return true;
        }

        /// <summary>
        /// Binds an <see cref="IFacet"/> to an Expression bound to filter on IModel.Instances
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private Expression BindFilters(Expression baseExpression, IFacet facet)
        {
            switch (facet)
            {
                case IfcTypeFacet f:
                    return ifcTypeFacetBinder.BindFilterExpression(baseExpression, f);

                case AttributeFacet af:
                    return attrFacetBinder.BindFilterExpression(baseExpression, af);

                case IfcPropertyFacet pf:

                    return psetFacetBinder.BindFilterExpression(baseExpression, pf);

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
    }
}
