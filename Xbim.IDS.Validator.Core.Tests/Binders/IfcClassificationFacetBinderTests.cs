using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class IfcClassificationFacetBinderTests : BaseModelTester
    {
        public IfcClassificationFacetBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new IfcClassificationFacetBinder(model);
        }

        /// <summary>
        /// System under test
        /// </summary>
        IfcClassificationFacetBinder Binder { get; }

        [InlineData("Uniformat", "E2020200", 7)]
        [InlineData("Uniformat", "E2020.*", 7, ConstraintType.Exact, ConstraintType.Pattern)]
        [InlineData("", "E2020200", 7)]
        [InlineData("Uniformat", "", 8)]
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
            var expression = Binder.BindFilterExpression(query.InstancesExpression, facet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }


        [InlineData("Uniformat", "E2020200")]
        [InlineData(null, "E2020200")]
        [InlineData("Uniformat", null)]
        [InlineData(null, null)]
        [Theory]
        public void CanGetClassificationsReferencesForEntity(string system, string identifier)
        {
            var instance = model.Instances[59964];
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

            var validationResult = new IdsValidationResult(instance, group, "test.ifc");

            Binder.ValidateEntity(instance, group, logger, validationResult, facet);

            foreach(var message in validationResult.Messages) 
            {
                var level = LogLevel.Information;
                if(message.Status == ValidationStatus.Failed)
                    level = LogLevel.Warning;
                logger.Log(level, "Message: {message}", message);
            }

            validationResult.Successful.Should().NotBeEmpty();
            validationResult.Failures.Should().BeEmpty();

        }

        [Fact]
        public void CanGetClassificationsForEntity()
        {
            var project = model.Instances[103];
            IfcClassificationFacet facet = new IfcClassificationFacet
            {
                IncludeSubClasses = false
            };

            facet.ClassificationSystem = new ValueConstraint("Uniformat");
            
#pragma warning disable CS0618 // Type or member is obsolete
            var group = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            group.Facets.Add(facet);

            var validationResult = new IdsValidationResult(project, group, "test.ifc");

            Binder.ValidateEntity(project, group, logger, validationResult, facet);

            foreach (var message in validationResult.Messages)
            {
                var level = LogLevel.Information;
                if (message.Status == ValidationStatus.Failed)
                    level = LogLevel.Warning;
                logger.Log(level, "Message: {message}", message);
            }

            validationResult.Successful.Should().NotBeEmpty();
            validationResult.Failures.Should().BeEmpty();

        }

    }
}
