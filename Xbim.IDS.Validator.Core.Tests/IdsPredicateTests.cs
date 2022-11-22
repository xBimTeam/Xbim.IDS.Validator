using FluentAssertions;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Sdk;

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
        [InlineData("IfcWall", 5, typeof(IIfcWall))]
        [InlineData("IfcSpace", 4, typeof(IIfcSpace))]
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
           
            // Act
            var expression = binder.Bind(query.InstancesExpression, facet);

            // Assert
            
            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount).And.AllBeAssignableTo(expectedType);
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

            // Act
            var expression = binder.Bind(query.InstancesExpression, facet);

            // Assert


            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount).And.AllBeAssignableTo(expectedType);
            
        }

        private static IModel BuildModel()
        {
            var filename = @"TestModels\SampleHouse4.ifc";
            return IfcStore.Open(filename);
        }


    }
}
