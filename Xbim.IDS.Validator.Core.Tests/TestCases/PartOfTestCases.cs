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




            // Unsupported tests
            { "pass-an_aggregate_may_specify_the_predefined_type_of_the_whole_1_2.ids", new [] { XbimSchemaVersion.Unsupported } }, // 
            { "fail-an_aggregate_may_specify_the_predefined_type_of_the_whole_2_2.ids", new [] { XbimSchemaVersion.Unsupported } },

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


       
        [MemberData(nameof(GetUnsupportedPassTestCases))]
        [SkippableTheory]
        public async Task UnsupportedPasses(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile);

            Skip.If(outcome.Status != ValidationStatus.Pass, "Not yet supported");

            outcome.Status.Should().Be(ValidationStatus.Pass);
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



        [MemberData(nameof(GetUnsupportedFailTestCases))]
        [SkippableTheory]
        public async Task UnsupportedFailures(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile);


            Skip.If(outcome.Status != ValidationStatus.Inconclusive, "Not yet supported");

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
