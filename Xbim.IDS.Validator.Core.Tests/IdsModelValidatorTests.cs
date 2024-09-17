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

        [Fact]
        public async Task Can_Detokenise()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\TokenisedSpec.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);

            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();
            var options = new VerificationOptions { PermittedIdsAuditStatuses = VerificationOptions.AnyState };
            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions: options);

            results.Status.Should().Be(ValidationStatus.Fail, "Project name won't match");
            results.IdsDocument.SpecificationsGroups.First().Author.Should().Match("{{*}}", "Author is tokenised");

            // Act
            options.RuntimeTokens["ProjectName"] = "Project Number";
            options.RuntimeTokens["Author"] = "info@xbim.net";

            results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions: options);
            //Asssert
            results.Status.Should().Be(ValidationStatus.Pass, "Token value replaced to match Model");
            results.IdsDocument.SpecificationsGroups.First().Author.Should().Be("info@xbim.net");

            results.IdsDocument.SpecificationsGroups.First().Milestone.Should().Be("{{Milestone}}", "Not token provided");

        }



        private static IModel BuildModel(string ifcFile)
        {
            return IfcStore.Open(ifcFile);
        }

    }
}
