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
    }
}
