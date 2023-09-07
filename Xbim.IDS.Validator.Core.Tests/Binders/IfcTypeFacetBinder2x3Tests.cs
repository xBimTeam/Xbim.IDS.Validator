using FluentAssertions;
using Xbim.IDS.Validator.Core.Binders;
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
            }
            else
            {
                result.Successful.Should().BeEmpty();
                result.Failures.Should().NotBeEmpty();
            }

        }
    }
}
