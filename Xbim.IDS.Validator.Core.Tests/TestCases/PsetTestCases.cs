using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class PsetTestCases : BaseTest
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


            // Unsupported tests
            { "fail-predefined_properties_are_supported_but_discouraged_2_2.ids", new [] { XbimSchemaVersion.Unsupported } }, // To implement IFCDOORPANELPROPERTIES edgecase
            { "pass-predefined_properties_are_supported_but_discouraged_1_2.ids", new [] { XbimSchemaVersion.Unsupported } }, // To implement IFCDOORPANELPROPERTIES edgecase
            { "pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_1_4.ids", new [] { XbimSchemaVersion.Unsupported } }, // XIDS support?
            { "pass-floating_point_numbers_are_compared_with_a_1e_6_tolerance_2_4.ids", new [] { XbimSchemaVersion.Unsupported } },
            //

        };

       
        [MemberData(nameof(GetPassTestCases))]

        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach(var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
            
        }


        [MemberData(nameof(GetUnsupportedPassTestCases))]
        [SkippableTheory]
        public async Task PassesToImplement(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile);

            Skip.If(outcome.Status != ValidationStatus.Pass, "DoorPanelsProperties etc & FP precision not yet supported");

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }


        
        [MemberData(nameof(GetFailureTestCases))]
        [Theory]
        public async Task ExpectedFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile);

                outcome.Status.Should().Be(ValidationStatus.Fail, schema.ToString());
            }
        }


        [MemberData(nameof(GetUnsupportedFailTestCases))]
        [SkippableTheory]
        public async Task FailuresToImplement(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile);

            Skip.If(outcome.Status != ValidationStatus.Fail, "DoorPanelsPropertyies etc not yet supported");

            outcome.Status.Should().Be(ValidationStatus.Fail);
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
