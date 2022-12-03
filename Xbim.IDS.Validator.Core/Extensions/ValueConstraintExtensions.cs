using Microsoft.Extensions.Logging;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Extensions
{
    public static class ValueConstraintExtensions
    {
        public static bool SatisfiesRequirement(this ValueConstraint constraint, FacetGroup requirement, object? candidateValue, ILogger? logger = null)
        {
            var cardinality = requirement.RequirementOptions?.FirstOrDefault();
            var expectation = cardinality != RequirementCardinalityOptions.Prohibited;
            
            
            return constraint.IsSatisfiedBy(candidateValue, logger) == expectation; 
        }


        public static RequirementCardinalityOptions? GetCardinality(this FacetGroup requirement)
        {
            return requirement.RequirementOptions?.FirstOrDefault();
        }

        public static bool IsRequired(this FacetGroup requirement)
        {
            return requirement.GetCardinality() != RequirementCardinalityOptions.Prohibited;
        }

        public static bool IsOptional(this FacetGroup requirement)
        {
            return requirement.GetCardinality() == null;
        }
    }
}
