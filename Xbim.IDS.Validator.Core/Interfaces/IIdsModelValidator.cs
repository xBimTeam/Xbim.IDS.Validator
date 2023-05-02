using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xbim.Common;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IIdsModelValidator
    {
        IIdsModelBinder ModelBinder { get; }
        
        [Obsolete("Prefer async method: ValidateAgainstIdsAsync")]
        /// <summary>
        /// Runs the IDS validation file against the supplied model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="idsFile"></param>
        /// <param name="logger"></param>
        /// <param name="verificationOptions"></param>
        /// <returns>The <see cref="ValidationOutcome"/></returns>
        ValidationOutcome ValidateAgainstIds(IModel model, string idsFile, ILogger logger, VerificationOptions? verificationOptions = default);

        /// <summary>
        /// Runs the IDS validation file asynchronously against the supplied model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="idsFile"></param>
        /// <param name="logger"></param>
        /// <param name="requirementCompleted">Callback invoked when a requirement has been executed</param>
        /// <param name="verificationOptions"></param>
        /// <returns></returns>
        Task<ValidationOutcome> ValidateAgainstIdsAsync(IModel model, string idsFile, ILogger logger, Action<ValidationRequirement>? requirementCompleted = default, VerificationOptions? verificationOptions = default);
    }
}