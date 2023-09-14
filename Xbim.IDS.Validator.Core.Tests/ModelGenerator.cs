using FluentAssertions;
using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO.Memory;

namespace Xbim.IDS.Validator.Core.Tests
{
    [Collection(nameof(TestEnvironment))]
    public class ModelGenerator
    {
        XbimEditorCredentials editor = new XbimEditorCredentials
        {
            ApplicationFullName = "xbim IDS",
            ApplicationIdentifier = "xbim IDS Tests",
            EditorsOrganisationName = "xbim Ltd",
            EditorsGivenName = "Andy",
            EditorsFamilyName = "Ward",
            ApplicationVersion = "1.0",
            ApplicationDevelopersName = "xbim"
        };

        [Fact]
        public void CanCreateMinimalModel()
        {
            string filename = "file.ifc";

            //new MemoryModel(new Ifc2x3.EntityFactoryIfc2x3());
            var model = IfcStore.Create(editor, XbimSchemaVersion.Ifc4x3, IO.XbimStoreType.InMemoryModel);
            AddHeaders(model, filename);
            using (var trans = model.BeginTransaction("Create"))
            {
                model.Instances.New<Ifc4x3.ProductExtension.IfcSpaceType>(w =>
                {
                    w.Name = "Test";
                    w.LongName = "Long Name";
                    w.PredefinedType = Ifc4x3.ProductExtension.IfcSpaceTypeEnum.SPACE;
                });


                var val = new Xbim.Common.ExpressValidation.Validator();
                val.ValidateLevel = ValidationFlags.All;

                var result = val.Validate(model).ToList();
                result.Should().BeEmpty();

                trans.Commit();
            }

            using (var sw = new FileStream(filename, FileMode.Create))
            {
                model.SaveAsIfc(sw);
            }
            model.Dispose();
        }

        [Fact]
        public void CanCreateIfc2x3Model()
        {
            string filename = "ifc2x3.ifc";

            using var model = new MemoryModel(new Ifc2x3.EntityFactoryIfc2x3());
            using var trans = model.BeginTransaction("Create");
            using var sw = new FileStream(filename, FileMode.Create);

            AddHeaders(model, filename);
            model.Instances.New<Ifc2x3.SharedBldgElements.IfcDoorStyle>(w =>
            {
                w.Name = "Test";
                w.ConstructionType = Ifc2x3.SharedBldgElements.IfcDoorStyleConstructionEnum.NOTDEFINED;
            });

            trans.Commit();
            model.SaveAsIfc(sw);
        }

        [Fact]
        public void CanCreateIfc2x3AirTerminalModel()
        {
            string filename = "ifc2x3-air-terminal.ifc";

            using var model = new MemoryModel(new Ifc2x3.EntityFactoryIfc2x3());
            using var trans = model.BeginTransaction("Create");
            using var sw = new FileStream(filename, FileMode.Create);

            AddHeaders(model, filename);
            var type = model.Instances.New<Ifc2x3.HVACDomain.IfcAirTerminalType>(w =>
            {
                w.Name = "AirTerminalType";
                w.PredefinedType = Ifc2x3.HVACDomain.IfcAirTerminalTypeEnum.GRILLE;
            });
            var instance = model.Instances.New<Ifc2x3.SharedBldgServiceElements.IfcFlowTerminal>(w =>
            {
                w.Name = "AirTerminal";
            });

            //var instance4 = model.Instances.New<Ifc4.HvacDomain.IfcAirTerminal>(w =>
            //{
            //    w.Name = "AirTerminal";
            //    w.PredefinedType = Ifc4.Interfaces.IfcAirTerminalTypeEnum.GRILLE;
            //});
            instance.AddDefiningType(type);

            trans.Commit();
            model.SaveAsIfc(sw);
        }

        private static void AddHeaders(IModel model, string filename)
        {
            model.Header.FileDescription.Description.Add("ViewDefinition [CoordinationView]");

            model.Header.FileName.Name = filename;
            model.Header.FileName.PreprocessorVersion = "xbim Toolkit";
            model.Header.FileName.OriginatingSystem = "xbim IDS Unit tests";
            model.Header.FileName.Organization.Add("xbim Ltd");
            model.Header.FileName.AuthorName.Add("Andy Ward");
            model.Header.FileName.AuthorizationName = ("n/a");
        }
    }
}
