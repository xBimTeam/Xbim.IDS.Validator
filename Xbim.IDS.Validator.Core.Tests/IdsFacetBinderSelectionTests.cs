using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class IdsFacetBinderSelectionTests
    {
        private IfcQuery query;
        static IModel model;

        public IdsFacetBinderSelectionTests()
        {
            query = new IfcQuery();
        }

        static IdsFacetBinderSelectionTests()
        {
            model = BuildModel();
        }



        [InlineData(421, "Pset_SpaceCommon", "IsExternal", false)]
        [InlineData(323, "Energy Analysis", "Area per Person", 28.5714285714286d)]
        [InlineData(323, "Dimensions", "Area", 15.41678125d)]
        [InlineData(10942, "Other", "Category", "Doors")] // Type
        [Theory]
        public void Can_Select_Properties(int entityLabel, string psetName, string propName, object expectedtext)
        {
            IfcPropertyFacet propFacet = new IfcPropertyFacet
            {
                PropertySetName = new ValueConstraint(psetName),
                PropertyName = new ValueConstraint(propName),
                //PropertyValue = 
            };


            var binder = new IdsFacetBinder(model);

            var result = binder.GetProperty(entityLabel, psetName, propName);

            // Assert

            result.Value.Should().Be(expectedtext);

        }

        private static IModel BuildModel()
        {
            var filename = @"TestModels\SampleHouse4.ifc";
            return IfcStore.Open(filename);
        }
    }
}
