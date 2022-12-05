using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class MaterialTestCases : BaseTest
    {
        public MaterialTestCases(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestCases/material/pass-a_material_category_may_pass_the_value_check.ids")]
        [InlineData(@"TestCases/material/pass-a_material_name_may_pass_the_value_check.ids")]
        // Needs XIDS parser fix for missing constraint & Optional
        //[InlineData(@"TestCases/material/pass-a_required_facet_checks_all_parameters_as_normal.ids")]
        //[InlineData(@"TestCases/material/pass-an_optional_facet_always_passes_regardless_of_outcome_1_2.ids")]
        [InlineData(@"TestCases/material/pass-any_constituent_category_in_a_constituent_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_constituent_name_in_a_constituent_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_layer_category_in_a_layer_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_layer_name_in_a_layer_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_constituent_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_layer_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_list_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_category_in_a_profile_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_constituent_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_layer_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_list_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_material_name_in_a_profile_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_profile_category_in_a_profile_set_will_pass_a_value_check.ids")]
        [InlineData(@"TestCases/material/pass-any_profile_name_in_a_profile_set_will_pass_a_value_check.ids")]
        // Needs XIDS parser fix for missing constraint
        //[InlineData(@"TestCases/material/pass-elements_with_any_material_will_pass_an_empty_material_facet.ids")]
        [InlineData(@"TestCases/material/pass-occurrences_can_inherit_materials_from_their_types.ids")]
        [InlineData(@"TestCases/material/pass-occurrences_can_override_materials_from_their_types.ids")]
        [Theory]
        public void EntityTestPass(string idsFile)
        {
            List<IdsValidationResult> results = VerifyIdsFile(idsFile);
            results.Should().NotBeEmpty("Expect at least one result");
            results.Where((IdsValidationResult r) => r.Failures.Any()).Should().BeEmpty("");
        }
    }
}
