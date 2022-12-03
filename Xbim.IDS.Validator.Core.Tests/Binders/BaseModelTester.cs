using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Ifc;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public abstract class BaseModelTester
    {
        protected static IModel model;
        protected IfcQuery query;

        private readonly ITestOutputHelper output;

        public BaseModelTester(ITestOutputHelper output)
        {
            this.output = output;
            query = new IfcQuery();
        }

        static BaseModelTester()
        {
            model = BuildModel();
        }

        private static IModel BuildModel()
        {
            var filename = @"TestModels\SampleHouse4.ifc";
            return IfcStore.Open(filename);
        }

        internal ILogger<IdsModelBinderTests> GetXunitLogger()
        {
            var services = new ServiceCollection()
                        .AddLogging((builder) => builder.AddXunit(output,
                        new Divergic.Logging.Xunit.LoggingConfig { LogLevel = LogLevel.Debug }));
            IServiceProvider provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<IdsModelBinderTests>>();
            Assert.NotNull(logger);
            return logger;
        }


        public enum ConstraintType
        {
            Exact,
            Pattern,
            Range,
            Structure
        }
    }
}
