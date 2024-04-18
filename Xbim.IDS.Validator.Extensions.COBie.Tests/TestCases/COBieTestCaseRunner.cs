using Xbim.Common.Step21;
using Xbim.Common;
using Xbim.IDS.Validator.Tests.Common;
using Xbim.IO.CobieExpress;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Extensions.COBie.Tests.TestCases
{
    /// <summary>
    /// A set of services to run IDS TestCases against COBie, configured for this unit test project's TestEnvironment
    /// </summary>
    [Collection(nameof(COBieTestEnvironment))]
    public abstract class COBieTestCaseRunner : IdsTestCaseRunner
    {
        protected COBieTestCaseRunner(ITestOutputHelper output) : base(output)
        {
        }

        protected override IModel LoadModel(string modelFile, XbimSchemaVersion schemaVersion, bool spotfix)
        {
            if (schemaVersion == XbimSchemaVersion.Cobie2X4)
            {

                modelFile = @"..\..\..\TestModels\SampleHouse4.xlsx";
                return OpenCOBie(modelFile);

            }
            else
            {
                return base.LoadModel(modelFile, schemaVersion, spotfix);
            }

        }

        private IModel OpenCOBie(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(file);
            var mapping = CobieModel.GetMapping();
            //mapping.ClassMappings.RemoveAll(m => m.Class == "System");
            var model = CobieModel.ImportFromTable(file, out string report, mapping);

            return model;
        }
    }
}
