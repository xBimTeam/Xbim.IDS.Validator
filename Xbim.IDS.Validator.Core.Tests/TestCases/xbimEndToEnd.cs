using FluentAssertions;
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

            outcome.Status.Should().Be(ValidationStatus.Success);
        }
    }
}
