using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.IDS.Validator.Tests.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests
{
    [Collection(nameof(TestEnvironment))]
    public class IdsModelBinderTests
    {
        
        private readonly ITestOutputHelper output;
        private readonly IServiceProvider provider;

        public IdsModelBinderTests(ITestOutputHelper output)
        {
            this.output = output;

            provider = TestEnvironment.ServiceProvider;
        }


        [InlineData(@"TestModels\Example.ids", @"TestModels\SampleHouse4.ifc")]
        //[InlineData(@"TestModels\Example.ids", @"\\Mac\Home\Downloads\villa_tugenhat.ifc\villa_tugenhat_v1.ifc")]
        //InlineData(@"TestModels\BasicRequirements.ids", @"\\Mac\Home\Downloads\villa_tugenhat.ifc\villa_tugenhat_v1.ifc")]
        [InlineData(@"TestModels\BasicRequirements.ids", @"TestModels\SampleHouse4.ifc")]
        [Theory]

        public void Can_Bind_Specification_to_model(string idsFile, string ifcFile)
        {
            var model = BuildModel(ifcFile);
            var modelBinder = provider.GetRequiredService<IIdsModelBinder>();
            var logger = TestEnvironment.GetXunitLogger<IdsModelBinderTests>(output);
            var schemaLogger = TestEnvironment.GetXunitLogger<IdsSchemaMigrator>(output);
            var schemaUpgrader = new IdsSchemaMigrator(schemaLogger);
            schemaUpgrader.MigrateToIdsSchemaVersion(idsFile, out var upgraded, IdsLib.IdsSchema.IdsNodes.IdsVersion.Ids0_9_7);

            var idsSpec = Xids.LoadBuildingSmartIDS(upgraded.Root, logger);
            

            foreach(var group in idsSpec.SpecificationsGroups)
            {
                logger.LogInformation("opening '{group}'", group.Name);
                foreach(var spec in group.Specifications)
                {
                    logger.LogInformation(" -- Spec '{spec}' : versions {ifcVersions}", spec.Name, spec.IfcVersion);
                    var applicableIfc = spec.Applicability.Facets.OfType<IfcTypeFacet>().FirstOrDefault();
                    logger.LogInformation("    Applicable to : {entity} with PredefinedType {predefined}", applicableIfc.IfcType.Short(), applicableIfc.PredefinedType?.Short());
                    foreach(var applicableFacet in spec.Applicability.Facets)
                    {
                        logger.LogInformation("       - {facetType}: where {description} ", applicableFacet.GetType().Name, applicableFacet.Short() );
                    }
                    if(spec.Requirement != null)
                    {
                        logger.LogInformation("    Requirements {reqCount}: {expectation}", spec.Requirement?.Facets?.Count ?? 0, spec.Requirement?.RequirementOptions?.FirstOrDefault()?.ToString() ?? "" );
                        int idx = 1;
                        foreach (var reqFacet in spec.Requirement.Facets)
                        {
                            logger.LogInformation("       [{i}] {facetType}: check {description} ", idx++, reqFacet.GetType().Name, reqFacet.Short());
                        }
                    }
                    IEnumerable <IPersistEntity> items = modelBinder.SelectApplicableEntities(model, spec);
                    logger.LogInformation("          Checking {count} applicable items", items.Count());
                    foreach (var item in items)
                    {
                        var i = item as IIfcRoot;
                        logger.LogInformation("        * {ID}: {Type} {Name} ", item.EntityLabel, item.GetType().Name, i?.Name);

                        var result = modelBinder.ValidateRequirement(item, spec.Requirement, logger);
                        LogLevel level;
                        int pad;
                        GetLogLevel(result.ValidationStatus, out level, out pad);
                        logger.Log(level, "{pad}          {result}: Checking {short}", "".PadLeft(pad, ' '), result.ValidationStatus.ToString().ToUpperInvariant(), spec.Requirement.Short());
                        foreach (var message in result.Messages)
                        {
                            GetLogLevel(message.Status, out level, out pad);
                            logger.Log(level, "{pad}              #{entity} {message}", "".PadLeft(pad, ' '), item.EntityLabel, message.ToString());
                        }

                    }



                }
            }
        }

        [Fact]
        public void Should_Deduplicate_SelectedResults()
        {
            string modelFile = @"TestModels\SampleHouse4.ifc";
            string idsScript = @"TestModels\DuplicatedElements.ids";

            var model = BuildModel(modelFile);

            var logger = TestEnvironment.GetXunitLogger<IdsModelValidatorTests>(output);
            var idsSpec = Xbim.InformationSpecifications.Xids.LoadBuildingSmartIDS(idsScript, logger);
            var modelBinder = provider.GetRequiredService<IIdsModelBinder>();

            var spec = idsSpec.AllSpecifications().First();

            // Arrange
            var options = new VerificationOptions { IncludeSubtypes = true };
            modelBinder.SetOptions(options);

            // Act
            var results = modelBinder.SelectApplicableEntities(model, spec);

            // Assert
            results.Should().NotBeNull();

            results.Should().HaveCount(5); // Not 5 + 2 duplicates
        }


        private static void GetLogLevel(ValidationStatus status, out LogLevel level, out int pad)
        {
            level = LogLevel.Information;
            pad = 0;
            if (status == ValidationStatus.Inconclusive) { level = LogLevel.Warning; pad = 4; }
            if (status == ValidationStatus.Fail) { level = LogLevel.Error; pad = 6; }
        }


        private static IModel BuildModel(string ifcFile)
        {
            return IfcStore.Open(ifcFile);
        }

    }
}
