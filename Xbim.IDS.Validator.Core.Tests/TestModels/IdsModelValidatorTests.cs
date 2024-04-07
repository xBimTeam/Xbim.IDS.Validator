using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestModels
{
    [Collection(nameof(TestEnvironment))]
    public class IdsModelValidatorTests
    {
        private readonly ITestOutputHelper output;

        private readonly IServiceProvider provider;

        public IdsModelValidatorTests(ITestOutputHelper output)
        {
            this.output = output;
            
            provider = TestEnvironment.ServiceProvider;
        }

        [Fact]
        public async Task Can_ValidateModels()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\Example.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);


            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger);

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);
            results.ExecutedRequirements.Should().NotBeEmpty();

            results.ExecutedRequirements.Count().Should().Be(4);

            results.ExecutedRequirements[0].Status.Should().Be(ValidationStatus.Pass); 
            results.ExecutedRequirements[0].ApplicableResults.Should().NotBeEmpty();


        }
        [Fact]
        public async Task Can_ValidateModelsAgainstXids()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\Example.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);


            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();
            var idsMigrator = provider.GetRequiredService<IIdsSchemaMigrator>();

            // Apply migrations
            idsMigrator.MigrateToIdsSchemaVersion(idsScript, out var upgraded);

            var idsSpec = Xbim.InformationSpecifications.Xids.LoadBuildingSmartIDS(upgraded.Root, logger);

            var results = await idsValidator.ValidateAgainstXidsAsync(model, idsSpec, logger);

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);
            results.ExecutedRequirements.Should().NotBeEmpty();

            results.ExecutedRequirements.Count().Should().Be(4);

            results.ExecutedRequirements[0].Status.Should().Be(ValidationStatus.Pass);
            results.ExecutedRequirements[0].ApplicableResults.Should().NotBeEmpty();


        }


        private static IModel BuildModel(string ifcFile)
        {
            return IfcStore.Open(ifcFile);
        }

    }
}
