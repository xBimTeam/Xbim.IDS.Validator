using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.IDS.Validator.Tests.Common;
using Xbim.Ifc;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests
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

            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions: new VerificationOptions { PermittedIdsAuditStatuses = VerificationOptions.Relaxed });

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

            var idsSpec = InformationSpecifications.Xids.LoadBuildingSmartIDS(upgraded.Root, logger);

            var results = await idsValidator.ValidateAgainstXidsAsync(model, idsSpec, logger);

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);
            results.ExecutedRequirements.Should().NotBeEmpty();

            results.ExecutedRequirements.Count().Should().Be(4);

            results.ExecutedRequirements[0].Status.Should().Be(ValidationStatus.Pass);
            results.ExecutedRequirements[0].ApplicableResults.Should().NotBeEmpty();


        }

        [Fact]
        public async Task Supports_Specs_Without_requirements()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\SpecWithoutRequirements.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);

            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions: new VerificationOptions { PermittedIdsAuditStatuses = VerificationOptions.Relaxed });

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Pass);
            results.ExecutedRequirements.Should().NotBeEmpty();

            var firstReq = results.ExecutedRequirements.First();

            firstReq.Status.Should().Be(ValidationStatus.Pass);
            firstReq.PassedResults.Should().NotBeEmpty();
            firstReq.FailedResults.Should().BeEmpty();
        }

        [Fact]
        public async Task Supports_Specs_Without_requirements_fails()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\SpecWithoutRequirements-Fail.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);

            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions: new VerificationOptions { PermittedIdsAuditStatuses = VerificationOptions.Relaxed });

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);
            results.ExecutedRequirements.Should().NotBeEmpty();
            var firstReq = results.ExecutedRequirements.First();

            firstReq.Status.Should().Be(ValidationStatus.Fail);
            firstReq.PassedResults.Should().BeEmpty();
            firstReq.FailedResults.Should().BeEmpty();
        }



        private static IModel BuildModel(string ifcFile)
        {
            return IfcStore.Open(ifcFile);
        }

    }
}
