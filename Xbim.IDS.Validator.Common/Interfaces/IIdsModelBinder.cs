using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    /// <summary>
    /// Interface defining how IDS specifications are executed against an IFC model using xbim
    /// </summary>
    public interface IIdsModelBinder
    {
        /// <summary>
        /// Sets the runtime options for this Biding
        /// </summary>
        /// <param name="options"></param>
        void SetOptions(VerificationOptions options);

        /// <summary>
        /// Selects the applicable xbim entities for the provided specification
        /// </summary>
        /// <param name="model"></param>
        /// <param name="spec"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        IEnumerable<IPersistEntity> SelectApplicableEntities(IModel model, Specification spec, ILogger logger);

        /// <summary>
        /// Verifies a specific xbim <see cref="IPersistEntity"/> against the IDS requirements
        /// </summary>
        /// <param name="item"></param>
        /// <param name="requirement"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        IdsValidationResult ValidateRequirement(IPersistEntity item, FacetGroup requirement, ILogger? logger);
    }
}