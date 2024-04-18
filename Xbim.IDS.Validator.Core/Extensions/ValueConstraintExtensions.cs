using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Extensions
{
    public static class ValueConstraintExtensions
    {


        /// <summary>
        /// Evaluates a candidate value against a constraint, accounting for the facet cardinality
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constraint"></param>
        /// <param name="candidateValue"></param>
        /// <param name="ctx"></param>
        /// <param name="logger"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [return: NotNull]
        public static bool ExpectationIsSatisifedBy<T>([NotNullWhen(true)]this ValueConstraint? constraint, object candidateValue, ValidationContext<T> ctx, ILogger? logger = null, bool caseSensitive = false) where T: IFacet
        {
            var expectation = ctx.FacetCardinality switch
            {
                Cardinality.Expected => true,
                Cardinality.Prohibited => false,
                Cardinality.Optional => true,  // Check
                
                _ => throw new NotImplementedException()
            };
            return constraint?.IsSatisfiedBy(candidateValue, caseSensitive, logger) == expectation;
        }

        /// <summary>
        /// Determines if the Constraint is null, empty or otherwise contains a null or empty string
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this ValueConstraint? constraint)
        {
            if (constraint == null)
                return true;
            if (constraint.IsEmpty())
                return true;
            return constraint.IsSingleExact(out object? val) && val is string s && string.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// Determines if the Constraint is just a Regex Wildcard (.*), indicating the field is not important for the specification
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        public static bool IsWildcard(this ValueConstraint? constraint)
        {
            // Handles an edge case where the user does not want to specify the manadatory Pset explicitly
            // A wild-card is saying I don't care about that field.
            if (constraint == null)
                return false;
            if (constraint.IsEmpty())
                return false;
            if (constraint.AcceptedValues == null || constraint.AcceptedValues!.Count != 1)
            {
                return false;
            }

            PatternConstraint? patternConstraint = constraint.AcceptedValues.FirstOrDefault() as PatternConstraint;
            if (patternConstraint == null)
            {
                return false;
            }
            if(patternConstraint.Pattern != ".*")
            {
                return false;
            }
          
            return true;
        }

        public static Cardinality? GetCardinality(this FacetGroup requirement, int idx)
        {
            if(requirement.RequirementOptions == null)
            {
                // Workaround for Options being null when any facet is invalid. Expected is the default
                requirement.RequirementOptions = new System.Collections.ObjectModel.ObservableCollection<RequirementCardinalityOptions>(requirement.Facets.Select(f => new RequirementCardinalityOptions(f, Cardinality.Expected)));
            }
            if(requirement.RequirementOptions.Count > idx)
            {
                return requirement.RequirementOptions?[idx]?.RelatedFacetCardinality;
            }
            else
            {
                return Cardinality.Expected;
            }
        }

        public static Cardinality? GetCardinality(this FacetGroup requirement, IFacet facet)
        {
            var idx = requirement.Facets.IndexOf(facet);
            if (idx != -1)
            {
                return GetCardinality(requirement, idx);
            }
            throw new ArgumentOutOfRangeException(nameof(facet));
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
                    case Cardinality.Expected: return true;
                    case Cardinality.Prohibited: return false;
                }
            }
            return null;
        }

        public static bool IsOptional(this FacetGroup requirement, IFacet currentFacet)
        {
            var idx = requirement.Facets.IndexOf(currentFacet);
            if (idx != -1)
            {
                return requirement.GetCardinality(idx) == Cardinality.Optional;
            }
            return true;
        }

        /// <summary>
        /// Determines whether any object satisified the constraint, accounting for unwrapping the value of a complex object using a mapper
        /// </summary>
        /// <param name="constraint"></param>
        /// <param name="value"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static bool SatisfiesConstraint(this ValueConstraint constraint, object value, IValueMapper mapper)
        {
            if (value == null)
            {
                return constraint.IsSatisfiedBy(value);
            }
            
            var valueType = value.GetType();
            var isNullWrapped = TypeHelper.IsNullable(valueType) && TypeHelper.IsValueType(valueType);
            var underlyingType = isNullWrapped ? Nullable.GetUnderlyingType(valueType) : valueType;

            if (TypeHelper.IsCollection(underlyingType))
            {
                throw new NotSupportedException("Collections not supported");
            }
            if (value.GetType().IsEnum)
            {
                return constraint.IsSatisfiedBy(value?.ToString(), true);
            }

            // Unpack objects to obtain a value - the ValueConstraint default implementation reverts to ToString() which may be inappropriate.
           
            if(mapper == null)
            {
                return false;
            }
            // We have a value mapper which provides an extensibility point for extension schemas. E.g COBie
            if(mapper.MapValue(value, out var unpacked))
            {
                return constraint.IsSatisfiedBy(unpacked, true);
            }
            else
            {
                throw new NotImplementedException($"Filtering on type {value?.GetType()?.Name} not implemented");
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
