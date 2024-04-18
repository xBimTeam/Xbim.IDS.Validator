using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{

    public class IfcTypeTestCases: StandardTestCaseRunner
    {
        private const string TestCaseFolder = "entity";
        public IfcTypeTestCases(ITestOutputHelper output) : base(output)
        {
        }

        public static IDictionary<string, XbimSchemaVersion[]> testExceptions = new Dictionary<string, XbimSchemaVersion[]>
        {
            // Schema dependent tests
           

            { "pass-a_matching_predefined_type_should_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-a_predefined_type_may_specify_a_user_defined_object_type.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-a_predefined_type_may_specify_a_user_defined_process_type.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-inherited_predefined_types_should_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-overridden_predefined_types_should_pass.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-restrictions_can_be_specified_for_the_predefined_type_1_3.ids", new [] { XbimSchemaVersion.Ifc4 } },
            { "pass-restrictions_can_be_specified_for_the_predefined_type_2_3.ids", new [] { XbimSchemaVersion.Ifc4 } },
           

            // Unsupported tests
 

        };

        [MemberData(nameof(GetFailureTestCases))]
        [Theory]
        public async Task ExpectedFailures(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Fail, $"{idsFile} ({schema})");
            }
        }

        [MemberData(nameof(GetPassTestCases))]
        [Theory]
        public async Task ExpectedPassess(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion:schema);

                outcome.Status.Should().Be(ValidationStatus.Pass, $"{idsFile} ({schema})");
            }
        }

        [MemberData(nameof(GetInvalidTestCases))]
        [Theory]
        public async Task ExpectedInvalid(string idsFile, params XbimSchemaVersion[] schemas)
        {
            foreach (var schema in GetSchemas(schemas))
            {
                var outcome = await VerifyIdsFile(idsFile, schemaVersion: schema);

                outcome.Status.Should().Be(ValidationStatus.Fail,$"{idsFile} ({schema})");
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



    }

}