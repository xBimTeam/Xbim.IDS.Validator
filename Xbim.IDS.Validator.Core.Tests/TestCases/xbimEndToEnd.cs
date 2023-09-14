using FluentAssertions;
using Xbim.Common.Step21;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public class XbimEndToEnd : BaseTest
    {
        public XbimEndToEnd(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestCases/xbim/pass-ifc2x3-unit_conversions_shall_take_place_to_ids_nominated_standard_units_2_2.ids")]
        

        [Theory]
        public async Task IFC2x3_Supported(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }

        [InlineData(@"TestCases/xbim/pass-ifc4x3_models_work.ids")]
        [InlineData(@"TestCases/xbim/pass-ifc4x3_new_attributes_available.ids")]
        [Theory]
        public async Task IFC4x3_Supported(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile, false, XbimSchemaVersion.Ifc4x3);

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }



        [InlineData(@"TestCases/xbim/pass-subclass_type_may_be_identified_with_subtype_extension_1_2.ids")]
        [InlineData(@"TestCases/xbim/pass-subclass_type_may_be_identified_with_subtype_extension_2_2.ids")]

        [Theory]
        public async Task Xbim_Proprietary_Extensions(string idsFile)
        {
            var outcome = await VerifyIdsFile(idsFile, false, XbimSchemaVersion.Ifc4, new VerificationOptions { IncludeSubtypes = true });

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }


        [InlineData(@"TestCases/xbim/pass-ifc2x3_using_ifc4_entity.ids", ValidationStatus.Pass)]
        [InlineData(@"TestCases/xbim/pass-ifc2x3-air_terminal_edge_case.ids", ValidationStatus.Pass)]
        [Theory]
        public async Task CrossSchemaHandlingSupported(string idsFile, ValidationStatus expected)
        {
            var outcome = await VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(expected);
        }

        [Theory(Skip ="Needs thought on querying Type's Predefined values via expressions")]
        [InlineData(@"TestCases/xbim/pass-ifc2x3-air_terminal_edge_case_with_predefined.ids", ValidationStatus.Pass)]
        [InlineData(@"TestCases/xbim/fail-ifc2x3-air_terminal_edge_case_with_predefined.ids", ValidationStatus.Fail)]
        public async Task CrossSchemaHandlingNotImplemented(string idsFile, ValidationStatus expected)
        {
            var outcome = await VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(expected);
        }

    }
}
