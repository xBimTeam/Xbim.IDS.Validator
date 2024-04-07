using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class PartOfTestCases : BaseTest
    {
        private const string TestCaseFolder = "partof";
        public PartOfTestCases(ITestOutputHelper output) : base(output)
        {
        }


        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            // Schema dependent tests
            { "fail-any_nested_whole_fails_a_nest_relationship.ids" , new [] { XbimSchemaVersion.Ifc4} },
            { "fail-the_nest_entity_must_match_exactly_1_2.ids" , new [] { XbimSchemaVersion.Ifc4} },
            { "fail-the_nest_predefined_type_must_match_exactly_1_2.ids" , new [] { XbimSchemaVersion.Ifc4} },




            // Unsupported tests: None

        };



        [MemberData(nameof(GetPassTestCases))]
        [Theory]
        public async Task ExpectedPasses(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: XbimSchemaVersion.Ifc4);

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

        public static IEnumerable<object[]> GetUnsupportedPassTestCases()
        {
            return GetUnsupportedTestsCases(TestCaseFolder, "pass", testExceptions);
        }


    }
}
