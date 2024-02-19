using Xbim.IDS.Validator.Core;

namespace Xbim.IDS.Validator.Common.Interfaces
{
    /// <summary>
    /// Interface enabling options to be passed to an object
    /// </summary>
    public interface ISupportOptions
    {
        /// <summary>
        /// Sets the options
        /// </summary>
        /// <param name="options"></param>
        void SetOptions(VerificationOptions options);
    }
}
