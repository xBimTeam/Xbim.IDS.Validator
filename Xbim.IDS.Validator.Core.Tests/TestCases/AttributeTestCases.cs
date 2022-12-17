using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class AttributeTestCases : BaseTest
    {
        public AttributeTestCases(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestCases/attribute/fail-a_prohibited_facet_returns_the_opposite_of_a_required_facet.ids")]
        
        [InlineData(@"TestCases/attribute/fail-attributes_are_not_inherited_by_the_occurrence.ids")]
        [InlineData(@"TestCases/attribute/fail-attributes_should_check_strings_case_sensitively_2_2.ids")]
        [InlineData(@"TestCases/attribute/fail-attributes_with_a_logical_unknown_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-attributes_with_an_empty_list_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-attributes_with_an_empty_set_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-attributes_with_empty_strings_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-attributes_with_null_values_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-booleans_must_be_specified_as_uppercase_strings_1_3.ids")]
        [InlineData(@"TestCases/attribute/fail-booleans_must_be_specified_as_uppercase_strings_2_3.ids")]
        [InlineData(@"TestCases/attribute/fail-dates_are_treated_as_strings_1_2.ids")]
        [InlineData(@"TestCases/attribute/fail-derived_attributes_cannot_be_checked_and_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-durations_are_treated_as_strings_2_2.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/fail-floating_point_numbers_are_compared_with_a_1e_6_tolerance_3_4.ids")]
        [InlineData(@"TestCases/attribute/fail-floating_point_numbers_are_compared_with_a_1e_6_tolerance_4_4.ids")]
        [InlineData(@"TestCases/attribute/fail-ids_does_not_handle_string_truncation_such_as_for_identifiers.ids")]
        [InlineData(@"TestCases/attribute/fail-invalid_attribute_names_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-inverse_attributes_cannot_be_checked_and_always_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-numeric_values_are_checked_using_type_casting_4_4.ids")]
        [InlineData(@"TestCases/attribute/fail-only_specifically_formatted_numbers_are_allowed_1_4.ids")]
        // TODO: Investigate why 1234.5 = "123,4.5"
        //[InlineData(@"TestCases/attribute/fail-only_specifically_formatted_numbers_are_allowed_2_4.ids")]
        [InlineData(@"TestCases/attribute/fail-specifying_a_float_when_the_value_is_an_integer_will_fail.ids")]
        [InlineData(@"TestCases/attribute/fail-value_checks_always_fail_for_lists.ids")]
        [InlineData(@"TestCases/attribute/fail-value_checks_always_fail_for_objects.ids")]
        [InlineData(@"TestCases/attribute/fail-value_checks_always_fail_for_selects.ids")]
        [InlineData(@"TestCases/attribute/fail-value_restrictions_may_be_used_3_3.ids")]


        [Theory]
        public void EntityTestFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Failed, schema.ToString());
            }
        }

        [InlineData(@"TestCases/attribute/pass-a_required_facet_checks_all_parameters_as_normal.ids")]
        // Awaiting Support of Optional Cardinality (min=0, max =1)
        //[InlineData(@"TestCases/attribute/pass-an_optional_facet_always_passes_regardless_of_outcome_1_2.ids")]
        //[InlineData(@"TestCases/attribute/pass-an_optional_facet_always_passes_regardless_of_outcome_2_2.ids")]
        [InlineData(@"TestCases/attribute/pass-attributes_referencing_an_object_should_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-attributes_should_check_strings_case_sensitively_1_2.ids")]
        [InlineData(@"TestCases/attribute/pass-attributes_with_a_boolean_false_should_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-attributes_with_a_boolean_true_should_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-attributes_with_a_select_referencing_a_primitive_should_pass.ids")]
        [InlineData(@"TestCases/attribute/pass-attributes_with_a_select_referencing_an_object_should_pass.ids")]
        [InlineData(@"TestCases/attribute/pass-attributes_with_a_string_value_should_pass.ids")]
        [InlineData(@"TestCases/attribute/pass-attributes_with_a_zero_duration_should_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-attributes_with_a_zero_number_have_meaning_and_should_pass.ids")]
        // IfcTask changed in IFC4
        [InlineData(@"TestCases/attribute/pass-booleans_must_be_specified_as_uppercase_strings_2_3.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-dates_are_treated_as_strings_1_2.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-durations_are_treated_as_strings_1_2.ids", XbimSchemaVersion.Ifc4)]
        // Awaiting Tolerance support
        //[InlineData(@"TestCases/attribute/pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_1_4.ids")]
        //[InlineData(@"TestCases/attribute/pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_2_4.ids")]
        [InlineData(@"TestCases/attribute/pass-globalids_are_treated_as_strings_and_not_expanded.ids")]
        
        // TODO: Fix Long-Int cast issue in XIDS
        // StairFlight.NumberOfRiser[s] got renamed in IFC4
        [InlineData(@"TestCases/attribute/pass-integers_follow_the_same_rules_as_numbers.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-integers_follow_the_same_rules_as_numbers_2_2.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/attribute/pass-name_restrictions_will_match_any_result_1_3.ids")]
        [InlineData(@"TestCases/attribute/pass-name_restrictions_will_match_any_result_2_3.ids")]
        //[InlineData(@"TestCases/attribute/pass-name_restrictions_will_match_any_result_3_3.ids")]
        [InlineData(@"TestCases/attribute/pass-non_ascii_characters_are_treated_without_encoding.ids")]
        [InlineData(@"TestCases/attribute/pass-numeric_values_are_checked_using_type_casting_1_4.ids")]
        [InlineData(@"TestCases/attribute/pass-numeric_values_are_checked_using_type_casting_2_4.ids")]
        [InlineData(@"TestCases/attribute/pass-numeric_values_are_checked_using_type_casting_3_4.ids")]
        [InlineData(@"TestCases/attribute/pass-only_specifically_formatted_numbers_are_allowed_3_4.ids")]
        [InlineData(@"TestCases/attribute/pass-only_specifically_formatted_numbers_are_allowed_4_4.ids")]
        // Fix Long/decimal implicit cast issues
        //[InlineData(@"TestCases/attribute/pass-strict_numeric_checking_may_be_done_with_a_bounds_restriction.ids")]
        [InlineData(@"TestCases/attribute/pass-typecast_checking_may_also_occur_within_enumeration_restrictions.ids")]
        [InlineData(@"TestCases/attribute/pass-value_restrictions_may_be_used_1_3.ids")]
        [InlineData(@"TestCases/attribute/pass-value_restrictions_may_be_used_2_3.ids")]
        [Theory]
        public void EntityTestPass(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Success, schema.ToString());
            }
        }

        [InlineData(@"TestCases/attribute/pass-an_optional_facet_always_passes_regardless_of_outcome_1_2.ids")]
        [InlineData(@"TestCases/attribute/pass-an_optional_facet_always_passes_regardless_of_outcome_2_2.ids")]
        [InlineData(@"TestCases/attribute/pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_1_4.ids")]
        [InlineData(@"TestCases/attribute/pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_2_4.ids")]
        [InlineData(@"TestCases/attribute/pass-integers_follow_the_same_rules_as_numbers.ids")]
        [InlineData(@"TestCases/attribute/pass-integers_follow_the_same_rules_as_numbers_2_2.ids")]
        [InlineData(@"TestCases/attribute/pass-strict_numeric_checking_may_be_done_with_a_bounds_restriction.ids")]
        [Theory(Skip = "To fix")]
        public void ToFix(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Success);

        }
    }
}
