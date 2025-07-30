using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Exclusions;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

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

        public async Task Can_ValidateModelsWithGlobalGuidExceptions()
        {
            var specNo = 5;
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\BasicRequirements1-0.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);

            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

            string[] ifcGuids = new[] { "3cUkl32yn9qRSPvBJVyWYp" };
            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions: 
                new VerificationOptions 
                {
                    EntityExclusions = new List<ISpecificationExclusion>() { new GuidExclusion(ifcGuids) },
                    OutputFullEntity = true,
                });

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);
            results.ExecutedRequirements[specNo].ApplicableResults.Should().HaveCount(3);

            results.ExecutedRequirements[specNo].ApplicableResults.Where(r => r.ValidationStatus == ValidationStatus.Skipped).Should().HaveCount(1);
            var skipped = results.ExecutedRequirements[specNo].ApplicableResults.First(r => r.ValidationStatus == ValidationStatus.Skipped);
            skipped.Messages.Should().HaveCount(1);
            skipped.Messages[0].ToString().Should().Be("[Skipped] Exception made using Guid policy");
            skipped.Messages[0].Status.Should().Be(ValidationStatus.Skipped);
            (skipped.FullEntity as IIfcRoot).GlobalId.Value.Should().Be("3cUkl32yn9qRSPvBJVyWYp");
        }

        // Spec named "Doors are Named correctly" (with ID 12.34) asserts that all IfcDoors are named 'Door_Int' which two pass, while the external door fails.
        // Test the result of spec if we skip different doors against different specs.
        [InlineData("3cUkl32yn9qRSPvBJVyWYp", "", ValidationStatus.Pass)] // Skip Failing door on all specs =>    Doors are Named correctly: ✔️
        [InlineData("3cUkl32yn9qRSPvBJVyWaG", "", ValidationStatus.Fail, 1)] // Skip a good door on all specs =>  Doors are Named correctly: ❌
        [InlineData("3cUkl32yn9qRSPvBJVyWYp", "Doors are Named correctly", ValidationStatus.Pass)]  // Door Fails but excluded on this spec by name => Doors are Named correctly: ✔️
        [InlineData("3cUkl32yn9qRSPvBJVyWYp", "12.34", ValidationStatus.Pass)]  // Door Fails but excluded on this test by Spec identifier  =>    Doors are Named correctly: ✔️
        [InlineData("3cUkl32yn9qRSPvBJVyWYp", "A,B,C,Doors are Named correctly", ValidationStatus.Pass)] // As above, but skip on hypothetical Specs, A,B,C
        [InlineData("3cUkl32yn9qRSPvBJVyWaG", "Doors are Named correctly", ValidationStatus.Fail, 1)]  // Skip a passing door => Doors are Named correctly: ❌
        [InlineData("3cUkl32yn9qRSPvBJVyWYp", "Spaces are Defined", ValidationStatus.Fail, 0)] // Failing door not excluded another spec => Doors are Named correctly: ❌
        [InlineData("3cUkl32yn9qRSPvBJVyWYp,3cUkl32yn9qRSPvBJVyWaG,3cUkl32yn9qRSPvBJVyWax", "12.34", ValidationStatus.Pass, 3)]  // All Doors skipped on test
        [InlineData("Missing", "", ValidationStatus.Fail, 0)]   // Skip non existing door
        [Theory]

        public async Task Can_ValidateModelsWithSpecGuidExceptions(string ifcGuidCsv, string specsCsv, ValidationStatus expectedStatus, int skippedCount = 1)
        {
            var specNo = 5;
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\BasicRequirements1-0.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);

            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();
            var guids = ifcGuidCsv.Split(",");
            var exclusions = new ExcludedElementDictionary();
            foreach (var guid in guids)
            {
                exclusions.Add(guid, specsCsv.Split(",").ToList());
            }
            
            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions:
                new VerificationOptions
                {
                    EntityExclusions = new List<ISpecificationExclusion>() { new GuidExclusion(exclusions) },
                    OutputFullEntity = true,
                });

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);

            results.ExecutedRequirements[specNo].Status.Should().Be(expectedStatus);
            results.ExecutedRequirements[specNo].ApplicableResults.Should().HaveCount(3);

            results.ExecutedRequirements[specNo].ApplicableResults.Where(r => r.ValidationStatus == ValidationStatus.Skipped).Should().HaveCount(skippedCount);
            if(skippedCount > 0)
            { 
                var skipped = results.ExecutedRequirements[specNo].ApplicableResults.First(r => r.ValidationStatus == ValidationStatus.Skipped);
                skipped.Messages.Should().HaveCount(1);
            }
        }

        [Fact]
        public async Task Can_ValidateModelsWithPredicateExceptions()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\BasicRequirements1-0.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);

            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

            var predicate = (Specification spec, IPersistEntity ent) => ent is IIfcRoot root && root.Name == "3 - Entrance hall";

            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions:
                new VerificationOptions
                {
                    EntityExclusions = new List<ISpecificationExclusion>() { new PredicateExclusion(predicate) },
                    OutputFullEntity = true,
                });

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);
            results.ExecutedRequirements[4].ApplicableResults.Should().HaveCount(4);

            results.ExecutedRequirements[4].ApplicableResults.Where(r => r.ValidationStatus == ValidationStatus.Skipped).Should().HaveCount(1);
            var skipped = results.ExecutedRequirements[4].ApplicableResults.First(r => r.ValidationStatus == ValidationStatus.Skipped);
            skipped.Messages.Should().HaveCount(1);
            skipped.Messages[0].ToString().Should().Be("[Skipped] Exception made using Predicate policy");
            skipped.Messages[0].Status.Should().Be(ValidationStatus.Skipped);
            (skipped.FullEntity as IIfcRoot).Name.ToString().Should().Be("3 - Entrance hall");
        }

        [Fact]
        public async Task Can_ValidateModelsWithFacetExceptions()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\BasicRequirements1-0.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);


            var idsValidator = provider.GetRequiredService<IIdsModelValidator>();
            var modelBinder = provider.GetRequiredService<IIdsModelBinder>();

#pragma warning disable CS0618 // Type or member is obsolete
            var idsExclusion = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            AttributeFacet facet = new AttributeFacet()
            {
                AttributeName = "Name",
                AttributeValue = new ValueConstraint("3 - Entrance hall")
            };
            idsExclusion.Facets.Add(facet);
            idsExclusion.RequirementOptions = new System.Collections.ObjectModel.ObservableCollection<RequirementCardinalityOptions>
            {
                new RequirementCardinalityOptions(facet, Cardinality.Optional)
            };

            var results = await idsValidator.ValidateAgainstIdsAsync(model, idsScript, logger, verificationOptions:
                new VerificationOptions
                {
                    EntityExclusions = new List<ISpecificationExclusion>() { new FacetsExclusion(idsExclusion, model, modelBinder) },
                    OutputFullEntity = true,
                });

            results.Should().NotBeNull();

            results.Status.Should().Be(ValidationStatus.Fail);
            results.ExecutedRequirements[4].ApplicableResults.Should().HaveCount(4);

            results.ExecutedRequirements[4].ApplicableResults.Where(r => r.ValidationStatus == ValidationStatus.Skipped).Should().HaveCount(1);
            var skipped = results.ExecutedRequirements[4].ApplicableResults.First(r => r.ValidationStatus == ValidationStatus.Skipped);
            skipped.Messages.Should().HaveCount(1);
            skipped.Messages[0].ToString().Should().Be("[Skipped] Exception made using IDS Facet policy");
            skipped.Messages[0].Status.Should().Be(ValidationStatus.Skipped);
            (skipped.FullEntity as IIfcRoot).Name.ToString().Should().Be("3 - Entrance hall");
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
