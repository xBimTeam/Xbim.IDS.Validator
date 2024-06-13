using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class PsetTestCases : StandardTestCaseRunner
    {
        private const string TestCaseFolder = "property";
        public PsetTestCases(ITestOutputHelper output) : base(output)
        {
        }


        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            // Schema dependent tests


            { "pass-a_zero_duration_will_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_matching_value_in_a_bounded_property_will_pass_1_4.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_matching_value_in_a_bounded_property_will_pass_2_4.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_matching_value_in_a_bounded_property_will_pass_3_4.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-dates_are_treated_as_strings_1_2.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-durations_are_treated_as_strings_1_2.ids", new [] { XbimSchemaVersion.Ifc4 } },

            { "pass-any_matching_value_in_an_enumerated_property_will_pass_1_3.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-any_matching_value_in_an_enumerated_property_will_pass_2_3.ids", new [] { XbimSchemaVersion.Ifc4 } },


            // Unsupported tests


        };

       
        [MemberData(nameof(GetPassTestCases))]

        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach(var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Pass, $"{idsFile} ({schema})");
            }
            
        }

        
        [MemberData(nameof(GetFailureTestCases))]
        [Theory]
        public async Task ExpectedFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile);

                outcome.Status.Should().Be(ValidationStatus.Fail, $"{idsFile} ({schema})");
            }
        }

        public static IEnumerable<object[]> GetInvalidTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "invalid", testExceptions);
        }

        public static IEnumerable<object[]> GetFailureTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "fail", testExceptions);
        }

        public static IEnumerable<object[]> GetPassTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "pass", testExceptions);
        }

        public static IEnumerable<object[]> GetUnsupportedPassTestCases()
        {
            return GetUnsupportedTestsCases(TestCaseFolder, "pass", testExceptions);
        }

        public static IEnumerable<object[]> GetUnsupportedFailTestCases()
        {
            return GetUnsupportedTestsCases(TestCaseFolder, "fail", testExceptions);
        }
    }
}
