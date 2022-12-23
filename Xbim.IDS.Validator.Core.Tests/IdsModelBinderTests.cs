﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class IdsModelBinderTests
    {
        
        private readonly ITestOutputHelper output;

        public IdsModelBinderTests(ITestOutputHelper output)
        {
            this.output = output;
        }


        [InlineData(@"TestModels\Example.ids", @"TestModels\SampleHouse4.ifc")]
        //[InlineData(@"TestModels\Example.ids", @"\\Mac\Home\Downloads\villa_tugenhat.ifc\villa_tugenhat_v1.ifc")]
        //InlineData(@"TestModels\BasicRequirements.ids", @"\\Mac\Home\Downloads\villa_tugenhat.ifc\villa_tugenhat_v1.ifc")]
        [InlineData(@"TestModels\BasicRequirements.ids", @"TestModels\SampleHouse4.ifc")]
        [Theory]

        public void Can_Bind_Specification_to_model(string idcFile, string ifcFile)
        {
            var model = BuildModel(ifcFile);
            var modelBinder = new IdsModelBinder(model);
            var logger = GetXunitLogger();

            var idsSpec = Xbim.InformationSpecifications.Xids.LoadBuildingSmartIDS(idcFile, logger);
            

            foreach(var group in idsSpec.SpecificationsGroups)
            {
                logger.LogInformation("opening '{group}'", group.Name);
                foreach(var spec in group.Specifications)
                {
                    logger.LogInformation(" -- Spec '{spec}' : versions {ifcVersions}", spec.Name, spec.IfcVersion);
                    var applicableIfc = spec.Applicability.Facets.OfType<IfcTypeFacet>().FirstOrDefault();
                    logger.LogInformation("    Applicable to : {entity} with PredefinedType {predefined}", applicableIfc.IfcType.SingleValue(), applicableIfc.PredefinedType?.SingleValue());
                    foreach(var applicableFacet in spec.Applicability.Facets)
                    {
                        logger.LogInformation("       - {facetType}: where {description} ", applicableFacet.GetType().Name, applicableFacet.Short() );
                    }

                    logger.LogInformation("    Requirements {reqCount}: {expectation}", spec.Requirement.Facets.Count, spec.Requirement.RequirementOptions?.FirstOrDefault().ToString() ?? "" );
                    int idx = 1;
                    foreach (var reqFacet in spec.Requirement.Facets)
                    {
                        logger.LogInformation("       [{i}] {facetType}: check {description} ", idx++, reqFacet.GetType().Name, reqFacet.Short());
                    }
                    IEnumerable <IPersistEntity> items = modelBinder.SelectApplicableEntities(spec);
                    logger.LogInformation("          Checking {count} applicable items", items.Count());
                    foreach (var item in items)
                    {
                        var i = item as IIfcRoot;
                        logger.LogInformation("        * {ID}: {Type} {Name} ", item.EntityLabel, item.GetType().Name, i?.Name);
                        
                        var result = modelBinder.ValidateRequirement(item, spec.Requirement, logger);
                        LogLevel level = LogLevel.Information;
                        int pad = 0;
                        if (result.ValidationStatus == ValidationStatus.Inconclusive) { level = LogLevel.Warning; pad = 4; }
                        if (result.ValidationStatus == ValidationStatus.Failed) { level = LogLevel.Error; pad = 6; }
                        logger.Log(level, "{pad}          {result}: Checking {short}", "".PadLeft(pad, ' '),  result.ValidationStatus.ToString().ToUpperInvariant(), spec.Requirement.Short());
                        foreach(var message in result.Messages)
                        {
                            logger.Log(level, "{pad}              #{entity} {message}", "".PadLeft(pad, ' '), item.EntityLabel, message.ToString());
                        }
                        
                    }

                   

                }
            }
        }

        

        private static IModel BuildModel(string ifcFile)
        {
            return IfcStore.Open(ifcFile);
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
