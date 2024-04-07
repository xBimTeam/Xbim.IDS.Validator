
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Cardinality Extensions
    /// </summary>
    public static class CardinalityExtensions
    {
        /// <summary>
        /// Builds Cardinality for a facet
        /// </summary>
        /// <param name="facet"></param>
        /// <param name="cardinality"></param>
        /// <returns></returns>
        public static RequirementCardinalityOptions BuildCardinality(this IFacet facet, RequirementCardinalityOptions.Cardinality cardinality = RequirementCardinalityOptions.Cardinality.Expected)
        {
            return new RequirementCardinalityOptions(facet, cardinality);

        }
    }
}
