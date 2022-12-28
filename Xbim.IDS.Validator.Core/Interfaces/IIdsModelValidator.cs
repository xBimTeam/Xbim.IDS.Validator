using Microsoft.Extensions.Logging;
using Xbim.Common;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IIdsModelValidator
    {
        IIdsModelBinder ModelBinder { get; }

        ValidationOutcome ValidateAgainstIds(IModel model, string idsFile, ILogger logger);
    }
}