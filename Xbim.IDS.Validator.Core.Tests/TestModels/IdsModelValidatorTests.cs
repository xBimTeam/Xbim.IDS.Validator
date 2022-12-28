using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestModels
{
    public class IdsModelValidatorTests
    {
        private readonly ITestOutputHelper output;

        private readonly IServiceProvider provider;

        public IdsModelValidatorTests(ITestOutputHelper output)
        {
            this.output = output;
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging().AddIdsValidation();

            provider = serviceCollection.BuildServiceProvider();
        }

        [Fact]
        public void Can_ValidateModels()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\Example.ids";

            var model = BuildModel(modelFile);

            var logger = GetXunitLogger();


            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

            var results = idsValidator.ValidateAgainstIds(model, idsScript, logger);

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Failed);
            results.ExecutedRequirements.Should().NotBeEmpty();

            results.ExecutedRequirements.Count().Should().Be(4);

            results.ExecutedRequirements[0].Status.Should().Be(ValidationStatus.Success); 
            results.ExecutedRequirements[0].ApplicableResults.Should().NotBeEmpty();


        }



        private static IModel BuildModel(string ifcFile)
        {
            return IfcStore.Open(ifcFile);
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
    }
}
