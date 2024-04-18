using Microsoft.Extensions.DependencyInjection;

namespace Xbim.IDS.Validator.Core.Configuration
{
    internal class IdsConfigurationBuilder : IIdsConfigurationBuilder
    {
        /// <summary>
        /// Constructs a new <see cref="IdsConfigurationBuilder"/>
        /// </summary>
        /// <param name="services"></param>
        public IdsConfigurationBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
