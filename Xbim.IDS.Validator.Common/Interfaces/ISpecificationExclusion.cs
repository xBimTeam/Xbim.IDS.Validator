using System;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    /// <summary>
    /// Interface defining policies how applicable entities can be excluded from checking
    /// </summary>
    public interface ISpecificationExclusion
    {
        /// <summary>
        /// Determines if the entity matches and should be excluded from this specification
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="entity"></param>
        /// <returns><c>true</c> if the entity should be excluded from testing; otherwise <c>false</c></returns>
        bool IsEntityMatching(Specification spec, IPersistEntity entity);

        /// <summary>
        /// The type of the exclusion policy
        /// </summary>
        string PolicyType { get; }
    }
}
