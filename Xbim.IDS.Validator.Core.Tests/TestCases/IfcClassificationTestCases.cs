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
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] _)
        {
            // Can't test IFC2x3 Classifications in our harness due to schema changes in IFC4

            var outcome = await VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Pass);
            
        }

        [MemberData(nameof(GetFailureTestCases))]
        [Theory]
        public async Task ExpectedFailures(string idsFile, params XbimSchemaVersion[] _)
        {

            var specialCase = Path.GetFileName(idsFile).StartsWith("fail-occurrences_override_the_type_classification_per_system");

            var outcome = await VerifyIdsFile(idsFile, specialCase);

            outcome.Status.Should().Be(ValidationStatus.Fail);
            
        }

        //[MemberData(nameof(GetInvalidTestCases))]
        //[Theory]
        //public async Task ExpectedInvalid(string idsFile, params XbimSchemaVersion[] schemas)
        //{
        //    foreach (var schema in GetSchemas(schemas))
        //    {
        //        var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema, validateIds: true);

        //        outcome.Status.Should().Be(ValidationStatus.Error, schema.ToString());
        //    }
        //}

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
    }
}
