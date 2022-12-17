using FluentAssertions;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class RestrictionTestCases : BaseTest
    {
        public RestrictionTestCases(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestCases/restriction/pass-a_bound_can_be_inclusive_1_4.ids")]
        [InlineData(@"TestCases/restriction/pass-a_bound_can_be_inclusive_2_3.ids")]
        [InlineData(@"TestCases/restriction/pass-a_bound_can_be_inclusive_2_4.ids")]
        [InlineData(@"TestCases/restriction/pass-a_bound_can_be_inclusive_3_4.ids")]
        [InlineData(@"TestCases/restriction/pass-an_enumeration_matches_case_sensitively_1_3.ids")]
        [InlineData(@"TestCases/restriction/pass-an_enumeration_matches_case_sensitively_2_3.ids")]
        [InlineData(@"TestCases/restriction/pass-length_checks_can_be_used_1_2.ids")]
        [InlineData(@"TestCases/restriction/pass-max_and_min_length_checks_can_be_used_2_3.ids")]
        [InlineData(@"TestCases/restriction/pass-max_and_min_length_checks_can_be_used_3_3.ids")]
        [InlineData(@"TestCases/restriction/pass-regex_patterns_can_be_used_1_3.ids")]
        [InlineData(@"TestCases/restriction/pass-regex_patterns_can_be_used_2_3.ids")]

        [Theory]
        public void EntityTestPass(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Success);
        }



        [InlineData(@"TestCases/restriction/fail-a_bound_can_be_inclusive_1_3.ids")]
        [InlineData(@"TestCases/restriction/fail-a_bound_can_be_inclusive_3_3.ids")]
        [InlineData(@"TestCases/restriction/fail-a_bound_can_be_inclusive_4_4.ids")]
        [InlineData(@"TestCases/restriction/fail-an_enumeration_matches_case_sensitively_1_3.ids")]
        [InlineData(@"TestCases/restriction/fail-an_enumeration_matches_case_sensitively_3_3.ids")]
        [InlineData(@"TestCases/restriction/fail-length_checks_can_be_used_1_2.ids")]
        [InlineData(@"TestCases/restriction/fail-max_and_min_length_checks_can_be_used_1_3.ids")]
        [InlineData(@"TestCases/restriction/fail-max_and_min_length_checks_can_be_used_4_3.ids")]
        [InlineData(@"TestCases/restriction/fail-patterns_always_fail_on_any_number.ids")]
        [InlineData(@"TestCases/restriction/fail-patterns_only_work_on_strings_and_nothing_else.ids")]
        [InlineData(@"TestCases/restriction/fail-regex_patterns_can_be_used_3_3.ids")]
        [Theory]
        public void EntityTestFailures(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Failed);
        }
    }
}
