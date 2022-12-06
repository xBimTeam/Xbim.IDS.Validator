using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        [InlineData("Uniformat", "", 7)]
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

    }
}
