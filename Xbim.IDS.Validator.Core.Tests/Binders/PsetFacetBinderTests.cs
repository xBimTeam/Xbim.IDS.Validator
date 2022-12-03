using FluentAssertions;
using Xbim.IDS.Validator.Core.Binders;
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
        [Theory]
        public void Can_Select_Properties(int entityLabel, string psetName, string propName, object expectedtext)
        {
            IfcPropertyFacet propFacet = new IfcPropertyFacet
            {
                PropertySetName = new ValueConstraint(psetName),
                PropertyName = new ValueConstraint(propName),
                //PropertyValue = 
            };


            var result = Binder.GetProperty(entityLabel, psetName, propName);

            // Assert

            result.Value.Should().Be(expectedtext);

        }

    }
}
