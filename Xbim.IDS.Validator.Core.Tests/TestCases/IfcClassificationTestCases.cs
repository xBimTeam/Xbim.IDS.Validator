using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class IfcClassificationTestCases : BaseTest
    {
        private const string TestCaseFolder = "class";

        public IfcClassificationTestCases(ITestOutputHelper output) : base(output)
        {
        }

        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            //{ "fail-durations_are_treated_as_strings_2_2.ids" , new [] { XbimSchemaVersion.Ifc4} },

        };

        [MemberData(nameof(GetPassTestCases))]
        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            // Can't test IFC2x3 Classifications in our harness due to schema changes in IFC4
            var _ = schemas;

            var specialCase = Path.GetFileName(idsFile).StartsWith("pass-occurrences_override_the_type_classification_per_system");
            var outcome = await VerifyIdsFile(idsFile, specialCase, XbimSchemaVersion.Ifc4);

            outcome.Status.Should().Be(ValidationStatus.Pass);
            
        }

        [MemberData(nameof(GetFailureTestCases))]
        [Theory]
        public async Task ExpectedFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            var _ = schemas;

            var outcome = await VerifyIdsFile(idsFile);

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
    }
}
