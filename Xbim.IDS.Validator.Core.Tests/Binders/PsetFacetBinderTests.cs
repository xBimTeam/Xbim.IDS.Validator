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
        [InlineData("BaseQuantities", "Width", "1810", 5)]  // ElementQuantity
        [Theory]
        public void Can_Query_By_Properties(string psetName, string propName, object propValue, int expectedCount,
            ConstraintType psetConstraint = ConstraintType.Exact,
            ConstraintType propConstraint = ConstraintType.Exact, 
            ConstraintType valueConstraint = ConstraintType.Exact
            )
        {

            IfcPropertyFacet propFacet = new IfcPropertyFacet
            {
                PropertySetName = new ValueConstraint(),
                PropertyName = new ValueConstraint(),
                PropertyValue = new ValueConstraint()
            };
            switch (psetConstraint)
            {
                case ConstraintType.Exact:
                    propFacet.PropertySetName.AddAccepted(new ExactConstraint(psetName));
                    break;

                case ConstraintType.Pattern:
                    propFacet.PropertySetName.AddAccepted(new PatternConstraint(psetName));
                    break;

            }
            switch (propConstraint)
            {
                case ConstraintType.Exact:
                    propFacet.PropertyName.AddAccepted(new ExactConstraint(propName));
                    break;

                case ConstraintType.Pattern:
                    propFacet.PropertyName.AddAccepted(new PatternConstraint(propName));
                    break;

            }
            if (propValue != null)
            {
                SetPropertyValue(propValue, valueConstraint, propFacet);
            }
            var binder = new PsetFacetBinder(BinderContext, Logger);

            // Act
            var expression = Binder.BindSelectionExpression(query.InstancesExpression, propFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }

        private static void SetPropertyValue(object propValue, ConstraintType valueConstraint, IfcPropertyFacet propFacet)
        {
            switch (valueConstraint)
            {
                case ConstraintType.Exact:
                    if (propValue is bool)
                        propFacet.PropertyValue.BaseType = NetTypeName.Boolean;
                    if (propValue is long)
                        propFacet.PropertyValue.BaseType = NetTypeName.Integer;
                    propFacet.PropertyValue.AddAccepted(new ExactConstraint(propValue.ToString()));
                    break;

                case ConstraintType.Pattern:
                    propFacet.PropertyValue.AddAccepted(new PatternConstraint(propValue.ToString()));
                    break;

                case ConstraintType.Range:
                    propFacet.PropertyValue.BaseType = NetTypeName.Double;
                    propFacet.PropertyValue.AddAccepted(new RangeConstraint("0", false, propValue.ToString(), true));
                    break;


            }
        }

        //[InlineData(421, "Pset_SpaceCommon", "IsExternal", false)]
        [InlineData(323, "Energy Analysis", "Area per Person", 28.5714285714286d)]
        [InlineData(323, "Dimensions", "Area", 15.41678125d)]
        [InlineData(10942, "Other", "Category", "Doors")] // Type
        [InlineData(3951, "Dimensions", "Thickness", 25d/1000)] // Type Inheritance
        [Theory]
        public void Can_Validate_Properties(int entityLabel, string psetName, string propName, object expectedtext)
        {

            var entity = Model.Instances[entityLabel];
            var propFacet = new IfcPropertyFacet
            {
                PropertySetName = psetName,
                PropertyName = propName,
                PropertyValue = new ValueConstraint()
            };
            SetPropertyValue(expectedtext, ConstraintType.Exact, propFacet);
            FacetGroup group = BuildGroup(propFacet);
            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, RequirementCardinalityOptions.Expected, result);

            // Assert

            result.Successful.Should().NotBeEmpty();
            result.Failures.Should().BeEmpty();

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



        }


        ILogger<PsetFacetBinder> Logger { get => GetLogger<PsetFacetBinder>(); }
    }
}
