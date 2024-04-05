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

            // Broken tests - Need IDS review as they are invalid. E.g. requirements stated where applicability is Prohibited
            { "fail-prohibited_specifications_fail_if_at_least_one_entity_passes_all_requirements_3_3.ids", new [] { XbimSchemaVersion.Unsupported } },

            { "pass-a_prohibited_specification_and_a_prohibited_facet_results_in_a_double_negative.ids", new [] { XbimSchemaVersion.Unsupported } },
            { "pass-multiple_specifications_are_independent_of_one_another.ids", new [] { XbimSchemaVersion.Unsupported } },
            { "pass-prohibited_specifications_fail_if_at_least_one_entity_passes_all_requirements_2_3.ids", new [] { XbimSchemaVersion.Unsupported } },
            { "pass-prohibited_specifications_fail_if_at_least_one_entity_passes_all_requirements_1_3.ids", new [] { XbimSchemaVersion.Unsupported } },


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

        // TODO: These Prohibited test cases are no longer valid. 
        [MemberData(nameof(GetUnsupportedTestCases))]
        [SkippableTheory]
        public async Task ToImplement(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile, validateIds: true);

            Skip.If(outcome.Status == ValidationStatus.Error, "TestCases need review. isd-lib reports error 204: requirements are not allowed when applicability is prohibited");

            outcome.Status.Should().Be(ValidationStatus.Pass);
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
