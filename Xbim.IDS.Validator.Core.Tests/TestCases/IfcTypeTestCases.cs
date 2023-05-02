using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{

    public class IfcTypeTestCases: BaseTest
    {
        public IfcTypeTestCases(ITestOutputHelper output) : base(output)
        {
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
        public async Task EntityTestFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Fail, schema.ToString());
            }
        }

        [InlineData("TestCases/entity/pass-a_matching_entity_should_pass.ids")]
        [InlineData("TestCases/entity/pass-a_matching_predefined_type_should_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData("TestCases/entity/pass-a_predefined_type_may_specify_a_user_defined_element_type.ids")]
        [InlineData("TestCases/entity/pass-a_predefined_type_may_specify_a_user_defined_object_type.ids", XbimSchemaVersion.Ifc4)]
        [InlineData("TestCases/entity/pass-a_predefined_type_may_specify_a_user_defined_process_type.ids", XbimSchemaVersion.Ifc4)]
        [InlineData("TestCases/entity/pass-an_matching_entity_should_pass_regardless_of_predefined_type.ids")]
        [InlineData("TestCases/entity/pass-entities_can_be_specified_as_a_xsd_regex_pattern_2_2.ids")]
        [InlineData("TestCases/entity/pass-entities_can_be_specified_as_an_enumeration_1_3.ids")]
        [InlineData("TestCases/entity/pass-entities_can_be_specified_as_an_enumeration_2_3.ids")]
        [InlineData("TestCases/entity/pass-inherited_predefined_types_should_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData("TestCases/entity/pass-overridden_predefined_types_should_pass.ids", XbimSchemaVersion.Ifc4)]
        [InlineData("TestCases/entity/pass-restrictions_an_be_specified_for_the_predefined_type_1_3.ids", XbimSchemaVersion.Ifc4)]
        [InlineData("TestCases/entity/pass-restrictions_an_be_specified_for_the_predefined_type_2_3.ids", XbimSchemaVersion.Ifc4)]
        [Theory]
        public async Task EntityTestPass(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion:schema);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
        }

        

        
    }

}