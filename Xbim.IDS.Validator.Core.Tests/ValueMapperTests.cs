using FluentAssertions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Configuration;
using Xbim.Ifc4.MeasureResource;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class ValueMapperTests
    {
        [Fact]
        public void CanMapBooleans()
        {
            var mapper = new IdsValueMapper(new[] { new IdsValueMapProvider() });

            mapper.ContainsMap<IExpressBooleanType>().Should().BeTrue();
            var value = new IfcBoolean(false);
            mapper.MapValue(value, out var result).Should().BeTrue();
            result.Should().BeOfType<string>();
            result.ToString().Should().Be("false");

            value = new IfcBoolean(true);
            mapper.MapValue(value, out result).Should().BeTrue();
            result.ToString().Should().Be("true");
        }

        [Fact]
        public void CanMapLogicals()
        {
            var mapper = new IdsValueMapper(new[] { new IdsValueMapProvider() });

            
            var value = new IfcLogical(false);
            mapper.MapValue(value, out var result).Should().BeTrue();
            result.Should().BeOfType<string>();
            result.ToString().Should().Be("false");

            value = new IfcLogical();
            mapper.MapValue(value, out result).Should().BeTrue();
            result.Should().BeOfType<string>();
            result.ToString().Should().Be("");

        }
    }
}
