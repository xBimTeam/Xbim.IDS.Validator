using Microsoft.Extensions.Logging;
using Xbim.Common;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IIdsModelValidator
    {
        IIdsModelBinder ModelBinder { get; }

        /// <summary>
        /// Runs the IDS validation file against the supplier model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="idsFile"></param>
        /// <param name="logger"></param>
        /// <param name="verificationOptions"></param>
        /// <returns></returns>
        ValidationOutcome ValidateAgainstIds(IModel model, string idsFile, ILogger logger, VerificationOptions? verificationOptions = default);
    }
}