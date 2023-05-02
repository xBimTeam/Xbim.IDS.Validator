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
        public void IFC2x3_Supported(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile);

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }

        [InlineData(@"TestCases/xbim/pass-ifc4x3_models_work.ids")]
        [InlineData(@"TestCases/xbim/pass-ifc4x3_new_attributes_available.ids")]
        [Theory]
        public void IFC4x3_Supported(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile, false, XbimSchemaVersion.Ifc4x3);

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }



        [InlineData(@"TestCases/xbim/pass-subclass_type_may_be_identified_with_subtype_extension_1_2.ids")]
        [InlineData(@"TestCases/xbim/pass-subclass_type_may_be_identified_with_subtype_extension_2_2.ids")]

        [Theory]
        public void Xbim_Proprietary_Extensions(string idsFile)
        {
            var outcome = VerifyIdsFile(idsFile, false, XbimSchemaVersion.Ifc4, new VerificationOptions { IncludeSubtypes = true });

            outcome.Status.Should().Be(ValidationStatus.Pass);
        }
    }
}
