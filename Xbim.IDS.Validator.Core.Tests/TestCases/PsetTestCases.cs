using FluentAssertions;
using Xbim.Common.Step21;
using Xbim.IO.Xml.BsConf;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class PsetTestCases : BaseTest
    {
        public PsetTestCases(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestCases/property/pass-a_name_check_will_match_any_property_with_any_string_value.ids")]
        [InlineData(@"TestCases/property/pass-a_name_check_will_match_any_quantity_with_any_value.ids")]
        [InlineData(@"TestCases/property/pass-a_number_specified_as_a_string_is_treated_as_a_string.ids")]
        [InlineData(@"TestCases/property/pass-a_property_set_to_false_is_still_considered_a_value_and_will_pass_a_name_check.ids")]
        [InlineData(@"TestCases/property/pass-a_property_set_to_true_will_pass_a_name_check.ids")]
        [InlineData(@"TestCases/property/pass-a_required_facet_checks_all_parameters_as_normal.ids")]
        [InlineData(@"TestCases/property/pass-a_zero_duration_will_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/property/pass-all_matching_properties_must_satisfy_requirements_1_3.ids")]
        [InlineData(@"TestCases/property/pass-all_matching_properties_must_satisfy_requirements_2_3.ids")]
        [InlineData(@"TestCases/property/pass-all_matching_property_sets_must_satisfy_requirements_1_3.ids")]
        [InlineData(@"TestCases/property/pass-all_matching_property_sets_must_satisfy_requirements_3_3.ids")]
        [InlineData(@"TestCases/property/pass-an_optional_facet_always_passes_regardless_of_outcome_1_2.ids")]
        [InlineData(@"TestCases/property/pass-an_optional_facet_always_passes_regardless_of_outcome_2_2.ids")]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_a_bounded_property_will_pass_1_4.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_a_bounded_property_will_pass_2_4.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_a_bounded_property_will_pass_3_4.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_a_list_property_will_pass_1_3.ids")]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_a_list_property_will_pass_2_3.ids")]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_a_table_property_will_pass_1_3.ids")]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_a_table_property_will_pass_2_3.ids")]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_an_enumerated_property_will_pass_1_3.ids")]
        [InlineData(@"TestCases/property/pass-any_matching_value_in_an_enumerated_property_will_pass_2_3.ids")]
        [InlineData(@"TestCases/property/pass-booleans_must_be_specified_as_uppercase_strings_2_3.ids")]
        [InlineData(@"TestCases/property/pass-dates_are_treated_as_strings_1_2.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/property/pass-durations_are_treated_as_strings_1_2.ids", XbimSchemaVersion.Ifc4)]
        // Fix 1e_6 precision
        //[InlineData(@"TestCases/property/pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_1_4.ids")]
        //[InlineData(@"TestCases/property/pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_2_4.ids")]
        [InlineData(@"TestCases/property/pass-if_multiple_properties_are_matched__all_values_must_satisfy_requirements_1_2.ids")]
        [InlineData(@"TestCases/property/pass-integer_values_are_checked_using_type_casting_1_4.ids")]
        [InlineData(@"TestCases/property/pass-integer_values_are_checked_using_type_casting_2_4.ids")]
        [InlineData(@"TestCases/property/pass-integer_values_are_checked_using_type_casting_3_4.ids")]
        [InlineData(@"TestCases/property/pass-measures_are_used_to_specify_an_ifc_data_type_2_2.ids")]
        [InlineData(@"TestCases/property/pass-non_ascii_characters_are_treated_without_encoding.ids")]
        [InlineData(@"TestCases/property/pass-only_specifically_formatted_numbers_are_allowed_3_4.ids")]
        [InlineData(@"TestCases/property/pass-only_specifically_formatted_numbers_are_allowed_4_4.ids")]
        //[InlineData(@"TestCases/property/pass-predefined_properties_are_supported_but_discouraged_1_2.ids")]
        [InlineData(@"TestCases/property/pass-properties_can_be_inherited_from_the_type_1_2.ids")]
        [InlineData(@"TestCases/property/pass-properties_can_be_inherited_from_the_type_2_2.ids")]
        [InlineData(@"TestCases/property/pass-properties_can_be_overriden_by_an_occurrence_1_2.ids")]
        [InlineData(@"TestCases/property/pass-real_values_are_checked_using_type_casting_1_3.ids")]
        [InlineData(@"TestCases/property/pass-real_values_are_checked_using_type_casting_2_3.ids")]
        [InlineData(@"TestCases/property/pass-real_values_are_checked_using_type_casting_3_3.ids")]
        [InlineData(@"TestCases/property/pass-specifying_a_value_performs_a_case_sensitive_match_1_2.ids")]
        [InlineData(@"TestCases/property/pass-unit_conversions_shall_take_place_to_ids_nominated_standard_units_2_2.ids")]

        [Theory]
        public void EntityTestPass(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach(var schema in GetSchemas(schemas))
            {
                var outcome = VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
            
        }


        [InlineData(@"TestCases/property/pass-predefined_properties_are_supported_but_discouraged_1_2.ids")]
        [Theory(Skip = "To implement IFCDOORPANELPROPERTIES edgecase")]
        public void PassesToImplement(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }


        [InlineData(@"TestCases/property/fail-a_logical_unknown_is_considered_falsey_and_will_not_pass.ids")]
        [InlineData(@"TestCases/property/fail-a_prohibited_facet_returns_the_opposite_of_a_required_facet.ids")]
        [InlineData(@"TestCases/property/fail-all_matching_properties_must_satisfy_requirements_3_3.ids")]
        [InlineData(@"TestCases/property/fail-all_matching_property_sets_must_satisfy_requirements_2_3.ids")]
        [InlineData(@"TestCases/property/fail-an_empty_string_is_considered_falsey_and_will_not_pass.ids")]
        [InlineData(@"TestCases/property/fail-any_matching_value_in_a_bounded_property_will_pass_4_4.ids")]
        [InlineData(@"TestCases/property/fail-any_matching_value_in_a_list_property_will_pass_3_3.ids")]
        [InlineData(@"TestCases/property/fail-any_matching_value_in_a_table_property_will_pass_3_3.ids")]
        [InlineData(@"TestCases/property/fail-any_matching_value_in_an_enumerated_property_will_pass_3_3.ids")]
        [InlineData(@"TestCases/property/fail-booleans_must_be_specified_as_uppercase_strings_1_3.ids")]
        [InlineData(@"TestCases/property/fail-booleans_must_be_specified_as_uppercase_strings_3_3.ids")]
        [InlineData(@"TestCases/property/fail-complex_properties_are_not_supported_1_2.ids")]
        [InlineData(@"TestCases/property/fail-complex_properties_are_not_supported_2_2.ids")]
        [InlineData(@"TestCases/property/fail-dates_are_treated_as_strings_2_2.ids")]
        [InlineData(@"TestCases/property/fail-durations_are_treated_as_strings_1_2.ids")]
        [InlineData(@"TestCases/property/fail-elements_with_a_matching_pset_but_no_property_also_fail.ids")]
        [InlineData(@"TestCases/property/fail-elements_with_no_properties_always_fail.ids")]
        [InlineData(@"TestCases/property/fail-floating_point_numbers_are_compared_with_a_1e_6_tolerance_3_4.ids")]
        [InlineData(@"TestCases/property/fail-floating_point_numbers_are_compared_with_a_1e_6_tolerance_4_4.ids")]
        [InlineData(@"TestCases/property/fail-ids_does_not_handle_string_truncation_such_as_for_identifiers.ids")]
        [InlineData(@"TestCases/property/fail-if_multiple_properties_are_matched__all_values_must_satisfy_requirements_2_2.ids")]
        [InlineData(@"TestCases/property/fail-integer_values_are_checked_using_type_casting_4_4.ids")]
        
        [InlineData(@"TestCases/property/fail-measures_are_used_to_specify_an_ifc_data_type_1_2.ids")]
        [InlineData(@"TestCases/property/fail-only_specifically_formatted_numbers_are_allowed_1_4.ids")]

        [InlineData(@"TestCases/property/fail-only_specifically_formatted_numbers_are_allowed_2_4.ids")]
        [InlineData(@"TestCases/property/fail-properties_can_be_overriden_by_an_occurrence_2_2.ids")]
        [InlineData(@"TestCases/property/fail-properties_with_a_null_value_fail.ids")]
        [InlineData(@"TestCases/property/fail-quantities_must_also_match_the_appropriate_measure.ids")]
        [InlineData(@"TestCases/property/fail-reference_properties_are_treated_as_objects_and_not_supported.ids")]
        [InlineData(@"TestCases/property/fail-specifying_a_value_fails_against_different_values.ids")]
        [InlineData(@"TestCases/property/fail-specifying_a_value_performs_a_case_sensitive_match_2_2.ids")]
        [InlineData(@"TestCases/property/fail-unit_conversions_shall_take_place_to_ids_nominated_standard_units_1_2.ids")]
        [Theory]
        public void EntityTestFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = VerifyIdsFile(idsFile);

                outcome.Status.Should().Be(ValidationStatus.Fail, schema.ToString());
            }
        }

        [InlineData(@"TestCases/property/fail-predefined_properties_are_supported_but_discouraged_2_2.ids")]
        [Theory(Skip= "Implement IFCDOORPANELPROPERTIES edge case")]
        public void FailuresToImplement(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }
    }
}
