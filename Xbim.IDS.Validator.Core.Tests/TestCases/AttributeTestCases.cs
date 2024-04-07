using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class AttributeTestCases : BaseTest
    {
        private const string TestCaseFolder = "attribute";
        public AttributeTestCases(ITestOutputHelper output) : base(output)
        {
        }


        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            // Schema dependent tests
            { "fail-durations_are_treated_as_strings_2_2.ids" , new [] { XbimSchemaVersion.Ifc4} },
            { "fail-value_checks_always_fail_for_objects.ids" , new [] { XbimSchemaVersion.Ifc4} },

            { "pass-attributes_referencing_an_object_should_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-attributes_with_a_boolean_false_should_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-attributes_with_a_boolean_true_should_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-attributes_with_a_zero_duration_should_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-booleans_must_be_specified_as_uppercase_strings_2_3.ids", new [] { XbimSchemaVersion.Ifc4 } },  // IfcTask changed in IFC4
            { "pass-dates_are_treated_as_strings_1_2.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-durations_are_treated_as_strings_1_2.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-integers_follow_the_same_rules_as_numbers.ids", new [] { XbimSchemaVersion.Ifc4 } },        // StairFlight.NumberOfRiser[s] got renamed in IFC4
            { "pass-integers_follow_the_same_rules_as_numbers_2_2.ids", new [] { XbimSchemaVersion.Ifc4 } },


            // Unsupported tests
            // None
        };

       

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

        public static IEnumerable<object[]> GetFailureTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "fail", testExceptions);
        }

        public static IEnumerable<object[]> GetPassTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "pass", testExceptions);
        }

        public static IEnumerable<object[]> GetUnsupportedTestCases()
        {
            return GetUnsupportedTestsCases(TestCaseFolder, "pass", testExceptions);
        }

    }
}
