using Divergic.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.CobieExpress;
using Xunit.Abstractions;
using AuditStatus = IdsLib.Audit.Status;

namespace Xbim.IDS.Validator.Core.Tests.TestCases
{
    [Collection(nameof(TestEnvironment))]
    public abstract class BaseTest
    {
        protected readonly ITestOutputHelper output;

        protected readonly ILogger logger;

        private readonly IServiceProvider provider;

        public BaseTest(ITestOutputHelper output)
        {
            this.output = output;
            logger = GetXunitLogger();
            provider = TestEnvironment.ServiceProvider;
            TestEnvironment.InitialiseXunitLogger(output);
        }

        /// <summary>
        /// Gets testcase parameters to feed into xunit Theory test using <see cref="MemberDataAttribute"/> conventions, skipping unsupported tests
        /// </summary>
        /// <param name="facetFolder">The folder to find the tests in</param>
        /// <param name="testType">The prefix to locate the tests by. Must be 'pass' or 'fail'</param>
        /// <param name="versionExceptions">A <see cref="IDictionary{TKey, TValue}"/> mapping tests to their supported schemas</param>
        /// <returns><see cref="IEnumerable"/> of <see cref="object[]"/> with two parameters</returns>
        public static IEnumerable<object[]> GetApplicableTestCases(string facetFolder, string testType, IDictionary<string, XbimSchemaVersion[]> versionExceptions)
        {
            FileInfo[] testCases = GetTestCases(facetFolder, testType);

            foreach (var ids in testCases)
            {

                var relativeName = Path.GetRelativePath(Environment.CurrentDirectory, ids.FullName);
                var filename = Path.GetFileName(relativeName);

                versionExceptions.TryGetValue(filename, out var schemaExceptions);
                if (schemaExceptions?.Contains(XbimSchemaVersion.Unsupported) == true)
                {
                    // No schemas supported => Ignore this test - not supported yet
                    continue;
                }

                yield return new object[]
                {
                    relativeName,
                    schemaExceptions ?? Array.Empty<XbimSchemaVersion>()
                };
            }
        }

        /// <summary>
        /// Gets unsupported testcase parameters to feed into xunit Theory test using <see cref="MemberDataAttribute"/> conventions
        /// </summary>
        /// <param name="facetFolder"></param>
        /// <param name="testType"></param>
        /// <param name="versionExceptions"></param>
        /// <returns></returns>
        public static IEnumerable<object[]> GetUnsupportedTestsCases(string facetFolder, string testType, IDictionary<string, XbimSchemaVersion[]> versionExceptions)
        {
            FileInfo[] idss = GetTestCases(facetFolder, testType);

            foreach (var ids in idss)
            {

                var relativeName = Path.GetRelativePath(Environment.CurrentDirectory, ids.FullName);
                var filename = Path.GetFileName(relativeName);

                versionExceptions.TryGetValue(filename, out var schemaExceptions);
                if (schemaExceptions?.Contains(XbimSchemaVersion.Unsupported) == true)
                {
                    // Yield Ignored test
                    yield return new object[]
                    {
                        relativeName
                    };
                }
            }
        }

        private static FileInfo[] GetTestCases(string facetFolder, string testType)
        {
            string testRoot = @$"TestCases/{facetFolder}";
            DirectoryInfo d = new DirectoryInfo(testRoot);
            var idsFiles = d.GetFiles($"{testType}-*.ids", SearchOption.AllDirectories);
            return idsFiles;
        }


        internal ILogger GetXunitLogger()
        {
            return TestEnvironment.GetXunitLogger<BaseTest>(output);
        }

        protected XbimSchemaVersion[] GetSchemas(XbimSchemaVersion[] schemaVersions)
        {
            if (schemaVersions == null || schemaVersions.Length == 0)
            {
                return new[] { XbimSchemaVersion.Ifc4, XbimSchemaVersion.Ifc2X3 };  // We don't test 4.3 unless explicit
            }
            var except = new[] { XbimSchemaVersion.Ifc2X3 };
            return schemaVersions;//.Except(except).ToArray();
        }

        protected async Task <ValidationOutcome> VerifyIdsFile(string idsFile, bool spotfix = false, XbimSchemaVersion schemaVersion = XbimSchemaVersion.Ifc4,
            VerificationOptions options = default
            )
        {
            IModel model = null;
            try
            {
                if(false)
                {
                    DoInPlaceUpgrade(idsFile);
                }
                string modelFile = Path.ChangeExtension(idsFile, "ifc");
                logger.LogInformation("Verifying {schema} model {model}", schemaVersion, modelFile);
                switch (schemaVersion)
                {
                    case XbimSchemaVersion.Ifc4:
                        model = IfcStore.Open(modelFile);
                        break;
                    case XbimSchemaVersion.Ifc4x3:
                        model = IfcStore.Open(modelFile);
                        break;

                    case XbimSchemaVersion.Ifc2X3:
                        {

                            // Very crude attempt to fake an IFC2x3 from IFC4. Because of breaking changes between schemas
                            // this should be treated as suspect - but does allow quick & dirty testing of our schema switching logic
                            var stepText = File.ReadAllText(modelFile);
                            stepText = stepText.Replace("FILE_SCHEMA(('IFC4'));", "FILE_SCHEMA(('IFC2x3'));");
                            using (var stream = new MemoryStream())
                            using (var sw = new StreamWriter(stream))
                            {
                                sw.Write(stepText);
                                sw.Flush();
                                stream.Seek(0, SeekOrigin.Begin);

                                model = IfcStore.Open(stream, IO.StorageType.Ifc, schemaVersion, IO.XbimModelType.MemoryModel);
                            }

                            break;
                        }
                    case XbimSchemaVersion.Cobie2X4:
                        modelFile = @"..\..\..\TestModels\SampleHouse4.xlsx";
                        model = OpenCOBie(modelFile);
                        break;

                    default:
                        throw new NotSupportedException($"Schema not supported {schemaVersion}");
                }

                if (spotfix)
                {
                    // These models appear invalid for the test case by containing an additional wall. rather than fix the IFC, we spotfix
                    // So we can continue to add the standard test files without manually fixing in future.
                    // e.g. classification/pass-occurrences_override_the_type_classification_per_system_1_3.ifc 
                    // Logged as https://github.com/buildingSMART/IDS/issues/108
                    var rogue = model.Instances[4];
                    if (rogue is IIfcWall w && w.GlobalId == "3Agm079vPIYBL4JExVrhD5")
                    {
                        using (var tran = model.BeginTransaction("Patch"))
                        {
                            model.Delete(rogue);
                            tran.Commit();
                            logger.LogWarning("Spotfixing extraneous IfcWall 3Agm079vPIYBL4JExVrhD5");
                        }
                    }
                    else
                    {
                        // Maybe the test files got fixed?
                        logger.LogWarning("Spotfix failed. Check if this code can be removed");
                    }
                }

                var validator = provider.GetRequiredService<IIdsModelValidator>();
                
                var outcome = await validator.ValidateAgainstIdsAsync(model, idsFile, logger, null, options);

                return outcome;
            }
            finally
            {
                if(model != null)
                {
                    model.Dispose();
                }
            }
        }

        protected AuditStatus ValidateIds(string idsFile)
        {
            var fileValidator = provider.GetRequiredService<IIdsValidator>();
            var fileValidity = fileValidator.ValidateIDS(idsFile);
            if (!AllowedStatuses.Contains(fileValidity))
            {
                //outcome.MarkCompletelyFailed($"IDS Validation failure {fileValidity}");
                logger.LogWarning("IDS File invalid: {errorCode}", fileValidity);
            }
            return fileValidity;

        }

        private void DoInPlaceUpgrade(string idsFile)
        {
            var migrator = new IdsSchemaMigrator(TestEnvironment.GetXunitLogger<IdsSchemaMigrator>(output));
            if(migrator.HasMigrationsToApply(idsFile))
            {
                if(migrator.MigrateToIdsSchemaVersion(idsFile, out var upgraded))
                {
                    var masterFile = Path.Combine("../../..", idsFile);
                    upgraded.Save(masterFile);
                }
            }
        }

        private static AuditStatus[] AllowedStatuses = new AuditStatus[] { AuditStatus.Ok };

        private IModel OpenCOBie(string file)
        {
            if(!File.Exists(file))
                throw new FileNotFoundException(file);
            var mapping = CobieModel.GetMapping();
            //mapping.ClassMappings.RemoveAll(m => m.Class == "System");
            var model = CobieModel.ImportFromTable(file, out string report, mapping);
            
            return model;
        }
    }
}
