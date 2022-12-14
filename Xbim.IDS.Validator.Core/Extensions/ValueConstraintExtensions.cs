using System.Diagnostics;
using System.Linq.Expressions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Extensions
{
    public static class ValueConstraintExtensions
    {

        public static RequirementCardinalityOptions? GetCardinality(this FacetGroup requirement, int idx)
        {
            if(requirement.RequirementOptions == null)
            {
                // Workaround for Options being null when any facet is invalid. Expected is the default
                requirement.RequirementOptions = new System.Collections.ObjectModel.ObservableCollection<RequirementCardinalityOptions>(requirement.Facets.Select(f => RequirementCardinalityOptions.Expected));
            }
            return requirement.RequirementOptions?[idx];
        }

        /// <summary>
        /// Determines whether this facet rule is Required, Prohibited or Optional
        /// </summary>
        /// <param name="requirement"></param>
        /// <param name="currentFacet"></param>
        /// <returns>true if Required;false if Prohibited; null if Optional</returns>
        public static bool? IsRequired(this FacetGroup requirement, IFacet currentFacet)
        {
            // Requirements cardinality is not stored on the applicable factet, but as a separate collection on Facet Group to requirements with the same ordinal
            var idx = requirement.Facets.IndexOf(currentFacet);
            if(idx != -1)
            {
                switch (requirement.GetCardinality(idx))
                {
                    case RequirementCardinalityOptions.Expected: return true;
                    case RequirementCardinalityOptions.Prohibited: return false;
                }
            }
            return null;
        }

        public static bool IsOptional(this FacetGroup requirement, IFacet currentFacet)
        {
            var idx = requirement.Facets.IndexOf(currentFacet);
            if (idx != -1)
            {
                return requirement.GetCardinality(idx) == null;
            }
            return true;
        }

        public static bool SatisifesConstraint(this ValueConstraint constraint, object value)
        {


            if (value == null)
            {
                return constraint.IsSatisfiedBy(value);
            }
            var valueType = value.GetType();
            var isNullWrapped = TypeHelper.IsNullable(valueType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(valueType) : valueType;

            if (TypeHelper.IsCollection(underlyingType))
            {
                throw new NotSupportedException("Collections not supported");
            }
            else if (value.GetType().IsEnum)
            {
                return constraint.IsSatisfiedBy(value?.ToString(), true);
            }

            // TODO: handle IFC2x3
            // Wrap simple navigation objects to use built-in equality operators
            else if (value is Ifc4.MeasureResource.IfcLabel label)
            {
                return constraint.IsSatisfiedBy(label.Value, true);
            }
            else if (value is Ifc4.MeasureResource.IfcText text)
            {
                return constraint.IsSatisfiedBy(text.Value, true);
            }
            else if (value is Ifc4.UtilityResource.IfcGloballyUniqueId guid)
            {
                return constraint.IsSatisfiedBy(guid.Value, true);
            }
            else if (value is Ifc4.MeasureResource.IfcIdentifier id)
            {
                return constraint.IsSatisfiedBy(id.Value, true);
            }
            else if (value is Ifc4.MeasureResource.IfcCountMeasure cnt)
            {
                return constraint.IsSatisfiedBy(cnt.Value, true);
            }
            else if (value is Ifc4.MeasureResource.IfcLengthMeasure len)
            {
                return constraint.IsSatisfiedBy(len.Value, true);
            }
            else if (value is Ifc4.MeasureResource.IfcAreaMeasure area)
            {
                return constraint.IsSatisfiedBy(area.Value, true);
            }
            else if (value is Ifc4.MeasureResource.IfcVolumeMeasure vol)
            {
                return constraint.IsSatisfiedBy(vol.Value, true);
            }
            else if (value is Ifc4.DateTimeResource.IfcDate date)
            {
                return constraint.IsSatisfiedBy(date.Value, true);
            }
            else if (value is Ifc4.DateTimeResource.IfcDuration interval)
            {
                return constraint.IsSatisfiedBy(interval.Value, true);
            }
            
            //else if (underlyingType == typeof(string))
            //{
            //    queryValue = Expression.Constant(ifcAttributeValue);
            //}
            
            else
            {
                throw new NotImplementedException($"Filtering on Ifc type {value?.GetType()?.Name} not implemented");
            }

        }

        /// <summary>
        /// Only for debug usage
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string SingleValue(this ValueConstraint constraint)
        {

            var first = constraint?.AcceptedValues?.Single();
            Debug.Assert(first != null);

            switch (first)
            {
                case ExactConstraint ec:
                    return ec.Value;

                case PatternConstraint pc:
                    return pc.Pattern;

                case RangeConstraint rc:
                    return rc.ToString();

                case ValueConstraint vc:
                    return vc.ToString();

                default:
                    throw new NotImplementedException(first.GetType().Name);
            }

        }
    }
}
