using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class MaterialTestCases : BaseTest
    {
        private const string TestCaseFolder = "material";

        public MaterialTestCases(ITestOutputHelper output) : base(output)
        {
        }

        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            // Schema dependent tests
            { "fail-a_constituent_set_with_no_data_will_fail_a_value_check.ids" , new [] { XbimSchemaVersion.Ifc4} },
            
            { "pass-a_material_category_may_pass_the_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_constituent_category_in_a_constituent_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_constituent_name_in_a_constituent_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_layer_category_in_a_layer_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_layer_name_in_a_layer_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_material_category_in_a_constituent_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_material_category_in_a_layer_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_material_category_in_a_list_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_material_category_in_a_profile_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_material_name_in_a_constituent_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_material_name_in_a_profile_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_profile_category_in_a_profile_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_profile_name_in_a_profile_set_will_pass_a_value_check.ids", new [] { XbimSchemaVersion.Ifc4 } },

        };

      

        [MemberData(nameof(GetPassTestCases))]
        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
        }


        [MemberData(nameof(GetFailureTestCases))]
        [Theory]
        public async Task ExpectedFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Fail, schema.ToString());
            }
        }

        public static IEnumerable<object[]> GetFailureTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "fail", testExceptions);
        }

        public static IEnumerable<object[]> GetPassTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "pass", testExceptions);
        }

    }
}
