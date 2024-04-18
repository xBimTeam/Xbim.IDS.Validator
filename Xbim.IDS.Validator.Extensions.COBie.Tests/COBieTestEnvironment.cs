using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common.Configuration;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Configuration;
using Xbim.IDS.Validator.Tests.Common;

namespace Xbim.IDS.Validator.Extensions.COBie.Tests
{
    /// <summary>
    /// A test environment configured for testing COBie with IDS
    /// </summary>
    public class COBieTestEnvironment : BaseTestEnvironment
    {

        [CollectionDefinition(nameof(COBieTestEnvironment))]
        public class xUnitBootstrap : ICollectionFixture<COBieTestEnvironment>
        {
            // Just to bootStrap COBieTestEnvironment
        }

        public override void InitialiseEnvironment(IServiceCollection services)
        {
            services.AddXbimToolkit()
                    .AddIdsValidation(cfg => 
                        cfg.AddCOBie()  // Load COBie Extensions
                        )
                    .AddLogging(s => s.SetMinimumLevel(LogLevel.Debug));
        }
    }

}