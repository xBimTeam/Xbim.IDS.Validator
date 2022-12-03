using Divergic.Logging.Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.InformationSpecifications;
using Xunit;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{

    public class IfcTypeTestCases
    {
        private readonly ITestOutputHelper output;

        private readonly ILogger logger;

        public IfcTypeTestCases(ITestOutputHelper output)
        {
            this.output = output;
            logger = GetXunitLogger();
        }

        [InlineData(@"TestCases/entity/fail-a_null_predefined_type_should_always_fail_a_specified_predefined_types.ids")]
        [InlineData(@"TestCases/entity/fail-a_predefined_type_from_an_enumeration_must_be_uppercase.ids")]
        [InlineData(@"TestCases/entity/fail-a_predefined_type_must_always_specify_a_meaningful_type__not_userdefined_itself.ids")]
        [InlineData(@"TestCases/entity/fail-an_entity_not_matching_a_specified_predefined_type_will_fail.ids")]
        [InlineData(@"TestCases/entity/fail-an_entity_not_matching_the_specified_class_should_fail.ids")]
        [InlineData(@"TestCases/entity/fail-entities_can_be_specified_as_a_xsd_regex_pattern_1_2.ids")]
        [InlineData(@"TestCases/entity/fail-entities_can_be_specified_as_an_enumeration_3_3.ids")]
        [InlineData(@"TestCases/entity/fail-entities_must_be_specified_as_uppercase_strings.ids")]
        [InlineData(@"TestCases/entity/fail-invalid_entities_always_fail.ids")]
        [InlineData(@"TestCases/entity/fail-restrictions_an_be_specified_for_the_predefined_type_3_3.ids")]
        [InlineData(@"TestCases/entity/fail-subclasses_are_not_considered_as_matching.ids")]
        [InlineData(@"TestCases/entity/fail-user_defined_types_are_checked_case_sensitively.ids")]

        [Theory]
        public void EntityTestFailures(string idsFile)
        {
            List<IdsValidationResult> results = VerifyIdsFile(idsFile);
            results.Where((IdsValidationResult r) => r.Failures.Any()).Should().NotBeEmpty("");
        }

        [InlineData("TestCases/entity/pass-a_matching_entity_should_pass.ids")]
        [InlineData("TestCases/entity/pass-a_matching_predefined_type_should_pass.ids")]
        [InlineData("TestCases/entity/pass-a_predefined_type_may_specify_a_user_defined_element_type.ids")]
        [InlineData("TestCases/entity/pass-a_predefined_type_may_specify_a_user_defined_object_type.ids")]
        [InlineData("TestCases/entity/pass-a_predefined_type_may_specify_a_user_defined_process_type.ids")]
        [InlineData("TestCases/entity/pass-an_matching_entity_should_pass_regardless_of_predefined_type.ids")]
        [InlineData("TestCases/entity/pass-entities_can_be_specified_as_a_xsd_regex_pattern_2_2.ids")]
        [InlineData("TestCases/entity/pass-entities_can_be_specified_as_an_enumeration_1_3.ids")]
        [InlineData("TestCases/entity/pass-entities_can_be_specified_as_an_enumeration_2_3.ids")]
        [InlineData("TestCases/entity/pass-inherited_predefined_types_should_pass.ids")]
        [InlineData("TestCases/entity/pass-overridden_predefined_types_should_pass.ids")]
        [InlineData("TestCases/entity/pass-restrictions_an_be_specified_for_the_predefined_type_1_3.ids")]
        [InlineData("TestCases/entity/pass-restrictions_an_be_specified_for_the_predefined_type_2_3.ids")]
        [Theory]
        public void EntityTestPass(string idsFile)
        {
            List<IdsValidationResult> results = VerifyIdsFile(idsFile);
            ((IEnumerable<IdsValidationResult>)results).Should().NotBeEmpty("Expect at least one result");
            results.Where((IdsValidationResult r) => r.Failures.Any()).Should().BeEmpty("");
        }

        private List<IdsValidationResult> VerifyIdsFile(string idsFile)
        {
            string ifcFile = Path.ChangeExtension(idsFile, "ifc");
            IfcStore model = IfcStore.Open(ifcFile);
            Xids ids = Xids.LoadBuildingSmartIDS(idsFile, logger);
            IdsModelBinder modelBinder = new IdsModelBinder(model);
            List<IdsValidationResult> results = new List<IdsValidationResult>();
            foreach (Specification spec in ids.AllSpecifications())
            {
                IEnumerable<IPersistEntity> applicable = modelBinder.SelectApplicableEntities(spec);
                foreach (IFacet req in spec.Requirement!.Facets)
                {
                    foreach (IPersistEntity entity in applicable)
                    {
                        IdsValidationResult result = modelBinder.ValidateRequirement(entity, req, logger);
                        results.Add(result);
                    }
                }
            }
            foreach (IdsValidationResult res in results)
            {
                LogLevel logLevel = LogLevel.Information;
                if (res.ValidationStatus == ValidationStatus.Failed)
                {
                    logLevel = LogLevel.Error;
                }
                if (res.ValidationStatus == ValidationStatus.Inconclusive)
                {
                    logLevel = LogLevel.Warning;
                }
                logger.Log(logLevel, "Entity {ent}", res.Entity?.EntityLabel);
                foreach (string pass in res.Successful)
                {
                    logger.LogInformation("  {message}", pass);
                }
                foreach (string fail in res.Failures)
                {
                    logger.LogError("  {error}", fail);
                }
            }
            return results;
        }

        internal ILogger GetXunitLogger()
        {
            IServiceCollection services = new ServiceCollection().AddLogging(delegate (ILoggingBuilder builder)
            {
                builder.AddXunit(output, new LoggingConfig
                {
                    LogLevel = LogLevel.Debug
                });
            });
            IServiceProvider provider = services.BuildServiceProvider();
            ILogger<IfcTypeTestCases> logger = provider.GetRequiredService<ILogger<IfcTypeTestCases>>();
            Assert.NotNull(logger);
            return logger;
        }
    }

}