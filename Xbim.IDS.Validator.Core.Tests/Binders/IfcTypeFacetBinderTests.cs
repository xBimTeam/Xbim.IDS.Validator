using FluentAssertions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.Ifc4;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class IfcTypeFacetBinderTests : BaseBinderTests
    {
        public IfcTypeFacetBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new IfcTypeFacetBinder(GetLogger<IfcTypeFacetBinder>());
            Binder.Initialise(BinderContext);
        }

        /// <summary>
        /// System under test
        /// </summary>
        IfcTypeFacetBinder Binder { get; }

        [InlineData("IfcProject", 1, typeof(IIfcProject))]
        [InlineData("IFCPROJECT", 1, typeof(IIfcProject))]
        [InlineData("ifcbuilding", 1, typeof(IIfcBuilding))]
        [InlineData("IfcWall", 5, typeof(IIfcWall))]    // Includes Standardcase
        [InlineData("IfcWallStandardCase", 2, typeof(IIfcWallStandardCase))]
        [InlineData("IfcSpace", 4, typeof(IIfcSpace))]
        [InlineData("IfcBuildingElement", 46, typeof(IIfcBuildingElement))] // Abstract Supertype
        [InlineData("IfcFlowTerminal", 0, typeof(IIfcFlowTerminal))]
        [InlineData("IfcFurnitureType", 9, typeof(IIfcFurnitureType))]
        [InlineData("IfcFurniture", 14, typeof(IIfcFurniture))]
        [InlineData("IfcFurnishingElement", 14, typeof(IIfcFurnishingElement))]
        [InlineData("IfcFurniture,IfcFurnitureType", 9 + 14, typeof(IIfcFurniture), typeof(IIfcFurnitureType))]
        [InlineData("IfcDoor,IfcWindow,IfcWall", 12, typeof(IIfcWall), typeof(IIfcWindow), typeof(IIfcDoor))]
        [Theory]
        public void Can_Query_Exact_IfcType(string ifcType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(Binder, ifcType, expectedCount, expectedTypes, ifcTypeConstraint: ConstraintType.Exact);
        }


        
        [InlineData("IfcWall", 3, typeof(IIfcWall))]
        [InlineData("IfcBuildingElement", 0, typeof(IIfcBuildingElement))] //
        [InlineData("IfcFurnishingElement", 0, typeof(IIfcFurnishingElement))]
        [Theory]
        public void Can_Query_Exact_IfcType_WithoutSubtypes(string ifcType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(Binder, ifcType, expectedCount, expectedTypes, ifcTypeConstraint: ConstraintType.Exact, includeSubTypes: false);
        }

        [InlineData("IfcWall.*", 9, typeof(IIfcWall), typeof(IIfcWallType))]
        [InlineData("IfcWall.*pe", 2, typeof(IIfcWallType))]
        [InlineData("IfcWall[a-z]{4}", 2, typeof(IIfcWallType))]
        [InlineData("IfcDoor.*", 12, typeof(IIfcDoor), typeof(IIfcDoorType),
            typeof(IIfcDoorLiningProperties), typeof(IIfcDoorPanelProperties))]
        [InlineData("IfcWall.*,IfcDoor.*", 9 + 12, typeof(IIfcWall), typeof(IIfcWallType),
            typeof(IIfcDoor), typeof(IIfcDoorType),
            typeof(IIfcDoorLiningProperties), typeof(IIfcDoorPanelProperties))]
        [InlineData("^(?!IFCPROJECT|IFCBUILDING|IFCBUILDINGSTOREY|IFCSITE)(.+)", 176496, typeof(IPersistEntity))]
        [Theory]
        public void Can_Query_IfcType_Patterns(string ifcType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(Binder, ifcType, expectedCount, expectedTypes, ifcTypeConstraint: ConstraintType.Pattern);
        }


        [InlineData("IfcInvalid")]
        [InlineData("IfcIdentifier")]
        [InlineData("IfcStrippedOptional")]
        [InlineData("1234")]
        [InlineData("")]
        [InlineData("*")]
        [InlineData(" ")]
        [Theory]
        public void Invalid_IfcTypes_Handled(string ifcType)
        {
            IfcTypeFacet facet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType)
            };

            // Act
            var ex = Record.Exception(() => Binder.BindSelectionExpression(query.InstancesExpression, facet));

            // Assert

            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();

        }

        [InlineData("IfcWall", "SOLIDWALL", 1, typeof(IIfcWall))]
        [InlineData("IfcWindowType", "USERDEFINED", 1, typeof(IIfcWindowType))]
        [InlineData("IfcWallStandardCase", "PARTITIONING", 2, typeof(IIfcWallStandardCase))]
        [InlineData("IfcSpace", "INTERNAL", 1, typeof(IIfcSpace))]
        [InlineData("IfcFurnitureType", "CHAIR", 2, typeof(IIfcFurnitureType))]
        [InlineData("IfcFlowTerminal", "", 0, typeof(IIfcFlowTerminal))]
        [InlineData("IfcWall", "SOLIDWALL,PARTITIONING", 3, typeof(IIfcWall))]
        [InlineData("IfcWall,IfcWallType", "SOLIDWALL,PARTITIONING", 4, typeof(IIfcWall), typeof(IIfcWallType))]
        [InlineData("IfcWallStandardCase,IfcWallType", "NOTDEFINED", 1, typeof(IIfcWallStandardCase), typeof(IIfcWallType))]
        [InlineData("IfcSlab", "FLOOR", 2, typeof(IIfcSlab))]
        [InlineData("IfcDoor", "DOOR", 3, typeof(IIfcDoor))]
        [InlineData("IfcFurniture", "1525x762mm", 1, typeof(IIfcFurniture))]
        [Theory]
        public void Can_Query_Exact_IfcTypeWith_PredefinedType(string ifcType, string predefinedType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(Binder, ifcType, expectedCount, expectedTypes, predefinedType, preConstraint: ConstraintType.Exact);
        }

        [InlineData("IfcWall", "SOLID.*", 1, typeof(IIfcWall))]
        [Theory]
        public void Can_Query_IfcTypeWith_PredefinedType_ByPattern(string ifcType, string predefinedType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(Binder, ifcType, expectedCount, expectedTypes, predefinedType, preConstraint: ConstraintType.Pattern);
        }



        [InlineData(10993, "IFCDOOR", "DOOR")]
        //[InlineData(10993, "IFCDOOR", "Door")]
        //[InlineData(10993, "IfcDoor", "DOOR")]
        [InlineData(10993, "IFCDOOR", null)]
        [InlineData(4705, "IFCWALLSTANDARDCASE", "PARTITIONING")] // Matches exact Type
        [InlineData(4705, "IFCWALL", "PARTITIONING", true)] // Matches Subtype
        [InlineData(4705, "IFCACTOR", null, true, false)] // Won't match as nota subtype
        [Theory]
        public void Can_Validate_Types(int entityLabel, string expectedType, string expectedPredefined, bool allowSubType = false, bool shouldPass = true)
        {

            var entity = Model.Instances[entityLabel];
            var propFacet = new IfcTypeFacet
            {
                IfcType = expectedType,
                IncludeSubtypes = allowSubType
            };
            if(expectedPredefined != null)
            {
                propFacet.PredefinedType = expectedPredefined;
            }
            
            FacetGroup group = BuildGroup(propFacet);
            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, Cardinality.Expected, result);

            // Assert
            if(shouldPass)
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





    }
}
