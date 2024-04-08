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


        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            // Schema dependent tests


        };


        [MemberData(nameof(GetPassTestCases))]
        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, validateIds: true);

                outcome.Status.Should().Be(ValidationStatus.Pass, schema.ToString());
            }
        }


        [MemberData(nameof(GetFailureTestCases))]
        [Theory]
        public async Task ExpectedFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, validateIds: true);

                outcome.Status.Should().Be(ValidationStatus.Fail, schema.ToString());
            }
        }

        

        [MemberData(nameof(GetInvalidTestCases))]
        [Theory]
        public async Task ExpectedInvalid(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema, validateIds: true);

                outcome.Status.Should().Be(ValidationStatus.Error, schema.ToString());
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

        public static IEnumerable<object[]> GetUnsupportedTestCases()
        {
            return GetUnsupportedTestsCases(TestCaseFolder, "*", testExceptions);
        }
    }
}
