using FluentAssertions;
using Xbim.Ifc4.RepresentationResource;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class IdsTestCases : BaseTest
    {
        public IdsTestCases(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestCases/ids/pass-a_minimal_ids_can_check_a_minimal_ifc_2_2.ids")]
        [InlineData(@"TestCases/ids/pass-a_prohibited_specification_and_a_prohibited_facet_results_in_a_double_negative.ids")]
        [InlineData(@"TestCases/ids/pass-a_specification_passes_only_if_all_requirements_pass_2_2.ids")]
        [InlineData(@"TestCases/ids/pass-multiple_specifications_are_independent_of_one_another.ids")]
        [InlineData(@"TestCases/ids/pass-optional_specifications_may_still_pass_if_nothing_is_applicable.ids")]
        [InlineData(@"TestCases/ids/pass-prohibited_specifications_fail_if_at_least_one_entity_passes_all_requirements_1_3.ids")]
        [InlineData(@"TestCases/ids/pass-prohibited_specifications_fail_if_at_least_one_entity_passes_all_requirements_2_3.ids")]
        [InlineData(@"TestCases/ids/pass-required_specifications_need_at_least_one_applicable_entity_1_2.ids")]
        [InlineData(@"TestCases/ids/pass-specification_optionality_and_facet_optionality_can_be_combined.ids")]
        [InlineData(@"TestCases/ids/pass-specification_version_is_purely_metadata_and_does_not_impact_pass_or_fail_result.ids")]
        [Theory]
        public void EntityTestPass(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }


        [InlineData(@"TestCases/ids/fail-a_minimal_ids_can_check_a_minimal_ifc_1_2.ids")]
        [InlineData(@"TestCases/ids/fail-a_specification_passes_only_if_all_requirements_pass_1_2.ids")]
        [InlineData(@"TestCases/ids/fail-prohibited_specifications_fail_if_at_least_one_entity_passes_all_requirements_3_3.ids")]
        [InlineData(@"TestCases/ids/fail-required_specifications_need_at_least_one_applicable_entity_2_2.ids")]
        [Theory]
        public void EntityTestFailures(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Fail);
        }
    }
}
