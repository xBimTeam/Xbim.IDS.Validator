using Divergic.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    public abstract class BaseTest
    {
        protected readonly ITestOutputHelper output;

        protected readonly ILogger logger;

        private readonly IServiceProvider provider;

        public BaseTest(ITestOutputHelper output)
        {
            this.output = output;
            logger = GetXunitLogger();
            provider = BuildServiceProvider();
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            serviceCollection.AddIdsValidation();

            var provider = serviceCollection.BuildServiceProvider();
            return provider;
        }

        static BaseTest()
        {
            IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
        }

        internal ILogger GetXunitLogger()
        {
            IServiceCollection services = new ServiceCollection().AddLogging(delegate (ILoggingBuilder builder)
            {
                builder.AddXunit(output, new LoggingConfig
                {
                    LogLevel = LogLevel.Debug,
                    IgnoreTestBoundaryException = true
                });
            });
            IServiceProvider provider = services.BuildServiceProvider();
            ILogger<BaseTest> logger = provider.GetRequiredService<ILogger<BaseTest>>();
            Assert.NotNull(logger);
            ILoggerFactory logFactory = provider.GetRequiredService<ILoggerFactory>();
            Xbim.Common.XbimLogging.LoggerFactory = logFactory;
            return logger;
        }

        protected XbimSchemaVersion[] GetSchemas(XbimSchemaVersion[] schemaVersions)
        {
            if (schemaVersions.Length == 0)
            {
                return new[] { XbimSchemaVersion.Ifc4, XbimSchemaVersion.Ifc2X3 };
            }
            return schemaVersions;
        }

        protected ValidationOutcome VerifyIdsFile(string idsFile, bool spotfix = false, XbimSchemaVersion schemaVersion = XbimSchemaVersion.Ifc4)
        {
            string ifcFile = Path.ChangeExtension(idsFile, "ifc");
            IfcStore model;
            if (schemaVersion == XbimSchemaVersion.Ifc4)
            {
                model = IfcStore.Open(ifcFile);
            }
            else if (schemaVersion == XbimSchemaVersion.Ifc4x3)
            {
                model = IfcStore.Open(ifcFile);
            }
            else
            {

                // Very crude attempt for fake an IFC2x3 from IFC4. Because of breaking changes between schemas
                // this should be treated as suspect - but does allow quick & dirty testing of our schema switching logic
                var stepText = File.ReadAllText(ifcFile);
                stepText = stepText.Replace("FILE_SCHEMA(('IFC4'));", "FILE_SCHEMA(('IFC2x3'));");
                using (var stream = new MemoryStream())
                using(var sw = new StreamWriter(stream)) 
                {
                    sw.Write(stepText);
                    sw.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    model = IfcStore.Open(stream, IO.StorageType.Ifc, schemaVersion, IO.XbimModelType.MemoryModel);
                }
            }
            
            if (spotfix)
            {
                // These models appear invalid for the test case by containing an additional wall. rather than fix the IFC, we spotfix
                // So we can continue to add the standard test files without manually fixing in future.
                // e.g. classification/pass-occurrences_override_the_type_classification_per_system_1_3.ifc
                var rogue = model.Instances[4];
                if (rogue is IIfcWall w && w.GlobalId == "3Agm079vPIYBL4JExVrhD5")
                {
                    using (var tran = model.BeginTransaction("Patch"))
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
            
            var validator = provider.GetRequiredService<IIdsModelValidator>();
            var outcome = validator.ValidateAgainstIds(model, idsFile, logger);

            var results = outcome.ExecutedRequirements.SelectMany(e => e.ApplicableResults);
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
            return outcome;
        }
    }
}
