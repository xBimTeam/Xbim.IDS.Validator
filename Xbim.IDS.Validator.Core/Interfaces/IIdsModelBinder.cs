using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IIdsModelBinder
    {
        IEnumerable<IPersistEntity> SelectApplicableEntities(IModel model, Specification spec);
        IdsValidationResult ValidateRequirement(IPersistEntity item, FacetGroup requirement, ILogger? logger);
    }
}