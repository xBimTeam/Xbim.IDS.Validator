using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if XbimV6
using Xbim.Common.Configuration;
#endif
using Xbim.IDS.Validator.Tests.Common;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class TestEnvironment : BaseTestEnvironment
    {

        [CollectionDefinition(nameof(TestEnvironment))]
        public class xUnitBootstrap : ICollectionFixture<TestEnvironment>
        {
            // Just to bootStrap TestEnvironment
        }

        public override void InitialiseEnvironment(IServiceCollection services)
        {
            services
#if XbimV6
                .AddXbimToolkit(/*c => c.AddMemoryModel()*/)
#endif
                .AddIdsValidation()
                .AddLogging(s => s.SetMinimumLevel(LogLevel.Debug));
        }
    }
}
