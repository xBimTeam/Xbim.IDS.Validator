using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class PartOfTestCases : BaseTest
    {
        public PartOfTestCases(ITestOutputHelper output) : base(output)
        {
        }
        

        [InlineData(@"TestCases/partof/pass-a_group_entity_must_match_exactly_2_2.ids")]
        [InlineData(@"TestCases/partof/pass-a_grouped_element_passes_a_group_relationship.ids")]
        [InlineData(@"TestCases/partof/pass-a_required_facet_checks_all_parameters_as_normal.ids")]
        [InlineData(@"TestCases/partof/pass-an_aggregate_entity_may_pass_any_ancestral_whole_passes.ids")]
        [InlineData(@"TestCases/partof/pass-an_aggregate_may_specify_the_entity_of_the_whole_1_2.ids")]
        [InlineData(@"TestCases/partof/pass-an_optional_facet_always_passes_regardless_of_outcome_1_2.ids")]
        [InlineData(@"TestCases/partof/pass-an_optional_facet_always_passes_regardless_of_outcome_2_2.ids")]
        [InlineData(@"TestCases/partof/pass-any_contained_element_passes_a_containment_relationship_2_2.ids")]
        [InlineData(@"TestCases/partof/pass-any_nested_part_passes_a_nest_relationship.ids")]
        [InlineData(@"TestCases/partof/pass-nesting_may_be_indirect.ids")]
        [InlineData(@"TestCases/partof/pass-the_aggregated_part_passes_an_aggregate_relationship.ids")]
        [InlineData(@"TestCases/partof/pass-the_container_entity_must_match_exactly_2_2.ids")]
        [InlineData(@"TestCases/partof/pass-the_container_may_be_indirect.ids")]
        [InlineData(@"TestCases/partof/pass-the_nest_entity_must_match_exactly_2_2.ids")]
        [Theory]
        public void EntityTestPass(string idsFile, params XbimSchemaVersion[] schemas)
        {
            //foreach (var schema in GetSchemas(schemas))
            {
                var schema = XbimSchemaVersion.Ifc4;
                var outcome = VerifyIdsFile(idsFile, schemaVersion: XbimSchemaVersion.Ifc4);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
        }

        // Not supported in XIDS
        [InlineData(@"TestCases/partof/pass-a_group_predefined_type_must_match_exactly_2_2.ids")]
        [InlineData(@"TestCases/partof/pass-an_aggregate_may_specify_the_predefined_type_of_the_whole_1_2.ids")]
        [InlineData(@"TestCases/partof/pass-the_container_predefined_type_must_match_exactly_2_2.ids")]
        [InlineData(@"TestCases/partof/pass-the_nest_predefined_type_must_match_exactly_2_2.ids")]
        [Theory(Skip ="Needs XIDS 0.9")]
        public void EntityTestPass_Predefined(string idsFile, params XbimSchemaVersion[] schemas)
        {
            //foreach (var schema in GetSchemas(schemas))
            {
                var schema = XbimSchemaVersion.Ifc4;
                var outcome = VerifyIdsFile(idsFile, schemaVersion: XbimSchemaVersion.Ifc4);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
        }


        

        [InlineData(@"TestCases/partof/fail-a_group_entity_must_match_exactly_1_2.ids")]
        [InlineData(@"TestCases/partof/fail-a_non_aggregated_element_fails_an_aggregate_relationship.ids")]
        [InlineData(@"TestCases/partof/fail-a_non_grouped_element_fails_a_group_relationship.ids")]
        [InlineData(@"TestCases/partof/fail-a_prohibited_facet_returns_the_opposite_of_a_required_facet.ids")]
        [InlineData(@"TestCases/partof/fail-an_aggregate_may_specify_the_entity_of_the_whole_2_2.ids")]
        [InlineData(@"TestCases/partof/fail-any_contained_element_passes_a_containment_relationship_1_2.ids")]
        [InlineData(@"TestCases/partof/fail-any_nested_whole_fails_a_nest_relationship.ids")]
        [InlineData(@"TestCases/partof/fail-the_aggregated_whole_fails_an_aggregate_relationship.ids")]
        [InlineData(@"TestCases/partof/fail-the_container_entity_must_match_exactly_1_2.ids")]
        [InlineData(@"TestCases/partof/fail-the_container_itself_always_fails.ids")]
        [InlineData(@"TestCases/partof/fail-the_nest_entity_must_match_exactly_1_2.ids")]

        [Theory]
        public void EntityTestFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            //foreach (var schema in GetSchemas(schemas))
            {
                var schema = XbimSchemaVersion.Ifc4;
                var outcome = VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Fail, schema.ToString());
            }
        }

        [InlineData(@"TestCases/partof/fail-a_group_predefined_type_must_match_exactly_2_2.ids")]
        [InlineData(@"TestCases/partof/fail-the_nest_predefined_type_must_match_exactly_1_2.ids")]
        [InlineData(@"TestCases/partof/fail-an_aggregate_may_specify_the_predefined_type_of_the_whole_2_2.ids")]
        [InlineData(@"TestCases/partof/fail-the_container_predefined_type_must_match_exactly_1_2.ids")]

        [Theory(Skip ="Needs PredefinedType Support")]
        public void EntityTestFailures_PreDefined(string idsFile, params XbimSchemaVersion[] schemas)
        {
            //foreach (var schema in GetSchemas(schemas))
            {
                var schema = XbimSchemaVersion.Ifc4;
                var outcome = VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Fail, schema.ToString());
            }
        }

    }
}
