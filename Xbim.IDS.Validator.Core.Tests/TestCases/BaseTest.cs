using Divergic.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public abstract class BaseTest
    {
        protected readonly ITestOutputHelper output;

        protected readonly ILogger logger;

        public BaseTest(ITestOutputHelper output)
        {
            this.output = output;
            logger = GetXunitLogger();
        }

        internal ILogger GetXunitLogger()
        {
            IServiceCollection services = new ServiceCollection().AddLogging(delegate (ILoggingBuilder builder)
            {
                builder.AddXunit(output, new LoggingConfig
                {
                    LogLevel = LogLevel.Debug
                });
            });
            IServiceProvider provider = services.BuildServiceProvider();
            ILogger<BaseTest> logger = provider.GetRequiredService<ILogger<BaseTest>>();
            Assert.NotNull(logger);
            return logger;
        }

        protected List<IdsValidationResult> VerifyIdsFile(string idsFile, bool spotfix = false)
        {
            string ifcFile = Path.ChangeExtension(idsFile, "ifc");
            IfcStore model = IfcStore.Open(ifcFile);
            if (spotfix)
            {
                // These models appear invalid for the test case by containing an additional wall. rather than fix the IFC, we spotfix
                // So we can continue to add the standard test files without manually fixing in future.
                // e.g. classification/pass-occurrences_override_the_type_classification_per_system_1_3.ifc
                var rogue = model.Instances[4];
                if (rogue is IIfcWall w && w.GlobalId == "3Agm079vPIYBL4JExVrhD5")
                {
                    using(var tran = model.BeginTransaction("Patch"))
                    {
                        model.Delete(rogue);
                        tran.Commit();
                    }
                }
                else
                {
                    // Maybe the test files got fixed?
                    logger.LogWarning("Spotfix failed. Check if this code can be removed");
                }
            }
            Xids ids = Xids.LoadBuildingSmartIDS(idsFile, logger);
            IdsModelBinder modelBinder = new IdsModelBinder(model);
            List<IdsValidationResult> results = new List<IdsValidationResult>();
            foreach (Specification spec in ids.AllSpecifications())
            {
                logger.LogInformation("{specName}", spec.Name);
                IEnumerable<IPersistEntity> applicable = modelBinder.SelectApplicableEntities(spec);
                if(spec.Requirement?.Facets == null)
                {
                    logger.LogWarning("Failed to find Requirements {specName}", spec.Name);
                    continue;
                }
                foreach (IFacet facet in spec.Requirement?.Facets)
                {
                    foreach (IPersistEntity entity in applicable)
                    {
                        IdsValidationResult result = modelBinder.ValidateRequirement(entity, spec.Requirement, facet, logger);
                        results.Add(result);
                    }
                }
            }
            foreach (IdsValidationResult res in results)
            {
                LogLevel logLevel = LogLevel.Information;
                if (res.ValidationStatus == ValidationStatus.Failed)
                {
                    logLevel = LogLevel.Error;
                }
                if (res.ValidationStatus == ValidationStatus.Inconclusive)
                {
                    logLevel = LogLevel.Warning;
                }
                logger.Log(logLevel, "Entity {ent}", res.Entity?.EntityLabel);
                foreach (string pass in res.Successful)
                {
                    logger.LogInformation("  {message}", pass);
                }
                foreach (string fail in res.Failures)
                {
                    logger.LogError("  {error}", fail);
                }
            }
            return results;
        }
    }
}
