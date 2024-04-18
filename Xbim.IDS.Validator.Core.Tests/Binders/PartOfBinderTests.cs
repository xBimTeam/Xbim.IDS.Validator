using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;
using static Xbim.InformationSpecifications.PartOfFacet;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class PartOfBinderTests : BaseBinderTests
    {
        public PartOfBinderTests(ITestOutputHelper output) : base(output)
        {
            Binder = new PartOfFacetBinder(BinderContext, Logger);
        }

        /// <summary>
        /// System under test
        /// </summary>
        PartOfFacetBinder Binder { get; }

        ILogger<PartOfFacetBinder> Logger { get => GetLogger<PartOfFacetBinder>(); }


        [InlineData(PartOfRelation.IfcRelAggregates, "IfcProject", 1)]
        [InlineData(PartOfRelation.IfcRelAggregates, "IfcSite", 1)]
        [InlineData(PartOfRelation.IfcRelAggregates, "IfcBuilding", 2)]
        [InlineData(PartOfRelation.IfcRelAggregates, "IFCBUILDINGSTOREY", 4)]
        [InlineData(PartOfRelation.IfcRelAggregates, "IfcBuild.*", 6, ConstraintType.Pattern)]
        [InlineData(PartOfRelation.IfcRelAggregates, "IfcCurtainwall", 26)]
        [InlineData(PartOfRelation.IfcRelAggregates, "", 34)]
        [InlineData(PartOfRelation.IfcRelAggregates, null, 34)]
        [InlineData(PartOfRelation.IfcRelAggregates, "IfcActor", 0)]
        [InlineData(PartOfRelation.IfcRelContainedInSpatialStructure, "IfcSpace", 14)]
        [InlineData(PartOfRelation.IfcRelContainedInSpatialStructure, "IfcBuildingStorey", 20)]
        // TODO: Nests and Groups examples - none in the Sample model currently
        [Theory]
        public void Can_Query_By_PartOf(PartOfRelation relation, string entityType, int expectedCount, 
            ConstraintType sysConType = ConstraintType.Exact)
        {
            var typeFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint()
            };
            PartOfFacet facet = new PartOfFacet
            {
               EntityType = typeFacet
            };
            facet.SetRelation(relation);
            switch (sysConType)
            {
                case ConstraintType.Exact:
                    if (!string.IsNullOrEmpty(entityType)) typeFacet.IfcType.AddAccepted(new ExactConstraint(entityType)); break;

                case ConstraintType.Pattern:
                    typeFacet.IfcType.AddAccepted(new PatternConstraint(entityType)); break;
            }
           

            // Act
            var expression = Binder.BindSelectionExpression(query.InstancesExpression, facet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }


        [InlineData(531, PartOfRelation.IfcRelAggregates, "IfcBuildingStorey")] // Storey has Space
        //[InlineData(531, PartOfRelation.IfcRelAggregates, "IfcDoor")] 

        [InlineData(38397, PartOfRelation.IfcRelContainedInSpatialStructure, "IfcSpace")]
        //[InlineData(38397, PartOfRelation.IfcRelContainedInSpatialStructure, "IfcSite")]
        // TODO: Nests, Groups
        [Theory]
        public void Can_Validate_Parts(int entityLabel, PartOfRelation relation,  string entityType)
        {

            var entity = Model.Instances[entityLabel];
            var typeFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint()
            };
            var propFacet = new PartOfFacet
            {
                EntityType = typeFacet
            };
            typeFacet.IfcType.AddAccepted(new PatternConstraint(entityType));
            propFacet.SetRelation(relation);
            FacetGroup group = BuildGroup(propFacet);
            var result = new IdsValidationResult(entity, group);
            Binder.ValidateEntity(entity, propFacet, Cardinality.Expected, result);

            // Assert

            result.Successful.Should().NotBeEmpty();
            result.Failures.Should().BeEmpty();
            result.ValidationStatus.Should().Be(ValidationStatus.Pass);
        }

        private static FacetGroup BuildGroup(PartOfFacet facet)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var group = new FacetGroup();
#pragma warning restore CS0618 // Type or member is obsolete
            group.RequirementOptions = new System.Collections.ObjectModel.ObservableCollection<RequirementCardinalityOptions>
            {
                new RequirementCardinalityOptions(facet, RequirementCardinalityOptions.Cardinality.Expected)
            };
            group.Facets.Add(facet);
            return group;
        }

    }
}
