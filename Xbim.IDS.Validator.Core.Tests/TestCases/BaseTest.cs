using Divergic.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Ifc;
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

        protected List<IdsValidationResult> VerifyIdsFile(string idsFile)
        {
            string ifcFile = Path.ChangeExtension(idsFile, "ifc");
            IfcStore model = IfcStore.Open(ifcFile);
            Xids ids = Xids.LoadBuildingSmartIDS(idsFile, logger);
            IdsModelBinder modelBinder = new IdsModelBinder(model);
            List<IdsValidationResult> results = new List<IdsValidationResult>();
            foreach (Specification spec in ids.AllSpecifications())
            {
                logger.LogInformation("{specName}", spec.Name);
                IEnumerable<IPersistEntity> applicable = modelBinder.SelectApplicableEntities(spec);
                foreach (IFacet facet in spec.Requirement!.Facets)
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
