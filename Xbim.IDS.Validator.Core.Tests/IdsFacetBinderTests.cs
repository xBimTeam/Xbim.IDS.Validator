using FluentAssertions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class IdsFacetBinderTests
    {

        private IfcQuery query;
        static IModel model;

        public IdsFacetBinderTests()
        {
            query = new IfcQuery();
        }

        static IdsFacetBinderTests()
        {
            model = BuildModel();
        }

        [InlineData("IfcProject", 1, typeof(IIfcProject))]
        [InlineData("IFCPROJECT", 1, typeof(IIfcProject))]
        [InlineData("ifcbuilding", 1, typeof(IIfcBuilding))]
        [InlineData("IfcWall", 5, typeof(IIfcWall))]
        [InlineData("IfcSpace", 4, typeof(IIfcSpace))]
        [InlineData("IfcBuildingElement", 46, typeof(IIfcBuildingElement))] // Abstract
        [InlineData("IfcFlowTerminal", 0, typeof(IIfcFlowTerminal))]
        [InlineData("IfcFurnitureType", 9, typeof(IIfcFurnitureType))]
        [InlineData("IfcFurniture", 14, typeof(IIfcFurniture))]
        [InlineData("IfcFurniture,IfcFurnitureType", 9 + 14, typeof(IIfcFurniture), typeof(IIfcFurnitureType))]
        [InlineData("IfcDoor,IfcWindow,IfcWall", 12, typeof(IIfcWall), typeof(IIfcWindow), typeof(IIfcDoor))]
        [Theory]
        public void Can_Query_Exact_IfcType(string ifcType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, ifcTypeConstraint: ConstraintType.Exact);
        }

        [InlineData("IfcWall.*", 9, typeof(IIfcWall), typeof(IIfcWallType))]
        [InlineData("IfcWall.*pe", 2, typeof(IIfcWallType))]
        [InlineData("IfcWall[a-z]{4}", 2, typeof(IIfcWallType))]
        [InlineData("IfcDoor.*", 12, typeof(IIfcDoor), typeof(IIfcDoorType), 
            typeof(IIfcDoorLiningProperties), typeof(IIfcDoorPanelProperties))]
        [InlineData("IfcWall.*,IfcDoor.*", 9 + 12, typeof(IIfcWall), typeof(IIfcWallType), 
            typeof(IIfcDoor), typeof(IIfcDoorType),
            typeof(IIfcDoorLiningProperties), typeof(IIfcDoorPanelProperties))]
        [Theory]
        public void Can_Query_IfcType_Patterns(string ifcType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, ifcTypeConstraint: ConstraintType.Pattern);
        }


        [InlineData("IfcInvalid")]
        [InlineData("IfcIdentifier")]
        [InlineData("1234")]
        [InlineData("")]
        [InlineData("*")]
        [InlineData(" ")]
        [Theory]
        public void Invalid_IfcTypes_Handled(string ifcType)
        {
            IfcTypeFacet facet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType)
            };

            var binder = new IfcTypeFacetBinder(model);

            // Act
            var ex = Record.Exception(() => binder.BindFilterExpression(query.InstancesExpression, facet));

            // Assert

            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();

        }

        [InlineData("IfcWall", "SOLIDWALL", 1, typeof(IIfcWall))]
        [InlineData("IfcWallStandardCase", "NOTDEFINED", 1, typeof(IIfcWallStandardCase))]
        [InlineData("IfcSpace", "INTERNAL", 1, typeof(IIfcSpace))]
        [InlineData("IfcFurnitureType", "CHAIR", 2, typeof(IIfcFurnitureType))]
        [InlineData("IfcFlowTerminal", "", 0, typeof(IIfcFlowTerminal))]
        [InlineData("IfcWall", "SOLIDWALL,PARTITIONING", 2, typeof(IIfcWall))]
        [InlineData("IfcWall,IfcWallType", "SOLIDWALL,PARTITIONING", 3, typeof(IIfcWall), typeof(IIfcWallType))]
        [InlineData("IfcWallStandardCase,IfcWallType", "NOTDEFINED", 2, typeof(IIfcWallStandardCase), typeof(IIfcWallType))]
        [Theory]
        public void Can_Query_Exact_IfcTypeWith_PredefinedType(string ifcType, string predefinedType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, predefinedType, preConstraint: ConstraintType.Exact);
        }

        [InlineData("IfcWall", "SOLID.*", 1, typeof(IIfcWall))]
        [Theory]
        public void Can_Query_IfcTypeWith_PredefinedType_ByPattern(string ifcType, string predefinedType, int expectedCount, params Type[] expectedTypes)
        {
            AssertIfcTypeFacetQuery(ifcType, expectedCount, expectedTypes, predefinedType, preConstraint: ConstraintType.Pattern);
        }

        private void AssertIfcTypeFacetQuery(string ifcType, int expectedCount, Type[] expectedTypes, string predefinedType = "",
            ConstraintType ifcTypeConstraint = ConstraintType.Exact, ConstraintType preConstraint = ConstraintType.Exact)
        {
            IfcTypeFacet facet = BuildIfcTypeFacetFromCsv(ifcType, predefinedType, ifcTypeConstraint, preDefConstraint: preConstraint);

            var binder = new IfcTypeFacetBinder(model);
            // Act
            var expression = binder.BindFilterExpression(query.InstancesExpression, facet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

            if (expectedCount > 0)
            {
                result.Should().AllSatisfy(t =>
                    expectedTypes.Where(e => e.IsAssignableFrom(t.GetType()))
                    .Should().ContainSingle($"Found {t.GetType().Name}, and expected one of {string.Join(',', expectedTypes.Select(t => t.Name))}"));

            }
        }

        [InlineData("IfcFurnitureType", nameof(IIfcFurnitureType.AssemblyPlace), "FACTORY", 1)]
        [InlineData("IfcFurniture", nameof(IIfcFurniture.ObjectType), "1525x762mm", 1)]
        [InlineData("IfcWall", nameof(IIfcWall.GlobalId), "3cUkl32yn9qRSPvBJVyWw5", 1)]
        [InlineData("IfcWall", nameof(IIfcWall.Name), "Basic Wall:Wall-Ext_102Bwk-75Ins-100LBlk-12P:285330", 1)]
        [InlineData("IfcSpace", nameof(IIfcSpace.Description), "Lounge", 1)]
        [Theory]
        public void Can_Query_By_Ifc_And_Attributes(string ifcType, string attributeFieldName, string attributeValue, int expectedCount)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };
            
            AttributeFacet attrFacet = new AttributeFacet
            {
                AttributeName = attributeFieldName,
                AttributeValue = new ValueConstraint(attributeValue)
            };
            var ifcbinder = new IfcTypeFacetBinder(model);

            var attrbinder = new AttributeFacetBinder(model);

            // Act
            var expression = ifcbinder.BindFilterExpression(query.InstancesExpression, ifcFacet);
            expression = attrbinder.BindFilterExpression(expression, attrFacet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }

        
        [InlineData(nameof(IIfcFurniture.ObjectType), "Chair - Dining", 6)]
        [Theory]
        public void Can_Query_By_Attributes(string attributeFieldName, string attributeValue, int expectedCount)
        {

            AttributeFacet attrFacet = new AttributeFacet
            {
                AttributeName = attributeFieldName,
                AttributeValue = new ValueConstraint(attributeValue)
            };
            var binder = new AttributeFacetBinder(model);
            
            // Act
            var expression = binder.BindFilterExpression(query.InstancesExpression, attrFacet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }

        [InlineData(nameof(IIfcFurniture.ObjectType), "Chair.*", 6)]
        [Theory]
        public void Can_Query_By_Attributes_Patterns(string attributeFieldName, string attributeValue, int expectedCount)
        {

            AttributeFacet attrFacet = new AttributeFacet
            {
                AttributeName = attributeFieldName,
                AttributeValue = new ValueConstraint()
            };
            attrFacet.AttributeValue.AddAccepted(new PatternConstraint(attributeValue));

            var binder = new AttributeFacetBinder(model);

            // Act
            var expression = binder.BindFilterExpression(query.InstancesExpression, attrFacet);

            // Assert

            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }

        [InlineData("IfcWall", "IfcOwnerHistory")]
        [InlineData("IfcWall", "IfcRelAggregates")]
        [InlineData("IfcWall", "*")]
        [InlineData("IfcWall", " ")]
        [Theory]
        public void Invalid_Attributes_Handled(string ifcType, string attributeFieldName)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };

            AttributeFacet attrFacet = new AttributeFacet
            {
                AttributeName = attributeFieldName,
                AttributeValue = new ValueConstraint("not relevant")
            };
            var ifcBinder = new IfcTypeFacetBinder(model);
            var attrBinder = new AttributeFacetBinder(model);
            
            var expression = ifcBinder.BindFilterExpression(query.InstancesExpression, ifcFacet);
            // Act
            var ex = Record.Exception(() => attrBinder.BindFilterExpression(expression, attrFacet));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<InvalidOperationException>();

        }

        [InlineData("PSet_WallCommon", "LoadBearing", "false", 5)]
        [InlineData("PSet_WallCommon", "LoadBearing", "true", 0)]
        [InlineData("Pset_MemberCommon", "LoadBearing", "true", 20)]
        [InlineData("Pset_PlateCommon", "ThermalTransmittance", "6.7069", 6)]

        [InlineData("Pset_MemberCommon", "Span", null, 20)]
        [InlineData("Pset_MemberCommon", "Span", "2043.570045136", 1)]
        [Theory]
        public void Can_Query_By_Properties(string psetName, string propName, string propValue, int expectedCount)
        {

            IfcPropertyFacet propFacet = new IfcPropertyFacet
            {
                PropertySetName = psetName,
                PropertyName = propName,
                PropertyValue = propValue
            };
            var binder = new PsetFacetBinder(model);

            // Act
            var expression = binder.BindFilterExpression(query.InstancesExpression, propFacet);

            // Assert
            
            var result = query.Execute(expression, model);
            result.Should().HaveCount(expectedCount);

        }

        private static IfcTypeFacet BuildIfcTypeFacetFromCsv(string ifcTypeCsv, string predefinedTypeCsv = "", 
            ConstraintType ifcConstraint = ConstraintType.Exact, ConstraintType preDefConstraint = ConstraintType.Exact)
        {
            IfcTypeFacet facet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(NetTypeName.String),
                PredefinedType = new ValueConstraint(NetTypeName.String),
            };
            
            var ifcValues = ifcTypeCsv.Split(',');
            foreach (var ifcVal in ifcValues) {
                if (string.IsNullOrEmpty(ifcVal)) continue;
                if (ifcConstraint == ConstraintType.Pattern)
                    facet.IfcType.AddAccepted(new PatternConstraint(ifcVal));
                else
                    facet.IfcType.AddAccepted(new ExactConstraint(ifcVal)); 
            }

            var pdTypes = predefinedTypeCsv.Split(',');
            foreach (var predef in pdTypes) 
            {
                if (string.IsNullOrEmpty(predef)) continue;
                if (preDefConstraint == ConstraintType.Pattern)
                    facet.PredefinedType.AddAccepted(new PatternConstraint(predef)); 
                else
                    facet.PredefinedType.AddAccepted(new ExactConstraint(predef));
            }
            return facet;
        }

        private static IModel BuildModel()
        {
            var filename = @"TestModels\SampleHouse4.ifc";
            //IfcStore.ModelProviderFactory.UseEsentModelProvider();
            return IfcStore.Open(filename);
            
        }


        private enum ConstraintType
        {
            Exact,
            Pattern,
            Range,
            Structure
        }
    }

}
