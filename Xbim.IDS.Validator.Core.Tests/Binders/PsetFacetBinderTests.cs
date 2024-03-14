using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class PsetFacetBinderTests : BaseModelTester
    {

        public PsetFacetBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new PsetFacetBinder(BinderContext, Logger);
        }

        /// <summary>
        /// System under test
        /// </summary>
        PsetFacetBinder Binder { get; }

        [InlineData("Pset_WallCommon", "LoadBearing", false, 5)]
        [InlineData("Pset_WallCommon", "LoadBearing", true, 0)]
        // Broken - Case sensitive?
        //[InlineData("PSet_WallCommon", "LoadBearing", false, 5)]
        [InlineData("Pset_MemberCommon", "LoadBearing", true, 20)]

        [InlineData("Other", "Category", "Furnit.*", 14, default(ConstraintType), default(ConstraintType), ConstraintType.Pattern)]
        [InlineData("Other", "Categ.*", "Furnit.*", 14, default(ConstraintType), ConstraintType.Pattern, ConstraintType.Pattern)]
        [InlineData("Pset_PlateCommon", "ThermalTransmittance", "6.7069", 6)]
        [InlineData("Pset_Plate.*", "ThermalTransmittance", "6.7069", 6, ConstraintType.Pattern)]
        [InlineData("Constraints", "Sill Height", "900", 4, default(ConstraintType), default(ConstraintType), ConstraintType.Range)]
        [InlineData("Pset_MemberCommon", "Span", null, 20)]
        [InlineData("Pset_MemberCommon", "Span", "2043.570045136", 1)]
        [InlineData("BaseQuantities", "Width", "1810", 5)]  // ElementQuantity  TODO: should factor in Unit Conversion?
        [Theory]
        public void Can_Query_By_Properties(string psetName, string propName, object propValue, int expectedCount,
            ConstraintType psetConstraint = ConstraintType.Exact,
            ConstraintType propConstraint = ConstraintType.Exact, 
            ConstraintType valueConstraint = ConstraintType.Exact
            )
        {
            AssertIfcPropertyFacetQuery(Binder, psetName, propName, propValue, expectedCount, psetConstraint, propConstraint, valueConstraint);

        }



        //[InlineData(421, "Pset_SpaceCommon", "IsExternal", false)]
        [InlineData(323, "Energy Analysis", "Area per Person", 28.5714285714286d)]
        [InlineData(323, "Dimensions", "Area", 15.41678125d)]
        [InlineData(323, "Dimensions", "AREA", 15.41678125d)]
        [InlineData(323, "DIMENSIONS", "Area", 15.41678125d)]
        [InlineData(323, null, "Area", 15.41678125d)]   // Technically not valid-Pset is required
        [InlineData(323, "", "Area", 15.41678125d)]     // ""
        [InlineData(323, " ", "Area", 15.41678125d)]    // ""
        [InlineData(1229, "Pset_WallCommon", "ThermalTransmittance", 0.235926059936681d)]   // Derived Unit - THERMALTRANSMITTANCEUNIT
        [InlineData(2826, "Constraints", "Angle", 1.570796326794897d)]   // Conversion-based Unit. PLANEANGLEUNIT - 90 degrees in Radians = pi/2
        [InlineData(10942, "Other", "Category", "Doors")] // Type
        [InlineData(3951, "Dimensions", "Thickness", 25d/1000)] // Type Inheritance
        [Theory]
        public void Can_Validate_Properties(int entityLabel, string psetName, string propName, object expectedtext)
        {

            var entity = Model.Instances[entityLabel];
            var propFacet = new IfcPropertyFacet
            {
                
                PropertyName = propName,
                PropertyValue = new ValueConstraint()
            };
            if(psetName != null)
            {
                propFacet.PropertySetName = psetName;
            }
            SetPropertyValue(expectedtext, ConstraintType.Exact, propFacet);
            FacetGroup group = BuildGroup(propFacet);
            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, RequirementCardinalityOptions.Expected, result);

            // Assert

            result.Successful.Should().NotBeEmpty();
            result.Failures.Should().BeEmpty();
            result.ValidationStatus.Should().Be(ValidationStatus.Pass);
        }

        [InlineData(323, "Energy Analysis", "Area per Person", 100d)]
        [InlineData(323, "Energy Analysis", "Area per Person", 28.571428d, false)]
        [InlineData(33350, "BaseQuantities", "Width", 1.91d)]
        [InlineData(33350, "BaseQuantities", "Width", 1.81d, false)]
        [Theory]
        public void Can_Validate_Properties_Prohibited(int entityLabel, string psetName, string propName, object expectedtext, bool shouldPass = true)
        {

            var entity = Model.Instances[entityLabel];
            var propFacet = new IfcPropertyFacet
            {

                PropertyName = propName,
                PropertyValue = new ValueConstraint()
            };
            if (psetName != null)
            {
                propFacet.PropertySetName = psetName;
            }
            SetPropertyValue(expectedtext, ConstraintType.Exact, propFacet);
            FacetGroup group = BuildGroup(propFacet);

            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, RequirementCardinalityOptions.Prohibited, result);

            // Assert
            if (shouldPass)
            {
                result.ValidationStatus.Should().Be(ValidationStatus.Pass);
                result.Successful.Should().BeEmpty();
                result.Failures.Should().NotBeEmpty();
            }
            else
            {
                result.ValidationStatus.Should().Be(ValidationStatus.Fail);
                result.Successful.Should().NotBeEmpty();
                //result.Failures.Should().BeEmpty();
            }
        }



        [InlineData(177, "BaseQuantities", "GrossFloorArea", 51.9948250000001d)]
        [InlineData(177, "BaseQuantities", "Height", 2500d/1000)]
        [InlineData(177, "BaseQuantities", "GrossVolume", 129987.0625d)]
        [Theory]
        public void Can_Validate_Quantites(int entityLabel, string psetName, string propName, double expectedquant)
        {

            var entity = Model.Instances[entityLabel];
            var propFacet = new IfcPropertyFacet
            {
                PropertySetName = psetName,
                PropertyName = propName,
                PropertyValue = new ValueConstraint()
            };
            SetPropertyValue(expectedquant, ConstraintType.Exact, propFacet);
            FacetGroup group = BuildGroup(propFacet);
            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, RequirementCardinalityOptions.Expected, result);

            // Assert

            result.Successful.Should().NotBeEmpty();
            result.Failures.Should().BeEmpty();
            result.ValidationStatus.Should().Be(ValidationStatus.Pass);


        }


        ILogger<PsetFacetBinder> Logger { get => GetLogger<PsetFacetBinder>(); }
    }
}
