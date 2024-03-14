using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;
using Xbim.IDS.Validator.Core.Extensions;

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

        [InlineData(37554, "Steel, Chrome Plated")]
        [InlineData(10993, "Door - Handle")]
        [InlineData(1752, "Brick, Common")]
        [InlineData(1752, "Plaster")]
        [Theory]
        public void CanValidateMaterialsForEntity(int entityLabel, string material)
        {
            var instance = Model.Instances[entityLabel];
            MaterialFacet facet = new MaterialFacet()
            {
                Value = new ValueConstraint(material)
            };

#pragma warning disable CS0618 // Type or member is obsolete
            var group = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            group.Facets.Add(facet);

            var validationResult = new IdsValidationResult(instance, group);

            Binder.ValidateEntity(instance, facet, group.GetCardinality(facet), validationResult);

            foreach (var message in validationResult.Messages)
            {
                var level = message.Status == ValidationStatus.Fail ? LogLevel.Warning : LogLevel.Information;
                logger.Log(level, "Message: {message}", message);
            }

            validationResult.Successful.Should().NotBeEmpty();
            validationResult.Failures.Should().BeEmpty();
            validationResult.ValidationStatus.Should().Be(ValidationStatus.Pass);

        }

    }
}
