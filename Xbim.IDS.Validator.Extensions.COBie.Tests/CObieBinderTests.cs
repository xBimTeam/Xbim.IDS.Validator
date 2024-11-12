using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.CobieExpress;
using Xbim.CobieExpress.Exchanger;
using Xbim.CobieExpress.Exchanger.FilterHelper;
using Xbim.Common;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.IDS.Validator.Extensions.COBie.Binders;
using Xbim.IDS.Validator.Tests.Common;
using Xbim.InformationSpecifications;
using Xbim.IO.CobieExpress;
using Xbim.IO.Memory;
using Xbim.IO.Table;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Extensions.COBie.Tests
{
    public class COBieBinderTests : COBieBaseModelTester
    {
        private static ITestOutputHelper staticOutput;
        private readonly IServiceProvider provider;
        private IIdsFacetBinderFactory facetBinderFactory;

        public COBieBinderTests(ITestOutputHelper testOutput) : base(testOutput) 
        {
            staticOutput = testOutput;
            provider = COBieTestEnvironment.ServiceProvider;
            facetBinderFactory = provider.GetRequiredService<IIdsFacetBinderFactory>();
        }

        ILogger<IfcTypeFacetBinder> IfcTypeLogger { get => GetLogger<IfcTypeFacetBinder>(); }


        const string file = @"TestModels/SampleHouse4.xlsx";

        [Fact]
        public void CanOpenCobie()
        {


            var mapping = CobieModel.GetMapping();
            mapping.ClassMappings.RemoveAll(m => m.Class == "System");

            using (IModel model = CobieModel.ImportFromTable(file, out string report, mapping))
            {

                output.WriteLine(report);

                var summary = model.Instances.OfType<CobieReferencedObject>().GroupBy(i => i.GetType().Name);

                foreach (var item in summary)
                {
                    output.WriteLine("{0}: {1}", item.Key, item.Count());
                }
                //// Get all the contacts
                var contacts = model.Instances.OfType<CobieContact>().ToList();

                foreach (var contact in contacts)
                {
                    output.WriteLine("Contact #{4} {0}: {1} = {2} - {3}: {5}", contact.Email, contact.Company, contact.ExternalId, contact.Category?.Value, contact.EntityLabel, contact.Created?.CreatedOn.Value);
                }

                var components = model.Instances.OfType<CobieComponent>().ToList();

                foreach (var comp in components)
                {
                    output.WriteLine("Component #{5} {0}: {1} = {2}  ({3}) in [{4}]", comp.Name, comp.Type?.Name, comp.ExternalId, comp.ExternalObject?.Name, comp.Spaces?.FirstOrDefault()?.Name, comp.EntityLabel);
                }

            }
        }

        [Fact]
        public void CanQueryContacts()
        {
            var binder = new IfcTypeFacetBinder(GetLogger<IfcTypeFacetBinder>()) { };
            binder.Initialise(BinderContext);
            var types = new[] { typeof(CobieContact) };
            AssertIfcTypeFacetQuery(binder, "COBIECONTACT", 3, types);
        }


        [InlineData("CobieContact", nameof(CobieContact.Company), "xbim", 1)]
        [InlineData("CobieContact", nameof(CobieContact.OrganizationCode), "XB", 1)]
        [InlineData("CobieContact", nameof(CobieContact.Department), "", 0)]
        [InlineData("CobieContact", nameof(CobieContact.Category), "Ro_30_40_79 : Software vendor", 1)]
        [InlineData("CobieContact", nameof(CobieContact.CreatedBy), "unknown@OpenBIM.org", 3)]
        [InlineData("CobieContact", nameof(CobieContact.CreatedOn), "2024-02-19T13:47:54", 3)]
        [InlineData("CobieFloor", nameof(CobieFloor.Categories), "Floor", 2)]
        [InlineData("CobieFloor", nameof(CobieFloor.ExternalSystem), "Autodesk Revit 2015 (ENU)", 2)]
        [InlineData("CobieFloor", nameof(CobieFloor.Elevation), "0", 1)]
        [InlineData("CobieFloor", nameof(CobieFloor.CreatedBy), "info@xbim.net", 2)]
        [InlineData("CobieType", nameof(CobieType.WarrantyDurationParts), "5", 1)]
        [InlineData("CobieType", nameof(CobieType.WarrantyDurationUnit), "Years", 1)]
        [InlineData("CobieType", nameof(CobieType.ExternalId), "3cUkl32yn9qRSPvA7VyWdr", 1)]
        [InlineData("CobieType", nameof(CobieType.ExternalObject), "IfcFurnitureType", 8)]
        [InlineData("CobieType", nameof(CobieType.Manufacturer), "info@manufacturer.unknown", 4)]
        [InlineData("CobieComponent", nameof(CobieComponent.Type), "FurnitureType.3 Chair - Dining\r\n", 6)]
        [InlineData("CobieComponent", nameof(CobieComponent.Spaces), "1 - Living room", 12)]
        [Theory]
        public void Can_Query_By_EntityType_And_Attributes(string ifcType, string attributeFieldName, string attributeValue, int expectedCount)
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
            var attrbinder = new COBieColumnFacetBinder(GetLogger<COBieColumnFacetBinder>(), GetValueMapper());
            attrbinder.Initialise(BinderContext);
            attrbinder.SetOptions(new VerificationOptions { AllowDerivedAttributes = true, IncludeSubtypes = true }) ;
            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = attrbinder.BindWhereExpression(expression, attrFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(expectedCount);

        }

        [InlineData(typeof(MaterialFacet))]
        [InlineData(typeof(IfcClassificationFacet))]
        [InlineData(typeof(PartOfFacet))]
        //[InlineData(typeof(AttributeFacet))]
        //[InlineData(typeof(IfcPropertyFacet))]
        [Theory]
        public void Cobie_Cannot_Query_By_Unsupported_Facets(Type facetType)
        {
            IfcTypeFacet ifcFacet = new IfcTypeFacet
            {
                IfcType = new ValueConstraint("CobieComponent"),
            };

            var targetFacet = (IFacet)Activator.CreateInstance(facetType);
            PopulateDummyData(targetFacet);

            var ifcbinder = facetBinderFactory.Create(ifcFacet, BinderContext, Model.SchemaVersion);
            var targetBinder = facetBinderFactory.Create(targetFacet, BinderContext, Model.SchemaVersion);

            // Act
            var expression = ifcbinder.BindSelectionExpression(query.InstancesExpression, ifcFacet);
            expression = targetBinder.BindWhereExpression(expression, targetFacet);

            // Assert

            var result = query.Execute(expression, Model);
            result.Should().HaveCount(0);

        }

        [Fact(Skip="Used to generate SampleHouse4.xlsx")]
        public void CreatesCOBie()
        {
            const string input = @"TestModels/SampleHouse4.ifc";

            var ifc = MemoryModel.OpenReadStep21(input);

            var cobie = new CobieModel();
            using (var txn = cobie.BeginTransaction("Sample house conversion"))
            {
                OutputFilters filters = GetFilters();

                var exchanger = new IfcToCoBieExpressExchanger(ifc, cobie,
                    extId: EntityIdentifierMode.GloballyUniqueIds,
                    filter: filters

                    /*,     // More advanced configuration options available
                    reportProgress: reportProgressDelegate,
                    filter: outputFilters,
                    configFile: pathToAttributeMappingConfigFile,
                    sysMode: SystemExtractionMode.System,
                    classify: true*/
                    );
                exchanger.Convert();
                txn.Commit();
            }


            // Finally export as a COBie spreadsheet
            var output = Path.ChangeExtension(input, ".xlsx");
            ModelMapping mapping = GetExportMapping();
            cobie.ExportToTable(output, out string report, mapping);

        }

        private static ModelMapping GetExportMapping()
        {
            var mapping = CobieModel.GetMapping();

            foreach (var item in mapping.ClassMappings)
            {
                foreach (var field in item.PropertyMappings)
                {
                    if (field.Hidden)
                    {
                        field.Hidden = false;
                    }
                }
            }

            return mapping;
        }

        private OutputFilters GetFilters()
        {
            var filters = new CobieExpress.Exchanger.FilterHelper.OutputFilters(logger, CobieExpress.Exchanger.FilterHelper.RoleFilter.Architectural);
            filters.IfcProductFilter.Items[nameof(Ifc4.SharedBldgElements.IfcCovering).ToUpper()] = false;
            filters.IfcProductFilter.Items[nameof(Ifc4.SharedBldgElements.IfcRoof).ToUpper()] = false;
            return filters;
        }

        private static void PopulateDummyData(IFacet targetFacet)
        {
            switch (targetFacet)
            {
                case AttributeFacet facet:
                    facet.AttributeName = "Name";
                    break;

                case IfcPropertyFacet facet:
                    facet.PropertySetName = "Pset_DoorCommon";
                    facet.PropertyName = "FR";
                    break;

                case IfcTypeFacet facet:
                    facet.IfcType = "CobieContact";
                    break;

                case MaterialFacet facet:
                    facet.Value = "Wood";
                    break;

                case IfcClassificationFacet facet:
                    facet.Identification = "EN_10";
                    break;

                case PartOfFacet facet:
                    facet.EntityRelation = "IfcRelAggregates";
                    break;

                default:
                    throw new NotImplementedException();
            };
        }


        public override IModel Model
        {
            get
            {
                return lazyCobieModel.Value;
            }
        }

        private static Lazy<IModel> lazyCobieModel = new Lazy<IModel>(() => BuildCOBieModel(staticOutput));

        private static IModel BuildCOBieModel(ITestOutputHelper outputHelper = null)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);
            var mapping = CobieModel.GetMapping();
            mapping.ClassMappings.RemoveAll(m => m.Class == "System");
            var model = CobieModel.ImportFromTable(file, out string report, mapping);
            outputHelper?.WriteLine(report);
            return model;
        }
    }
}
