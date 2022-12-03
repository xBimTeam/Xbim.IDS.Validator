using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class IdsModelBinderTests
    {
        static IModel model;
        private readonly ITestOutputHelper output;

        public IdsModelBinderTests(ITestOutputHelper output)
        {
            this.output = output;
        }
        static IdsModelBinderTests()
        {
            model = BuildModel();
        }


        [Fact]
        public void Can_Bind_Specification_to_model()
        {
            var modelBinder = new IdsModelBinder(model);
            var logger = GetXunitLogger();

            var idsSpec = Xbim.InformationSpecifications.Xids.LoadBuildingSmartIDS(@"TestModels\Example.ids", logger);
            

            foreach(var group in idsSpec.SpecificationsGroups)
            {
                logger.LogInformation("opening '{group}'", group.Name);
                foreach(var spec in group.Specifications)
                {
                    logger.LogInformation(" -- Spec {spec} : versions {ifcVersions}", spec.Name, spec.IfcVersion);
                    var applicableIfc = spec.Applicability.Facets.OfType<IfcTypeFacet>().FirstOrDefault();
                    logger.LogInformation("    Applicable to : {entity} with Type {predefined}", applicableIfc.IfcType.SingleValue(), applicableIfc.PredefinedType?.SingleValue());
                    foreach(var applicableFacet in spec.Applicability.Facets)
                    {
                        logger.LogInformation("       - {facetType}: check {description} ", applicableFacet.GetType().Name, applicableFacet.Short() );
                    }

                    IEnumerable<IPersistEntity> items = modelBinder.SelectApplicableEntities(spec);
                    foreach(var item in items)
                    {
                        var i = item as IIfcRoot;
                        logger.LogInformation("        * {ID}: {Type} {Name} ", item.EntityLabel, item.GetType().Name, i?.Name);
                        //logger.LogInformation("        Requirement: {name} {descr}", spec?.Requirement?.Name, spec.Requirement.Description);
                        foreach (var facet in spec.Requirement.Facets)
                        {
                            var result = modelBinder.ValidateRequirement(item, spec.Requirement, facet, logger);
                            LogLevel level = LogLevel.Information;
                            if (result.ValidationStatus == ValidationStatus.Inconclusive) level = LogLevel.Warning;
                            if (result.ValidationStatus == ValidationStatus.Failed) level = LogLevel.Error;
                            logger.Log(level, "          {result}: Checking {short} : {req}", result.ValidationStatus, facet.Short(), facet.ToString());
                        }
                    }

                   

                }
            }
        }

        

        private static IModel BuildModel()
        {
            var filename = @"TestModels\SampleHouse4.ifc";
            return IfcStore.Open(filename);
        }

        internal ILogger<IdsModelBinderTests> GetXunitLogger()
        {
            var services = new ServiceCollection()
                        .AddLogging((builder) => builder.AddXunit(output, 
                        new Divergic.Logging.Xunit.LoggingConfig { LogLevel=LogLevel.Debug }));
            IServiceProvider provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<IdsModelBinderTests>>();
            Assert.NotNull(logger);
            return logger;
        }
    }
}
