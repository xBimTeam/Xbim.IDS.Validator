using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Exclusions
{
    /// <summary>
    /// A simple exclusion policy based on matching the <see cref="IIfcRoot.GlobalId"/> by a supplied set of IfcGuids
    /// </summary>
    public class GuidExclusion : ISpecificationExclusion
    {
        private readonly IDictionary<string, List<string>> ifcGuids;

        /// <summary>
        /// Construct a <see cref="GuidExclusion"/> with the given IfcGuid
        /// </summary>
        /// <param name="ifcGuids">A list of 22 character IfcGuids</param>
        public GuidExclusion(IEnumerable<string> ifcGuids)
        {
            this.ifcGuids = ifcGuids.ToDictionary(k => k, v => new List<string>()); // new Dictionary<string, IList<string>>();
        }

        public GuidExclusion(ExcludedElementDictionary ifcGuids)
        {
            this.ifcGuids = ifcGuids;
        }

        public string PolicyType => "Guid";

        //<inheritDocs>
        public bool IsEntityMatching(Specification spec, IPersistEntity entity)
        {
            // Exclude entity where guid matches and either a) specification matches, or b) no specification defined (i.e. global exclusion)
            return entity is IIfcRoot root && ifcGuids.TryGetValue(root.GlobalId, out var specs) && SpecificationMatches(specs, spec);
        }

        private bool SpecificationMatches(List<string> applicableSpecs, Specification spec)
        {
            if(!applicableSpecs.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                return true;    // All specifications if unstated - global
            }
            // Matches the ID or a name
            return applicableSpecs.Any(specId => spec.Guid == specId || spec.Name == specId);
        }
    }

    /// <summary>
    /// A dictionary of IfcGuids and the sets of specifications (by spec Name or Identifier) to exclude them from
    /// </summary>
    public class ExcludedElementDictionary : Dictionary<string, List<string>>
    {

    }
}
