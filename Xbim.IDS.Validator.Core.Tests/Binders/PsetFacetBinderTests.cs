using FluentAssertions;
using Xbim.IDS.Validator.Core.Binders;

using Xbim.Ifc4.Interfaces;

using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class PsetFacetBinderTests : BaseModelTester
    {
        public PsetFacetBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new PsetFacetBinder(model);
        }

        /// <summary>
        /// System under test
        /// </summary>
        PsetFacetBinder Binder { get; }

        [InlineData("PSet_WallCommon", "LoadBearing", "false", 5)]
        [InlineData("PSet_WallCommon", "LoadBearing", "true", 0)]
        [InlineData("Pset_MemberCommon", "LoadBearing", "true", 20)]
        [InlineData("Pset_PlateCommon", "ThermalTransmittance", "6.7069", 6)]

        [InlineData("Pset_MemberCommon", "Span", null, 20)]
        [InlineData("Pset_MemberCommon", "Span", "2043.570045136", 1)]
        [Theory]
        public void Can_Query_By_Properties(string psetName, string propName, string propValue, int expectedCount)
        {

            IfcPropertyFacet propFacet = new IfcPropertyFacet
            {
                PropertySetName = psetName,
                PropertyName = propName,
                PropertyValue = propValue
            };
            var binder = new PsetFacetBinder(model);

            // Act
            var expression = Binder.BindFilterExpression(query.InstancesExpression, propFacet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }


        [InlineData(421, "Pset_SpaceCommon", "IsExternal", false)]
        [InlineData(323, "Energy Analysis", "Area per Person", 28.5714285714286d)]
        [InlineData(323, "Dimensions", "Area", 15.41678125d)]
        [InlineData(10942, "Other", "Category", "Doors")] // Type
        [InlineData(3951, "Dimensions", "Thickness", 25d)] // Type Inheritance
        [Theory]
        public void Can_Select_Properties(int entityLabel, string psetName, string propName, object expectedtext)
        {
           
            var result = Binder.GetProperty(entityLabel, psetName, propName);

            // Assert

            result.Value.Should().Be(expectedtext);

        }


        [InlineData(177, "BaseQuantities", "GrossFloorArea", 51.9948250000001d)]
        [InlineData(177, "BaseQuantities", "Height", 2500d)]
        [InlineData(177, "BaseQuantities", "GrossVolume", 129987.0625d)]
        [Theory]
        public void Can_Select_Quantites(int entityLabel, string psetName, string propName, double expectedquant)
        {
            
            var result = Binder.GetQuantity(entityLabel, psetName, propName);

            // Assert

            if(result is IIfcQuantityArea area)
                area.AreaValue.Value.Should().Be(expectedquant); 
            else if(result is IIfcQuantityLength l)
                l.LengthValue.Value.Should().Be(expectedquant);
            else if (result is IIfcQuantityVolume v)
                v.VolumeValue.Value.Should().Be(expectedquant);
            else
                throw new NotImplementedException(result.GetType().Name); 
            

            

        }

    }
}
