using FluentAssertions;
using IdsLib.IdsSchema.IdsNodes;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Tests.Binders;
using Xbim.InformationSpecifications;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests
{
    public class IdsMigratorTests : BaseBinderTests
    {
        public IdsMigratorTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CanUpgradeIdsFiles()
        {
            var migrator = new IdsSchemaMigrator(MigrationLogger);
            var idsFile = @"TestModels/Example.ids";
            // Act
            migrator.HasMigrationsToApply(idsFile).Should().BeTrue("Old IDS file");
            migrator.GetIdsVersion(idsFile).Should().Be(IdsVersion.Invalid);
            var result = migrator.MigrateToIdsSchemaVersion(idsFile, out var target, IdsLib.IdsSchema.IdsNodes.IdsVersion.Ids0_9_7);
            result.Should().BeTrue("Expected to migrate");
            target.Should().NotBeNull("Expected a document");


            // Assert
            AssertIdsSchemaLocation(target);
            migrator.HasMigrationsToApply(target).Should().BeFalse("Should be upgraded");

            AssertValidIdsSchema(target);

            var ids = Xids.LoadBuildingSmartIDS(target.Root);
            ids.Should().NotBeNull("Expected to load");

            ids.AllSpecifications().Should().HaveCount(4);

        }

        [InlineData(@"TestModels/Example.ids", IdsVersion.Invalid)]
        [InlineData(@"TestModels/BasicRequirements.ids", IdsVersion.Ids0_9_7)]
        [InlineData(@"TestModels/sample.ids", IdsVersion.Ids0_9_7)]
        [InlineData(@"Xbim.IDS.Validator.Core.xml", IdsVersion.Invalid)]    // Not IDS
        [Theory]
        public void CanReadIdsVersion(string idsFile, IdsVersion expected)
        {
            var migrator = new IdsSchemaMigrator(MigrationLogger);
            var version = migrator.GetIdsVersion(XDocument.Load(idsFile));
            version.Should().Be(expected);
        }

        [InlineData(@"TestModels/Example.ids", "0.0")]
        [InlineData(@"TestModels/BasicRequirements.ids", "0.9.7")]
        [InlineData(@"TestModels/sample.ids", "0.9.7")]
        [InlineData(@"Xbim.IDS.Validator.Core.xml", "0.0")]    // Not IDS
        [Theory]
        public void CanReadVersion(string idsFile, string expected)
        {
            var migrator = new IdsSchemaMigrator(MigrationLogger);
            var version = migrator.GetIdsVersion(XDocument.Load(idsFile)).GetVersion();
            version.Should().Be(new Version(expected));
        }

        [InlineData("IDS_v0.9.3-v0.9.6_migration", "0.9.3", "0.9.6")]
        [InlineData("IDS_v0.9.6-v0.9.7_migration", "0.9.6", "0.9.7")]
        [InlineData("IDS_v1.0_migration", "1.0")]
        [Theory]
        public void CanExtractVersions(string versionString, string expectedLow, string expectedHigh = "")
        {
            var versions = versionString.ExtractVersions();

            if(expectedHigh == "")
            {
                versions.Length.Should().Be(1);
                versions[0].Should().Be(new Version(expectedLow));
            }
            else
            {
                versions.Length.Should().Be(2);
                versions[0].Should().Be(new Version(expectedLow));
                versions[1].Should().Be(new Version(expectedHigh));
            }
        }

        private void AssertValidIdsSchema(XDocument target)
        {
            // validate with ids-lib
            var validator = new IdsValidator(GetLogger<IdsValidator>());
            using var newIdsStream = new MemoryStream();
            target.Save(newIdsStream);
            newIdsStream.Position = 0;
            var status = validator.ValidateIDS(newIdsStream);

            status.Should().Be(IdsLib.Audit.Status.Ok);
        }

        private static void AssertIdsSchemaLocation(XDocument target)
        {
            var location = target.ReadIdsSchemaLocation();
            location.Should().Be("http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/0.9.7/ids.xsd");
        }

        ILogger<IdsSchemaMigrator> MigrationLogger { get => GetLogger<IdsSchemaMigrator>(); }
    }
}
