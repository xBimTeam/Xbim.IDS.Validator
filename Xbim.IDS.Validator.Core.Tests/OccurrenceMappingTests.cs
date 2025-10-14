using FluentAssertions;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc;
using Xbim.Ifc.Fluent;
using Xbim.Ifc4.Interfaces;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class OccurrenceMappingTests
    {

        [InlineData("IfcAirTerminal", "IfcFlowTerminal", "IfcAirTerminalType")]
        [InlineData("IfcFan", "IfcFlowMovingDevice", "IfcFanType")]
        [InlineData("IfcSpaceHeater", "IfcEnergyConversionDevice", "IfcSpaceHeaterType")]
        [InlineData("IfcVibrationIsolator", "IfcEquipmentElement", "IfcVibrationIsolatorType")]
        [Theory]
        public void IsMappedAsExpected(string ifc4Occurrence, string ifc2x3equivalent, string ifc2x3TypeDiscriminator)
        {
            var lookup = SchemaTypeMap.Ifc2x3TypeMap.ToDictionary(k => k.Key, k=> k.Value);

            var map = lookup[ifc4Occurrence.ToUpperInvariant()];

            map.ElementType.Name.Should().Be(ifc2x3equivalent);
            map.DefiningType.Name.Should().Be(ifc2x3TypeDiscriminator);

        }

        [Fact]
        public void CanConstructAllOccurrences()
        {
            var builder = new FluentModelBuilder();
            XbimEditorCredentials editor = GetEditor();
            foreach (var pair in SchemaTypeMap.Ifc2x3TypeMap)
            {
                builder
                    .AssignEditor(editor)
                    .CreateModel(Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3)
                    .SetOwnerHistory()
                    .CreateEntities((f, b) =>
                    {
                        var type = (IIfcTypeObject)CreateEntity(pair.Value.DefiningType.Name, b);
                        var entity = (IIfcProduct)CreateEntity(pair.Value.ElementType.Name, b);
                        type.Should().NotBeNull();
                        entity.Should().NotBeNull();

                        type.Name = pair.Value.DefiningType.Name;   // To address TypeObject.WR1

                        entity.AddDefiningType(type);
                    }).AssertValid();

            }

            
        }

        private static XbimEditorCredentials GetEditor()
        {
            var editor = new XbimEditorCredentials
            {
                EditorsGivenName = "Andy",
                EditorsFamilyName = "Ward",
                EditorsIdentifier = "andy.ward@xbim.net",
                EditorsOrganisationName = "Xbim",
                EditorsOrganisationIdentifier = "net.xbim",

                ApplicationFullName = "Unit Tests",
                ApplicationVersion = "1.0.0-alpha",
                ApplicationDevelopersName = "Xbim",  // When same as EditorOrg => reuses the org
                ApplicationIdentifier = "ids-unittests"
            };
            return editor;
        }

        private static IIfcRoot CreateEntity(string expressTypeName, IModelInstanceBuilder ctx)
        {
            var expressType = ctx.Model.Metadata.ExpressType(expressTypeName.ToUpperInvariant());
            if (expressType is null) throw new NotSupportedException($"{expressTypeName} Type not supported in {ctx.Model.SchemaVersion}");

            var entity = (IIfcRoot)ctx.Instances.New(expressType.Type);
            return entity;
        }
    }
}
