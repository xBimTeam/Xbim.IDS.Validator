using FluentAssertions;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class IdsPredicateTests
    {

        private IfcQuery query;
        static IModel model;

        public IdsPredicateTests()
        {
            query = new IfcQuery();
        }

        static IdsPredicateTests()
        {
            model = BuildModel();
        }

        [InlineData("IfcProject", 1, typeof(IIfcProject))]
        [InlineData("IFCPROJECT", 1, typeof(IIfcProject))]
        [InlineData("ifcbuilding", 1, typeof(IIfcBuilding))]
        [InlineData("IfcWall", 5, typeof(IIfcWall))]
        [InlineData("IfcSpace", 4, typeof(IIfcSpace))]
        [InlineData("IfcBuildingElement", 46, typeof(IIfcBuildingElement))] // Abstract
        [InlineData("IfcFurnitureType", 9, typeof(IIfcFurnitureType))]
        [InlineData("IfcFlowTerminal", 0, typeof(IIfcFlowTerminal))]
        [Theory]
        public void Can_Query_By_IfcType(string ifcType, int expectedCount, Type expectedType)
        {
            IfcTypeFacet facet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType)
            };

            var binder = new IdsFacetBinder(model);
            var expressType = binder.GetExpressType(facet);
           
            // Act
            var expression = binder.Bind(query.InstancesExpression, facet, expressType);

            // Assert
            
            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount).And.AllBeAssignableTo(expectedType);
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

            var binder = new IdsFacetBinder(model);
            var expressType = binder.GetExpressType(facet);

            // Act
            var ex = Record.Exception(() => binder.Bind(query.InstancesExpression, facet, expressType));

            // Assert

            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();

        }

        [InlineData("IfcWall", "SOLIDWALL", 1, typeof(IIfcWall))]
        [InlineData("IfcSpace", "INTERNAL", 1, typeof(IIfcSpace))]
        [InlineData("IfcFurnitureType", "CHAIR", 2, typeof(IIfcFurnitureType))]
        [InlineData("IfcFlowTerminal", "", 0, typeof(IIfcFlowTerminal))]
        [Theory]
        public void Can_Query_By_IfcTypeWith_PredefinedType(string ifcType, string predefinedType, int expectedCount, Type expectedType)
        {
            IfcTypeFacet facet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
                PredefinedType = new ValueConstraint(predefinedType)
            };

            var binder = new IdsFacetBinder(model);
            var expressType = binder.GetExpressType(facet);
            // Act
            var expression = binder.Bind(query.InstancesExpression, facet, expressType);

            // Assert


            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount).And.AllBeAssignableTo(expectedType);
            
        }

        [InlineData("IfcFurnitureType", nameof(IIfcFurnitureType.AssemblyPlace), "FACTORY", 1)]
        [InlineData("IfcFurniture", nameof(IIfcFurniture.ObjectType), "1525x762mm", 1)]
        [InlineData("IfcWall", nameof(IIfcWall.GlobalId), "3cUkl32yn9qRSPvBJVyWw5", 1)]
        [InlineData("IfcWall", nameof(IIfcWall.Name), "Basic Wall:Wall-Ext_102Bwk-75Ins-100LBlk-12P:285330", 1)]
        [InlineData("IfcSpace", nameof(IIfcSpace.Description), "Lounge", 1)]
        [Theory]
        public void Can_Query_By_AdditionalAttributes(string ifcType, string attributeFieldName, string attributeValue, int expectedCount)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };
            
            AttributeFacet attrFacet = new AttributeFacet
            {
                AttributeName = attributeFieldName,
                AttributeValue = new ValueConstraint(attributeValue)
            };
            var binder = new IdsFacetBinder(model);
            var expressType = binder.GetExpressType(ifcFacet);
            var expression = binder.Bind(query.InstancesExpression, ifcFacet, expressType);
            // Act
            expression = binder.Bind(expression, attrFacet, expressType);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }

        [InlineData("IfcWall", "IfcOwnerHistory")]
        [InlineData("IfcWall", "IfcRelAggregates")]
        [InlineData("IfcWall", "*")]
        [InlineData("IfcWall", " ")]
        [Theory]
        public void Invalid_Attributes_Handled(string ifcType, string attributeFieldName)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };

            AttributeFacet attrFacet = new AttributeFacet
            {
                AttributeName = attributeFieldName,
                AttributeValue = new ValueConstraint("not relevant")
            };
            var binder = new IdsFacetBinder(model);
            var expressType = binder.GetExpressType(ifcFacet);
            var expression = binder.Bind(query.InstancesExpression, ifcFacet, expressType);
            // Act
            var ex = Record.Exception(() => binder.Bind(expression, attrFacet, expressType));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();

        }

        private static IModel BuildModel()
        {
            var filename = @"TestModels\SampleHouse4.ifc";
            return IfcStore.Open(filename);
        }


    }
}
