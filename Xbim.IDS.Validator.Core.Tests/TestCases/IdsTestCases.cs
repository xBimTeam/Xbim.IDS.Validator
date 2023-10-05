using FluentAssertions;
using Xbim.Common.Step21;
using Xbim.Ifc4.RepresentationResource;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class IdsTestCases : BaseTest
    {
        private const string TestCaseFolder = "ids";
        public IdsTestCases(ITestOutputHelper output) : base(output)
        {
        }

        
        [MemberData(nameof(GetPassTestCases))]
        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
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

        public static IEnumerable<object[]> GetFailureTestCases()
        {

            return GetApplicableTestCases(TestCaseFolder, "fail", testExceptions);
        }

        public static IEnumerable<object[]> GetPassTestCases()
        {
            return GetApplicableTestCases(TestCaseFolder, "pass", testExceptions);
        }

        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            // { "fail-durations_are_treated_as_strings_2_2.ids" , new [] { XbimSchemaVersion.Ifc4} },

        };
    }
}
