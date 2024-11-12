using FluentAssertions;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Core;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Extensions.COBie.Tests.TestCases
{
    public class XbimCobieTests : COBieTestCaseRunner
    {
        public XbimCobieTests(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestCases/xbim/pass-cobie_models_work.ids", ValidationStatus.Pass)]
        [InlineData(@"TestCases/xbim/pass-cobie_checks_ext_id_length.ids", ValidationStatus.Pass)]
        [InlineData(@"TestCases/xbim/pass-cobie_models_can_verify.ids", ValidationStatus.Pass)]
        [InlineData(@"TestCases/xbim/pass-cobie_verifies_missing_references.ids", ValidationStatus.Pass)]
        [InlineData(@"TestCases/xbim/pass-cobie_models_support_attribute_verification.ids", ValidationStatus.Pass)]
        [InlineData(@"TestCases/xbim/fail-cobie_models_support_attribute_verification.ids", ValidationStatus.Fail)]
        [Theory]
        public async Task COBieSupported(string idsFile, ValidationStatus expected)
        {
            var outcome = await VerifyIdsFile(idsFile, schemaVersion: XbimSchemaVersion.Cobie2X4, options:
                new VerificationOptions { IncludeSubtypes = true, AllowDerivedAttributes = true, PermittedIdsAuditStatuses = VerificationOptions.Relaxed });

            outcome.Status.Should().Be(expected);
        }
    }
}
