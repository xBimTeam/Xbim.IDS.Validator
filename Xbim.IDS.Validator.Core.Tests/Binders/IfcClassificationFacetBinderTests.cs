using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class IfcClassificationFacetBinderTests : BaseModelTester
    {
        public IfcClassificationFacetBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new IfcClassificationFacetBinder(BinderContext, GetLogger<IfcClassificationFacetBinder>());
        }

        /// <summary>
        /// System under test
        /// </summary>
        IfcClassificationFacetBinder Binder { get; }

      
        [InlineData("Uniclass", "EF_25_30_97", 5)] // Via Type
        [InlineData("Uniclass", "Pr_40_50_12", 7)]
        [InlineData("Uniclass", "Pr_40_50.*", 7, ConstraintType.Exact, ConstraintType.Pattern)]
        [InlineData("", "Pr_40_50_12", 7)]
        [InlineData("Uniclass", "", 12)]
        [Theory]
        public void Can_Query_By_Classifications(string system, string identifier, int expectedCount, ConstraintType sysConType = ConstraintType.Exact, ConstraintType idConType = ConstraintType.Exact)
        {

            IfcClassificationFacet facet = new IfcClassificationFacet
            {
                ClassificationSystem = new ValueConstraint(NetTypeName.String),
                Identification = new ValueConstraint(NetTypeName.String),
                IncludeSubClasses = false
            };
            switch (sysConType)
            {
                case ConstraintType.Exact:
                    if(!string.IsNullOrEmpty(system)) facet.ClassificationSystem.AddAccepted(new ExactConstraint(system)); break;

                case ConstraintType.Pattern:
                    facet.ClassificationSystem.AddAccepted(new PatternConstraint(system)); break;
            }
            switch (idConType)
            {
                case ConstraintType.Exact:
                    if (!string.IsNullOrEmpty(identifier)) facet.Identification.AddAccepted(new ExactConstraint(identifier)); break;

                case ConstraintType.Pattern:
                    facet.Identification.AddAccepted(new PatternConstraint(identifier)); break;
            }

            // Act
            var expression = Binder.BindSelectionExpression(query.InstancesExpression, facet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }


        [InlineData("Uniclass", "Pr_40_50_12")]
        [InlineData(null, "Pr_40_50_12")]
        [InlineData("Uniclass", null)]
        [InlineData(null, null)]
        [Theory]
        public void CanGetClassificationsReferencesForEntity(string system, string identifier)
        {
            var instance = Model.Instances[59964];
            IfcClassificationFacet facet = new IfcClassificationFacet
            {
                IncludeSubClasses = false
            };

            if (system != null) facet.ClassificationSystem = new ValueConstraint(system);
            if (identifier != null) facet.Identification = new ValueConstraint(identifier);
#pragma warning disable CS0618 // Type or member is obsolete
            var group = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            group.Facets.Add(facet);

            var validationResult = new IdsValidationResult(instance, group);

            Binder.ValidateEntity(instance, facet, group.GetCardinality(facet), validationResult);

            foreach(var message in validationResult.Messages) 
            {
                var level = LogLevel.Information;
                if(message.Status == ValidationStatus.Fail)
                    level = LogLevel.Warning;
                logger.Log(level, "Message: {message}", message);
            }

            validationResult.Successful.Should().NotBeEmpty();
            validationResult.Failures.Should().BeEmpty();

        }

        [Fact]
        public void CanGetClassificationsForEntity()
        {
            var project = Model.Instances[103];
            IfcClassificationFacet facet = new IfcClassificationFacet
            {
                IncludeSubClasses = false
            };

            facet.ClassificationSystem = new ValueConstraint("Uniclass");
            
#pragma warning disable CS0618 // Type or member is obsolete
            var group = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            group.Facets.Add(facet);

            var validationResult = new IdsValidationResult(project, group);

            Binder.ValidateEntity(project, facet, group.GetCardinality(facet), validationResult);

            foreach (var message in validationResult.Messages)
            {
                var level = LogLevel.Information;
                if (message.Status == ValidationStatus.Fail)
                    level = LogLevel.Warning;
                logger.Log(level, "Message: {message}", message);
            }

            validationResult.Successful.Should().NotBeEmpty();
            validationResult.Failures.Should().BeEmpty();

        }

    }
}
