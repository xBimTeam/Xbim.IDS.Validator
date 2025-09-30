using FluentAssertions;
using Xbim.IDS.Validator.Core.Helpers;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class OccurrenceMappingTests
    {

        [InlineData("IfcAirTerminal", "IfcFlowTerminal", "IfcAirTerminalType")]
        [InlineData("IfcFan", "IfcFlowMovingDevice", "IfcFanType")]
        [InlineData("IfcVibrationIsolator", "IfcDiscreteAccessory", "IfcVibrationIsolatorType")]
        [Theory]
        public void IsMappedAsExpected(string ifc4Occurrence, string ifc2x3equivalent, string ifc2x3TypeDiscriminator)
        {
            var lookup = SchemaTypeMap.Ifc2x3TypeMap.ToDictionary(k => k.Key, k=> k.Value);

            var map = lookup[ifc4Occurrence.ToUpperInvariant()];

            map.ElementType.Name.Should().Be(ifc2x3equivalent);
            map.DefiningType.Name.Should().Be(ifc2x3TypeDiscriminator);

        }
    }
}
