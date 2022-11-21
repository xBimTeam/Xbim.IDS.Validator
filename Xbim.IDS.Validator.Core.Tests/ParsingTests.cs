using FluentAssertions;
using Xbim.Ifc4.Interfaces;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class ParsingTests
    {

        [Fact]
        public void ParserLoadsModel()
        {
            var parser = new IdsParser(@"TestModels\SampleHouse4.ifc");
            parser.Model.Should().NotBeNull();
        }


        [Fact]
        public void CanSelectApplicableEntitiesByTypeName()
        {
            var parser = new IdsParser(@"TestModels\SampleHouse4.ifc");

            var results = parser.GetProducts("IfcWall");

            results.Should().AllBeAssignableTo<IIfcWall>();


        }

        [Fact]
        public void CanSelectApplicableEntitiesByTypeNameAndPredefinedType()
        {
            var parser = new IdsParser(@"TestModels\SampleHouse4.ifc");

            var results = parser.GetProducts("IfcWall", "SOLIDWALL");

            results.Should().AllSatisfy((a) =>
            {
                a.Should().BeOfType<IIfcWall>();
                var w = a as IIfcWall;
                w.Should().NotBeNull();
                w?.PredefinedType.Should().Be(IfcWallTypeEnum.SOLIDWALL);
            });


        }

        [Fact]
        public void CanSelectApplicableEntitiesByIncludingSubClass()
        {
            var parser = new IdsParser(@"TestModels\SampleHouse4.ifc");

            var results = parser.GetProducts("IfcWall");

            results.OfType<IIfcWallStandardCase>().Should().NotBeEmpty();


        }

        [Fact]
        public void CanSelectApplicableEntitiesCaseInsensitive()
        {
            var parser = new IdsParser(@"TestModels\SampleHouse4.ifc");

            var results = parser.GetProducts("ifcwall");

            results.Should().AllBeAssignableTo<IIfcWall>();


        }
    }
}