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

            //{ "fail-a_specification_passes_only_if_all_requirements_pass_1_2.ids", new [] { XbimSchemaVersion.Ifc2X3 } },
            //{ "fail-prohibited_specifications_fails_if_the_applicability_matches.ids", new [] { XbimSchemaVersion.Ifc2X3 } },
            // 

        };


        [MemberData(nameof(GetPassTestCases))]
        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile);

                outcome.Status.Should().Be(ValidationStatus.Pass, $"{idsFile} ({schema})");
            }
            ValidateIds(idsFile).Should().Be(IdsLib.Audit.Status.Ok);
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
            ValidateIds(idsFile).Should().Be(IdsLib.Audit.Status.Ok);
        }

        

        [MemberData(nameof(GetInvalidTestCases))]
        [Theory]
        public async Task ExpectedInvalid(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Fail, $"{idsFile} ({schema})");
            }
            ValidateIds(idsFile).Should().NotBe(IdsLib.Audit.Status.Ok);
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
