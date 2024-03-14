using FluentAssertions;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class IfcTypeFacetBinder2x3Tests : BaseModelTester
    {
        public IfcTypeFacetBinder2x3Tests(ITestOutputHelper output) : base(output, Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3)
        {
            Binder = new IfcTypeFacetBinder(BinderContext, GetLogger<IfcTypeFacetBinder>());
        }

        /// <summary>
        /// System under test
        /// </summary>
        IfcTypeFacetBinder Binder { get; }


        [InlineData(20125, "IFCWINDOW", "744x1500_Steel")]
        [InlineData(325421, "IFCDOOR", "600x600_Metal")]
        [InlineData(15129, "IFCWALLSTANDARDCASE", "Basic Wall:Wall_Ext25_MtlLathe5_CemPlstr20:81269")]  // Instance Preferred over Type
        [Theory]
        public void Can_Validate_Types(int entityLabel, string expectedType, string expectedPredefined, bool allowSubType = false, bool shouldPass = true)
        {

            var entity = Model.Instances[entityLabel];
            var propFacet = new IfcTypeFacet
            {
                IfcType = expectedType,
                IncludeSubtypes = allowSubType
            };
            if (expectedPredefined != null)
            {
                propFacet.PredefinedType = expectedPredefined;
            }

            FacetGroup group = BuildGroup(propFacet);
            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, RequirementCardinalityOptions.Expected, result);

            // Assert
            if (shouldPass)
            {

                result.Successful.Should().NotBeEmpty();
                result.Failures.Should().BeEmpty();
                result.ValidationStatus.Should().Be(ValidationStatus.Pass);
            }
            else
            {
                result.Successful.Should().BeEmpty();
                result.Failures.Should().NotBeEmpty();
                result.ValidationStatus.Should().Be(ValidationStatus.Fail);
            }

        }

        [InlineData("IFCELECTRICAPPLIANCETYPE", "", 1, typeof(IIfcElectricApplianceType))] // In 2x3
        [InlineData("IFCELECTRICAPPLIANCE", "", 3, typeof(IIfcFlowTerminal))] // Implicit via Type
        [InlineData("IFCLIGHTFIXTURE", "", 24, typeof(IIfcFlowTerminal))] // Implicit via Type
        [InlineData("IFCLAMP", "", 0, typeof(IIfcFlowTerminal))] // Implicit via Type
        [InlineData("IFCOUTLET", "", 0, typeof(IIfcFlowTerminal))] // Implicit via Type
        [InlineData("IfcSanitaryTerminal", "", 0, typeof(IIfcFlowTerminal))] // Implicit via Type
        [InlineData("IFCDOORTYPE", "", 19, typeof(IIfcDoorStyle))] // Using 4x equivalent in 2x3
        [InlineData("IFCWINDOWTYPE", "", 9, typeof(IIfcWindowStyle))] // Using 4x equivalent in 2x3
        [InlineData("IFCWINDOWTYPE", "Unknown", 0, typeof(IIfcWindowStyle))] // Using 4x equivalent in 2x3
        // [InlineData("IFCLIGHTFIXTURE", "NOTDEFINED", 24, typeof(IIfcFlowTerminal))] // Implicit via Type with Predefined TODO: Needs implementation
        [Theory]
        public void Ifc2x3_Can_Use_Types_From_Newer_Schemas(string ifcType, string predefinedType, int expectedCount, params Type[] expectedTypes)
        {

            AssertIfcTypeFacetQuery(Binder, ifcType, expectedCount, expectedTypes, predefinedType, ifcTypeConstraint: ConstraintType.Exact, includeSubTypes: false);
        }
    }
}
