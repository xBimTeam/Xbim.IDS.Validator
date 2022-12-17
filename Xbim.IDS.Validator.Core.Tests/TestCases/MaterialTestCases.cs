using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class MaterialTestCases : BaseTest
    {
        public MaterialTestCases(ITestOutputHelper output) : base(output)
        {
        }

        // The following were not in IFC2x3: Categories, Profiles, Constituents, LayerNames
        [InlineData(@"TestCases/material/pass-a_material_category_may_pass_the_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-a_material_name_may_pass_the_value_check.ids")]
        [InlineData(@"TestCases/material/pass-a_required_facet_checks_all_parameters_as_normal.ids")]
        // Needs XIDS support Optional
        //[InlineData(@"TestCases/material/pass-an_optional_facet_always_passes_regardless_of_outcome_1_2.ids")]
        [InlineData(@"TestCases/material/pass-any_constituent_category_in_a_constituent_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_constituent_name_in_a_constituent_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_layer_category_in_a_layer_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_layer_name_in_a_layer_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_constituent_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_layer_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_list_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_profile_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_constituent_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_layer_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_list_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_profile_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_profile_category_in_a_profile_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-any_profile_name_in_a_profile_set_will_pass_a_value_check.ids", XbimSchemaVersion.Ifc4)]
        [InlineData(@"TestCases/material/pass-elements_with_any_material_will_pass_an_empty_material_facet.ids")]
        [InlineData(@"TestCases/material/pass-occurrences_can_inherit_materials_from_their_types.ids")]
        [InlineData(@"TestCases/material/pass-occurrences_can_override_materials_from_their_types.ids")]
        [Theory]
        public void EntityTestPass(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Success, schema.ToString());
            }
        }



        [InlineData(@"TestCases/material/fail-a_constituent_set_with_no_data_will_fail_a_value_check.ids")]
        [InlineData(@"TestCases/material/fail-a_material_list_with_no_data_will_fail_a_value_check.ids")]
        [InlineData(@"TestCases/material/fail-a_prohibited_facet_returns_the_opposite_of_a_required_facet.ids")]
        [InlineData(@"TestCases/material/fail-elements_without_a_material_always_fail.ids")]
        [InlineData(@"TestCases/material/fail-material_with_no_data_will_fail_a_value_check.ids")]
        [Theory]
        public void EntityTestFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Failed, schema.ToString());
            }
        }

    }
}
