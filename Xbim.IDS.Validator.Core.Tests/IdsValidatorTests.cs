using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Tests.Binders;
using Xunit.Abstractions;
using IdsLib;
using static IdsLib.Audit;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class IdsValidatorTests : BaseModelTester
    {
        public IdsValidatorTests(ITestOutputHelper output) : base(output)
        {
        }

        [InlineData(@"TestModels\IDS_wooden-windows.ids", Status.Ok)]
        [InlineData(@"TestModels\sample.ids", Status.IdsContentError)]
        [InlineData(@"TestModels\BasicRequirements.ids", Status.IdsContentError)]
        [InlineData(@"TestModels\Example.ids", Status.IdsStructureError | Status.IdsContentError)]
        [Theory]
        public void CanValidateIDS(string filename, Status expectedStatus)
        {
            var validator = new IdsValidator(Logger);
            var result = validator.ValidateIDS(filename);

            result.Should().Be(expectedStatus);
        }

        [Fact]
        public void CanTestFolderInBatch()
        {
            var validator = new IdsValidator(Logger);
            var result = validator.ValidateIdsFolder(@"TestCases", @"TestModels\ids-0.9.6.xsd");
            result.Should().Be(Status.IdsStructureError | Status.IdsContentError);
        }


        ILogger<IdsValidator> Logger { get => GetLogger<IdsValidator>(); }
    }
}
