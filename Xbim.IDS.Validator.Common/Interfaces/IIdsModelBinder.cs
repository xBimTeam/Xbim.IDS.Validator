using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IIdsModelBinder
    {
        void SetOptions(VerificationOptions options);
        IEnumerable<IPersistEntity> SelectApplicableEntities(IModel model, Specification spec);
        IdsValidationResult ValidateRequirement(IPersistEntity item, FacetGroup requirement, ILogger? logger);
    }
}