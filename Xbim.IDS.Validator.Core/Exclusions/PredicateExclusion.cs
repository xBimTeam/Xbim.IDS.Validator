using System;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Exclusions
{
    /// <summary>
    /// An exclusion policy using function predicates to filter out entities from testing
    /// </summary>
    /// <remarks>Enables programatic filtering against the current IDS <see cref="Specification"/> and xbim <see cref="IPersistEntity"/> objects</remarks>
    public class PredicateExclusion : ISpecificationExclusion
    {
        /// <summary>
        /// Constructs a new <see cref="PredicateExclusion"/> with the provided filter predicate
        /// </summary>
        /// <param name="exclusionPredicate"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PredicateExclusion(Func<Specification, IPersistEntity, bool> exclusionPredicate)
        {
            ExclusionPredicate = exclusionPredicate ?? throw new ArgumentNullException(nameof(exclusionPredicate));
        }

        public string PolicyType => "Predicate";

        protected Func<Specification, IPersistEntity, bool> ExclusionPredicate { get; }

        //<inheritDocs>
        public bool IsEntityMatching(Specification spec, IPersistEntity entity)
        {
            return ExclusionPredicate(spec, entity);
        }

        
    }
}
