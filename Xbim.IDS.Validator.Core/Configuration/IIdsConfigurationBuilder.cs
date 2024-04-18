using Microsoft.Extensions.DependencyInjection;

namespace Xbim.IDS.Validator.Core.Configuration
{
    /// <summary>
    /// An interface for configuring the xbim IDS system
    /// </summary>
    public interface IIdsConfigurationBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/>
        /// </summary>
        IServiceCollection Services { get; }
    }
}
