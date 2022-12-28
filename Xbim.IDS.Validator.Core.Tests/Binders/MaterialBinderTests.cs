using FluentAssertions;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class MaterialBinderTests : BaseModelTester
    {
        public MaterialBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new MaterialFacetBinder(BinderContext, GetLogger<MaterialFacetBinder>());
        }

        /// <summary>
        /// System under test
        /// </summary>
        MaterialFacetBinder Binder { get; }

        [InlineData("Brick, Common", 3)]
        [InlineData("Vapor Retarder", 1)]
        [InlineData("Brick.*", 3, ConstraintType.Pattern)]
        [InlineData("Concrete.*", 6, ConstraintType.Pattern)]
        [InlineData(".*Sand.*", 3, ConstraintType.Pattern)]
        [Theory]
        public void Can_Query_By_Materials(string materialName, int expectedCount, ConstraintType conType =  ConstraintType.Exact)
        {

            MaterialFacet facet = new MaterialFacet
            {
                Value = new ValueConstraint(NetTypeName.String)
            };
            switch(conType)
            {
                case ConstraintType.Exact:
                    facet.Value.AddAccepted(new ExactConstraint(materialName)); break;

                case ConstraintType.Pattern:
                    facet.Value.AddAccepted(new PatternConstraint(materialName)); break;
            }

            // Act
            var expression = Binder.BindSelectionExpression(query.InstancesExpression, facet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }

       

    }
}
