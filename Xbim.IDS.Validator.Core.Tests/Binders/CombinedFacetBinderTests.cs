using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;
using static Xbim.InformationSpecifications.PartOfFacet;

namespace Xbim.IDS.Validator.Core.Tests.Binders
{
    public class CombinedFacetBinderTests: BaseBinderTests
    {

        public CombinedFacetBinderTests(ITestOutputHelper output) : base(output)
        {
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
  
            var ifcbinder = new IfcTypeFacetBinder(IfcTypeLogger);
            ifcbinder.Initialise(BinderContext);

            var attrbinder = new AttributeFacetBinder(GetLogger<AttributeFacetBinder>(), GetValueMapper());
            attrbinder.Initialise(BinderContext);

            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = attrbinder.BindWhereExpression(expression, attrFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }

        [Fact]
        public void Can_Query_By_Ifc_Enum_And_Attribute()
        {
            // Find all doors and windows where the Tag is 1-30 characters long
            // we're really testing that the concat of the two types casts to something other than IfcRoot
            int expectedCount = 7;
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(),
            };
            
            ifcFacet.IfcType.AddAccepted(new ExactConstraint("IFCDOOR"));
            ifcFacet.IfcType.AddAccepted(new ExactConstraint("IFCWINDOW"));
            AttributeFacet attrFacet = new AttributeFacet
            {
                AttributeName = "Tag",
                AttributeValue = new ValueConstraint()
            };
            attrFacet.AttributeValue.AddAccepted(new StructureConstraint() { MinLength = 1, MaxLength = 10 });

   
            var ifcbinder = new IfcTypeFacetBinder(IfcTypeLogger);
            ifcbinder.Initialise(BinderContext);

            var attrbinder = new AttributeFacetBinder(GetLogger<AttributeFacetBinder>(), GetValueMapper());
            attrbinder.Initialise(BinderContext);

            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = attrbinder.BindWhereExpression(expression, attrFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }

        [InlineData("IfcFurniture", "Uniclass", "Pr_40_50_12", 4)]
        [InlineData("IfcFurnitureType", "Uniclass", "Pr_40_50_12", 3)]
        [InlineData("IfcProject", "Uniclass", null, 1)]
        [InlineData("IfcMaterial", "Uniclass", null, 0)]
        [InlineData("IfcWindowType", "Uniclass", null, 1)]
        [InlineData("IfcWindow", "Uniclass", null, 4)]  // Via WindowType
        [InlineData("IfcWindow", "Uniclass", "EF_25_30_97", 4)]
        [InlineData("IfcWindow", "Uniclass", "EF_25_30", 4)]
        [InlineData("IfcProperty", "Uniclass", null, 0)]
        [Theory]
        public void Can_Query_By_Ifc_And_Classifications(string ifcType, string system, string ident, int expectedCount)
        {

            
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };

            IfcClassificationFacet classFacet = new IfcClassificationFacet
            {
                ClassificationSystem = system,
            };
            if(!string.IsNullOrEmpty(ident))
            {
                classFacet.Identification = new ValueConstraint(ident);
            }
            var ifcbinder = new IfcTypeFacetBinder(IfcTypeLogger);
            ifcbinder.Initialise(BinderContext);
            var classbinder = new IfcClassificationFacetBinder(GetLogger<IfcClassificationFacetBinder>());
            classbinder.Initialise(BinderContext);
            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = classbinder.BindWhereExpression(expression, classFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }


       

        
        //TODO: Tests for Docs etc



        [InlineData("IfcWall", "Brick, Common", 3)]
        [InlineData("IfcSlab", "Vapor Retarder", 1)]
        [InlineData("IfcWall", "Vapor Retarder", 0)]
        [InlineData("IfcActor", "Vapor Retarder", 0)]
        [Theory]
        public void Can_Query_By_Ifc_And_Materials(string ifcType, string material, int expectedCount)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };

            MaterialFacet materialFacet = new MaterialFacet
            {
                Value = new ValueConstraint(),
            };
            materialFacet.Value = material;
            var ifcbinder = new IfcTypeFacetBinder(IfcTypeLogger);
            ifcbinder.Initialise(BinderContext);
            var materialbinder = new MaterialFacetBinder(GetLogger<MaterialFacetBinder>());
            materialbinder.Initialise(BinderContext);
            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = materialbinder.BindWhereExpression(expression, materialFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }


        [InlineData("IfcWindowType", "Dimensions", "Frame Depth", 65, 1)]
        [InlineData("IfcWindow", "BaseQuantities", "Width", 1810, 4)]
        // Needs cast fix in IDS for long-int 
        //[InlineData("IfcWall", "Analytical Properties", "Roughness", 3, 3)]
        [InlineData("IfcWall", "Construction", "Function", "Exterior", 3)]  // Via Type
        [InlineData("IfcWallType", "Construction", "Function", "Exterior", 1)]
        [Theory]
        public void Can_Query_By_Ifc_And_Properties(string ifcType, string psetName, string propName, object value,  int expectedCount)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };

            IfcPropertyFacet propertyFacet = new IfcPropertyFacet
            {
                PropertySetName = new ValueConstraint(),
                PropertyName = new ValueConstraint(),
                PropertyValue = new ValueConstraint(),
            };
            propertyFacet.PropertySetName.AddAccepted( new ExactConstraint(psetName));
            propertyFacet.PropertyName.AddAccepted(new ExactConstraint(propName));
            if(value != null)
                propertyFacet.PropertyValue.AddAccepted(new ExactConstraint(value?.ToString()));
            var ifcbinder = new IfcTypeFacetBinder(IfcTypeLogger);
            ifcbinder.Initialise(BinderContext);

            var psetbinder = new PsetFacetBinder(GetLogger<PsetFacetBinder>());
            psetbinder.Initialise(BinderContext);

            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = psetbinder.BindWhereExpression(expression, propertyFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);
            
        }


        // To implement
        [Fact]
        public void MaterialPropertiesNotSupported()
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint("IFCMATERIAL"),
            };

            IfcPropertyFacet propertyFacet = new IfcPropertyFacet
            {
                PropertySetName = new ValueConstraint("Pset_MaterialCombustion"),
                PropertyName = new ValueConstraint("SpecificHeatCapacity"),
                DataType = "IFCLABEL"
            };

            var ifcbinder = new IfcTypeFacetBinder(IfcTypeLogger);
            ifcbinder.Initialise(BinderContext);
            var psetbinder = new PsetFacetBinder(GetLogger<PsetFacetBinder>());
            psetbinder.Initialise(BinderContext);

            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);

            var ex = Record.Exception(() => expression = psetbinder.BindWhereExpression(expression, propertyFacet));

            ex.Should().NotBeNull();
            ex.Should().BeOfType<NotImplementedException>();
        }


        [InlineData("IfcBuildingElement", PartOfRelation.IfcRelAggregates, "IfcCurtainWall", 2)]
        [InlineData("IfcSpatialStructureElement", PartOfRelation.IfcRelContainedInSpatialStructure, "IfcBuilding", 1)]
        // TODO: Additional Relations
        [Theory]
        public void Can_Query_By_Ifc_And_PartOf(string ifcType, PartOfRelation partOfRelation, string entityType, int expectedCount)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint(ifcType),
            };

            PartOfFacet partOfFacet = new PartOfFacet
            {
                EntityType = new IfcTypeFacet() { IfcType = entityType }
            };
            partOfFacet.SetRelation(partOfRelation);
            
            
            var ifcbinder = new IfcTypeFacetBinder(IfcTypeLogger);
            ifcbinder.Initialise(BinderContext);

            var partOfbinder = new PartOfFacetBinder(GetLogger<PartOfFacetBinder>());
            partOfbinder.Initialise(BinderContext);
            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = partOfbinder.BindWhereExpression(expression, partOfFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }

        ILogger<IfcTypeFacetBinder> IfcTypeLogger { get => GetLogger<IfcTypeFacetBinder>(); }
    }

}
