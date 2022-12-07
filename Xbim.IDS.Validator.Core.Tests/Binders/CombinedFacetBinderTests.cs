using FluentAssertions;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class CombinedFacetBinderTests: BaseModelTester
    {

        public CombinedFacetBinderTests(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData("IfcFurnitureType", nameof(IIfcFurnitureType.AssemblyPlace), "FACTORY", 1)]
        [InlineData("IfcFurniture", nameof(IIfcFurniture.ObjectType), "1525x762mm", 1)]
        [InlineData("IfcWall", nameof(IIfcWall.GlobalId), "3cUkl32yn9qRSPvBJVyWw5", 1)]
        [InlineData("IfcWall", nameof(IIfcWall.Name), "Basic Wall:Wall-Ext_102Bwk-75Ins-100LBlk-12P:285330", 1)]
        [InlineData("IfcSpace", nameof(IIfcSpace.Description), "Lounge", 1)]
        [Theory]
        public void Can_Query_By_Ifc_And_Attributes(string ifcType, string attributeFieldName, string attributeValue, int expectedCount)
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
            var ifcbinder = new IfcTypeFacetBinder(model);

            var attrbinder = new AttributeFacetBinder(model);

            // Act
            var expression = ifcbinder.BindFilterExpression(query.InstancesExpression, ifcFacet);
            expression = attrbinder.BindFilterExpression(expression, attrFacet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }

        [InlineData("IfcFurniture", "Uniclass", "Pr_40_50_12", 4)]
        [InlineData("IfcFurnitureType", "Uniclass", "Pr_40_50_12", 3)]
        [InlineData("IfcProject", "Uniclass", "", 1)]
        [InlineData("IfcMaterial", "Uniclass", "", 0)]
        [InlineData("IfcWindowType", "Uniclass", "", 1)]
        
        // TODO: Fix Filtering on Type inherited
        //[InlineData("IfcWindow", "Uniclass", "", 4)]
        // TODO: handle invalid types
        //[InlineData("IfcProperty", "Uniclass", "", 0)]
        [Theory]
        public void Can_Query_By_Ifc_And_Classifications(string ifcType, string system, string ident, int expectedCount)
        {

            
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };

            IfcClassificationFacet classFacet = new IfcClassificationFacet
            {
                ClassificationSystem = system,
                Identification = new ValueConstraint(ident)
            };
            var ifcbinder = new IfcTypeFacetBinder(model);

            var classbinder = new IfcClassificationFacetBinder(model);

            // Act
            var expression = ifcbinder.BindFilterExpression(query.InstancesExpression, ifcFacet);
            expression = classbinder.BindFilterExpression(expression, classFacet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }

        //TODO: Tests for Materials, Psets, Docs etc


    }

}
