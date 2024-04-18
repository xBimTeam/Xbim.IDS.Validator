using Xbim.InformationSpecifications;
using Xbim.IDS.Validator.Core.Extensions;
using FluentAssertions;
using Xbim.IO.CobieExpress;
using Xbim.IDS.Validator.Core.Configuration;
using Xbim.CobieExpress;
using Xbim.IDS.Validator.Common.Interfaces;

namespace Xbim.IDS.Validator.Extensions.COBie.Tests
{
    [Collection(nameof(COBieTestEnvironment))]
    public class ValueConstraintExtensionsTests
    {
        [Fact]
        public void CanSatisfyComplexObjects()
        {
            var mapper = GetValueMapper();

            ValueConstraint constraint = new ValueConstraint("test");

            constraint.SatisfiesConstraint(new Ifc4.MeasureResource.IfcLabel("test"), mapper).Should().BeTrue("For IfcLabel");
            constraint.SatisfiesConstraint(new Ifc4.MeasureResource.IfcIdentifier("test"), mapper).Should().BeTrue("For IfcIdentifier");
            constraint.SatisfiesConstraint(new Ifc4.MeasureResource.IfcText("test"), mapper).Should().BeTrue("For IfcText");


            constraint = new ValueConstraint(true.ToString().ToUpperInvariant());

            constraint.SatisfiesConstraint(new Ifc4.MeasureResource.IfcBoolean(true), mapper).Should().BeTrue("For IfcBoolean");
            constraint.SatisfiesConstraint(true, mapper).Should().BeTrue("For bool");


            constraint = new ValueConstraint(100d);
            constraint.SatisfiesConstraint(new Ifc4.MeasureResource.IfcAreaMeasure(100d), mapper).Should().BeTrue("For IfcAreaMeasure");
            constraint.SatisfiesConstraint(100d, mapper).Should().BeTrue(because: "For double");

        }
        
        [Fact]
        public void CanSatisfyComplexCobieObjects()
        {
            var mapper = GetValueMapper();
            mapper.AddMap<CobieRole>(v => v.Value);
            ValueConstraint constraint = new ValueConstraint("cobie");

            using var model = new CobieModel();
            using var txn = model.BeginTransaction("Cobie Data");

            var role = model.Instances.New<Xbim.CobieExpress.CobieRole>();
            role.Value = "cobie";
            constraint.SatisfiesConstraint(role, mapper).Should().BeTrue("For CobieRole");


        }

        [Fact]
        public void CanSatisfyComplexCobieAsset()
        {
            var mapper = GetValueMapper();
            mapper.AddMap<CobieComponent>(v => v.Name);
            ValueConstraint constraint = new ValueConstraint("cobie");

            using var model = new CobieModel();
            using var txn = model.BeginTransaction("Cobie Data");

            var role = model.Instances.New<Xbim.CobieExpress.CobieComponent>();
            role.Name = "cobie";
            constraint.SatisfiesConstraint(role, mapper).Should().BeTrue("For CobieComponent");


        }

        protected IValueMapper GetValueMapper()
        {
            return new IdsValueMapper(new[] { new IdsValueMapProvider() });
        }
    }
}
