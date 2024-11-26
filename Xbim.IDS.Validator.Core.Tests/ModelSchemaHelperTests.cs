using Xbim.IO.Memory;
using Xbim.IDS.Validator.Core.Helpers;
using System.Linq.Expressions;
using FluentAssertions;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;


namespace Xbim.IDS.Validator.Core.Tests
{
    public class ModelSchemaHelperTests
    {

        [Fact]
        public void CanGetAncestor()
        {
            var model = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4x1());

            Expression left = Expression.Constant(model.Instances.OfType<Ifc4.SharedBldgElements.IfcWall>());
            Expression right = Expression.Constant(model.Instances.OfType<Ifc4.SharedBldgElements.IfcWindow>());
            model.GetCommonAncestorType(left, right).Should().Be(typeof(Ifc4.ProductExtension.IfcBuildingElement));
        }


        [Fact]
        public void CanGetAncestorObject()
        {
            var model = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4x1());

            Expression left = Expression.Constant(model.Instances.OfType<Xbim.Ifc4.Kernel.IfcActor>());
            Expression right = Expression.Constant(model.Instances.OfType<Ifc4.SharedBldgElements.IfcWindow>());
            model.GetCommonAncestorType(left, right).Should().Be(typeof(Ifc4.Kernel.IfcObject));
        }

        [Fact]
        public void CanGetAncestorRooted()
        {
            var model = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4x1());

            Expression left = Expression.Constant(model.Instances.OfType<Ifc4.Kernel.IfcPropertySet>());
            Expression right = Expression.Constant(model.Instances.OfType<Ifc4.SharedBldgElements.IfcWindow>());
            model.GetCommonAncestorType(left, right).Should().Be(typeof(Ifc4.Kernel.IfcRoot));
        }

        [Fact]
        public void CanGetAncestorUnRooted()
        {
            var model = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4x1());

            Expression left = Expression.Constant(model.Instances.OfType<Ifc4x3.MaterialResource.IfcMaterial>());
            Expression right = Expression.Constant(model.Instances.OfType<Ifc4.SharedBldgElements.IfcWindow>());
            model.GetCommonAncestorType(left, right).Should().Be(typeof(IPersistEntity));
        }


        [Fact]
        public void CanGetAncestorByInterface()
        {
            var model = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4x1());

            Expression left = Expression.Constant(model.Instances.OfType<IIfcWall>());
            Expression right = Expression.Constant(model.Instances.OfType<IIfcWindow>());
            model.GetCommonAncestorType(left, right).Should().Be(typeof(Ifc4.ProductExtension.IfcBuildingElement));
        }
    }
}
