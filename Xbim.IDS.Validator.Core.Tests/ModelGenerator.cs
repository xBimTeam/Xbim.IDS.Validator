using FluentAssertions;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;
using Xbim.Ifc4x3.ProductExtension;
using Xbim.IO.Memory;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class ModelGenerator
    {
        [Fact]
        public void CanCreateMinimalModel()
        {
            string filename = "file.ifc";
            var editor = new XbimEditorCredentials
            {
                ApplicationFullName = "xbim IDS",
                ApplicationIdentifier = "xbim IDS Tests",
                EditorsOrganisationName = "xbim Ltd",
                EditorsGivenName = "Andy",
                EditorsFamilyName="Ward",
                ApplicationVersion= "1.0",  
                ApplicationDevelopersName="xbim"
            };
            //new MemoryModel(new Ifc2x3.EntityFactoryIfc2x3());
            var model = IfcStore.Create(editor, Common.Step21.XbimSchemaVersion.Ifc4x3, IO.XbimStoreType.InMemoryModel);
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
                val.ValidateLevel = Common.Enumerations.ValidationFlags.All;

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
