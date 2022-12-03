using FluentAssertions;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class IfcTypeFacetBinderTests : BaseModelTester
    {
        public IfcTypeFacetBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new IfcTypeFacetBinder(model);
        }

        /// <summary>
        /// System under test
        /// </summary>
        IfcTypeFacetBinder Binder { get; }

        [InlineData("IfcProject", 1, typeof(IIfcProject))]
        [InlineData("IFCPROJECT", 1, typeof(IIfcProject))]
        [InlineData("ifcbuilding", 1, typeof(IIfcBuilding))]
        [InlineData("IfcWall", 5, typeof(IIfcWall))]
        [InlineData("IfcSpace", 4, typeof(IIfcSpace))]
        [InlineData("IfcBuildingElement", 46, typeof(IIfcBuildingElement))] // Abstract
        [InlineData("IfcFlowTerminal", 0, typeof(IIfcFlowTerminal))]
        [InlineData("IfcFurnitureType", 9, typeof(IIfcFurnitureType))]
        [InlineData("IfcFurniture", 14, typeof(IIfcFurniture))]
        [InlineData("IfcFurniture,IfcFurnitureType", 9 + 14, typeof(IIfcFurniture), typeof(IIfcFurnitureType))]
        [InlineData("IfcDoor,IfcWindow,IfcWall", 12, typeof(IIfcWall), typeof(IIfcWindow), typeof(IIfcDoor))]
        [Theory]
        public void Can_Query_Exact_IfcType(string ifcType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, ifcTypeConstraint: ConstraintType.Exact);
        }

        [InlineData("IfcWall.*", 9, typeof(IIfcWall), typeof(IIfcWallType))]
        [InlineData("IfcWall.*pe", 2, typeof(IIfcWallType))]
        [InlineData("IfcWall[a-z]{4}", 2, typeof(IIfcWallType))]
        [InlineData("IfcDoor.*", 12, typeof(IIfcDoor), typeof(IIfcDoorType),
            typeof(IIfcDoorLiningProperties), typeof(IIfcDoorPanelProperties))]
        [InlineData("IfcWall.*,IfcDoor.*", 9 + 12, typeof(IIfcWall), typeof(IIfcWallType),
            typeof(IIfcDoor), typeof(IIfcDoorType),
            typeof(IIfcDoorLiningProperties), typeof(IIfcDoorPanelProperties))]
        [Theory]
        public void Can_Query_IfcType_Patterns(string ifcType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, ifcTypeConstraint: ConstraintType.Pattern);
        }


        [InlineData("IfcInvalid")]
        [InlineData("IfcIdentifier")]
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
            var ex = Record.Exception(() => Binder.BindFilterExpression(query.InstancesExpression, facet));

            // Assert

            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();

        }

        [InlineData("IfcWall", "SOLIDWALL", 1, typeof(IIfcWall))]
        [InlineData("IfcWallStandardCase", "NOTDEFINED", 1, typeof(IIfcWallStandardCase))]
        [InlineData("IfcSpace", "INTERNAL", 1, typeof(IIfcSpace))]
        [InlineData("IfcFurnitureType", "CHAIR", 2, typeof(IIfcFurnitureType))]
        [InlineData("IfcFlowTerminal", "", 0, typeof(IIfcFlowTerminal))]
        [InlineData("IfcWall", "SOLIDWALL,PARTITIONING", 2, typeof(IIfcWall))]
        [InlineData("IfcWall,IfcWallType", "SOLIDWALL,PARTITIONING", 3, typeof(IIfcWall), typeof(IIfcWallType))]
        [InlineData("IfcWallStandardCase,IfcWallType", "NOTDEFINED", 2, typeof(IIfcWallStandardCase), typeof(IIfcWallType))]
        [Theory]
        public void Can_Query_Exact_IfcTypeWith_PredefinedType(string ifcType, string predefinedType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, predefinedType, preConstraint: ConstraintType.Exact);
        }

        [InlineData("IfcWall", "SOLID.*", 1, typeof(IIfcWall))]
        [Theory]
        public void Can_Query_IfcTypeWith_PredefinedType_ByPattern(string ifcType, string predefinedType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, predefinedType, preConstraint: ConstraintType.Pattern);
        }


        private void AssertIfcTypeFacetQuery(string ifcType, int expectedCount, Type[] expectedTypes, string predefinedType = "",
            ConstraintType ifcTypeConstraint = ConstraintType.Exact, ConstraintType preConstraint = ConstraintType.Exact)
        {
            IfcTypeFacet facet = BuildIfcTypeFacetFromCsv(ifcType, predefinedType, ifcTypeConstraint, preDefConstraint: preConstraint);

            // Act
            var expression = Binder.BindFilterExpression(query.InstancesExpression, facet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

            if (expectedCount > 0)
            {
                result.Should().AllSatisfy(t =>
                    expectedTypes.Where(e => e.IsAssignableFrom(t.GetType()))
                    .Should().ContainSingle($"Found {t.GetType().Name}, and expected one of {string.Join(',', expectedTypes.Select(t => t.Name))}"));

            }
        }


        private static IfcTypeFacet BuildIfcTypeFacetFromCsv(string ifcTypeCsv, string predefinedTypeCsv = "",
            ConstraintType ifcConstraint = ConstraintType.Exact, ConstraintType preDefConstraint = ConstraintType.Exact)
        {
            IfcTypeFacet facet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(NetTypeName.String),
                PredefinedType = new ValueConstraint(NetTypeName.String),
            };

            var ifcValues = ifcTypeCsv.Split(',');
            foreach (var ifcVal in ifcValues)
            {
                if (string.IsNullOrEmpty(ifcVal)) continue;
                if (ifcConstraint == ConstraintType.Pattern)
                    facet.IfcType.AddAccepted(new PatternConstraint(ifcVal));
                else
                    facet.IfcType.AddAccepted(new ExactConstraint(ifcVal));
            }

            var pdTypes = predefinedTypeCsv.Split(',');
            foreach (var predef in pdTypes)
            {
                if (string.IsNullOrEmpty(predef)) continue;
                if (preDefConstraint == ConstraintType.Pattern)
                    facet.PredefinedType.AddAccepted(new PatternConstraint(predef));
                else
                    facet.PredefinedType.AddAccepted(new ExactConstraint(predef));
            }
            return facet;
        }


    }
}
