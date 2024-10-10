﻿using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class PsetFacetBinderTests : BaseBinderTests
    {

        public PsetFacetBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new PsetFacetBinder(Logger);
            Binder.Initialise(BinderContext);
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

        [InlineData("Other", "Category", "Furnit.*", 23, default(ConstraintType), default(ConstraintType), ConstraintType.Pattern)]
        [InlineData("Other", "Categ.*", "Furnit.*", 23, default(ConstraintType), ConstraintType.Pattern, ConstraintType.Pattern)]
        [InlineData("Pset_PlateCommon", "ThermalTransmittance", "6.7069", 6)]
        [InlineData("Pset_Plate.*", "ThermalTransmittance", "6.7069", 6, ConstraintType.Pattern)]
        [InlineData("Constraints", "Sill Height", 900, 4, default(ConstraintType), default(ConstraintType), ConstraintType.Range)]
        [InlineData("Pset_MemberCommon", "Span", null, 20)]
        [InlineData("Pset_MemberCommon", "Span", "2043.570045136", 1)]
        [InlineData("Other", "FrameDepth", "89", 1)]    // Pset on Type
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
        [InlineData(323, "Dimensions", "Area", 15.41678125d, "IfcAreaMeasure")]

        [InlineData(4705, "Pset_WallCommon", "IsExternal", "false")]
        [InlineData(4705, "Pset_WallCommon", "IsExternal", false)]
        [InlineData(323, null, "Area", 15.41678125d)]   // Technically not valid-Pset is required
        [InlineData(323, "", "Area", 15.41678125d)]     // ""
        [InlineData(323, " ", "Area", 15.41678125d)]    // ""
        [InlineData(1229, "Pset_WallCommon", "ThermalTransmittance", 0.235926059936681d)]   // Derived Unit - THERMALTRANSMITTANCEUNIT
        [InlineData(2826, "Constraints", "Angle", 1.570796326794897d)]   // Conversion-based Unit. PLANEANGLEUNIT - 90 degrees in Radians = pi/2
        [InlineData(10942, "Other", "Category", "Doors")] // Type
        [InlineData(3951, "Dimensions", "Thickness", 25d/1000)] // Type Inheritance
        [Theory]
        public void Can_Validate_Properties(int entityLabel, string psetName, string propName, object expectedtext, string dataType = null)
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
            if (dataType != null)
            {
                propFacet.DataType = dataType;
            }
            SetPropertyValue(expectedtext, ConstraintType.Exact, propFacet);
            FacetGroup group = BuildGroup(propFacet);
            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, Cardinality.Expected, result);
            OutputMessages(result);
            // Assert

            result.Successful.Should().NotBeEmpty();
            result.Failures.Should().BeEmpty();
            result.ValidationStatus.Should().Be(ValidationStatus.Pass);
        }

        [InlineData(323, "Energy Analysis", "Area per Person", null, 100d)]
        [InlineData(33350, "BaseQuantities", "Width", null, 1.91d)]
        [InlineData(33350, "BaseQuantities", "Width", "IfcLengthMeasure", 1.91d)]
        [InlineData(33350, "BaseQuantities", "Depth", null, 0.2d)]
        [InlineData(33350, "BaseQuantities", "Depth", null, null)]          // No depth on Window
        [InlineData(37554, "BaseQuantities", "Width", null, null)]   // has no Base Quants
        [InlineData(37554, "BaseQuantities", "Width", "IfcLengthMeasure", 1.91d)]   // has no Base Quants

        [InlineData(323, "Energy Analysis", "Area per Person", null, 28.571428d, false)]
        [InlineData(33350, "BaseQuantities", "Width", null, 1.81d, false)]
        [InlineData(33350, "BaseQuantities", "Width", "IfcLengthMeasure", 1.81d, false)]

        [InlineData(4705, "Constraints", "Base Offset", null, null, false)] // Prop with Value Exists
        [InlineData(4705, "Other", "Category", null, "Walls", false)] // Value matches
        [InlineData(4705, "Other", "Category", "IfcText", "Walls", false)] // Value matches
        [Theory]
        public void Can_Validate_Prohibited_Properties(int entityLabel, string psetName, string propName, string dataType,  object expectedtext, bool shouldPass = true)
        {

            var entity = Model.Instances[entityLabel];
            var propFacet = new IfcPropertyFacet
            {
                PropertySetName = psetName,
                PropertyName = propName,
                PropertyValue = new ValueConstraint()
            };
            if (dataType != null)
            {
                propFacet.DataType = dataType;
            }
            if(expectedtext != null)
                SetPropertyValue(expectedtext, ConstraintType.Exact, propFacet);
            FacetGroup group = BuildGroup(propFacet);

            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, Cardinality.Prohibited, result);
            OutputMessages(result);

            // Assert
            if (shouldPass)
            {
                result.ValidationStatus.Should().Be(ValidationStatus.Pass);
                result.Successful.Should().NotBeEmpty();
                result.Failures.Should().BeEmpty();
            }
            else
            {
                result.ValidationStatus.Should().Be(ValidationStatus.Fail);
                result.Successful.Should().NotBeEmpty();
                result.Failures.Should().NotBeEmpty();
            }
        }

        private void OutputMessages(IdsValidationResult result)
        {
            foreach (var message in result.Messages)
            {
                var level = message.Status == ValidationStatus.Fail ? LogLevel.Warning : LogLevel.Information;
                logger.Log(level, "Message: {message}", message);
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
            Binder.ValidateEntity(entity, propFacet, Cardinality.Expected, result);

            // Assert

            result.Successful.Should().NotBeEmpty();
            result.Failures.Should().BeEmpty();
            result.ValidationStatus.Should().Be(ValidationStatus.Pass);


        }


        ILogger<PsetFacetBinder> Logger { get => GetLogger<PsetFacetBinder>(); }
    }
}
