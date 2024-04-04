using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using IdsLib.IdsSchema.IdsNodes;
using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Interfaces;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Class to migrate IDS files to newer schema version using embedded XSLT transforms
    /// </summary>
    public class IdsSchemaMigrator : IIdsSchemaMigrator
    {

        /// <summary>
        /// Constructs a new <see cref="IdsSchemaMigrator"/>
        /// </summary>
        /// <param name="logger"></param>
        public IdsSchemaMigrator(ILogger<IdsSchemaMigrator> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Migrates the supplied IDS File to the target IDS version
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="target"></param>
        /// <param name="targetIdsVersion"></param>
        /// <returns><c>true</c> if the migration succeeded; else <c>false</c></returns>
        public bool MigrateToIdsSchemaVersion(string sourceFile, out XDocument target, IdsVersion targetIdsVersion = IdsVersion.Ids0_9_7)
        {
            try
            {
                XDocument currentIds = XDocument.Load(sourceFile);
                var applicableScripts = ListAvailableMigrations(sourceFile, targetIdsVersion, true);
                foreach (var script in applicableScripts)
                {
                    // TODO Does not account for targeting mid-range of a script. E.g. target 0.95 falls between 0.93 and 0.96 script.
                    currentIds = ApplyUpgrade(currentIds, script);
                }
                target = currentIds;

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to migrate IDS script");
            }
            target = new XDocument();
            return false;
        }


        /// <summary>
        /// Gets the <see cref="IdsVersion"/> from the IDS File
        /// </summary>
        /// <param name="idsFile"></param>
        /// <returns></returns>
        public IdsVersion GetIdsVersion(string idsFile)
        {
            XDocument currentIds;
            try
            {
                currentIds = XDocument.Load(idsFile);
                return GetIdsVersion(currentIds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to open file {file}", idsFile);
                return IdsVersion.Invalid;
            }
        }

        /// <summary>
        /// Gets the <see cref="IdsVersion"/> from the IDS Xml Document
        /// </summary>
        /// <param name="idsDoc"></param>
        /// <returns></returns>
        public IdsVersion GetIdsVersion(XDocument idsDoc)
        {
            return idsDoc.ReadIdsVersion();
        }

        /// <summary>
        /// Indicates whether this IDS file can be upgraded
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        public bool HasMigrationsToApply(string sourceFile) => ListAvailableMigrations(sourceFile).Any();


        /// <summary>
        /// Indicates whether this IDS Xml File can be upgraded
        /// </summary>
        /// <param name="idsDocument"></param>
        /// <returns></returns>
        public bool HasMigrationsToApply(XDocument idsDocument) => ListAvailableMigrations(idsDocument).Any();


        /// <summary>
        /// Lists the available IDS migration scripts
        /// </summary>
        /// <param name="sourceFile">The source IDS file</param>
        /// <param name="upperVersion">The desired upper version - defaults to 1.0</param>
        /// <param name="includeGlobalScripts">Whether to include version agnostic scripts</param>
        /// <returns></returns>
        public IEnumerable<string> ListAvailableMigrations(string sourceFile, IdsVersion upperVersion = IdsVersion.Ids1_0, bool includeGlobalScripts = false)
        {
            XDocument currentIds;
            try
            {
                currentIds = XDocument.Load(sourceFile);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to open file {file}", sourceFile);
                return Enumerable.Empty<string>();
            }

            return ListAvailableMigrations(currentIds, upperVersion, includeGlobalScripts);

        }

        /// <summary>
        /// Lists the available IDS migration scripts
        /// </summary>
        /// <param name="idsDocument">The source IDS Xml Document</param>
        /// <param name="upperVersion">The desired upper version - defaults to 1.0</param>
        /// <param name="includeGlobalScripts">Whether to include version agnostic scripts</param>
        /// <returns></returns>
        public IEnumerable<string> ListAvailableMigrations(XDocument idsDocument, IdsVersion upperVersion = IdsVersion.Ids1_0, bool includeGlobalScripts = false)
        {
            foreach (var script in GetUpgradeScripts())
            {
                var upgradeVersion = GetMigrationTargetVersion(script);

                if (ScriptInRange(upgradeVersion, idsDocument.ReadIdsVersion(), upperVersion) || (includeGlobalScripts && IsGlobalScript(upgradeVersion)))
                {
                    yield return script;
                }
            }
        }

        private static bool ScriptInRange(Version scriptVersion, IdsVersion currentIdsVersion, IdsVersion targetIdsVersion)
        {

            return (scriptVersion > currentIdsVersion.GetVersion() && scriptVersion <= targetIdsVersion.GetVersion());
        }

        private static bool IsGlobalScript(Version upgradeVersion)
        {
            return upgradeVersion == highVersion;
        }

        static readonly Version highVersion = new Version(999, 999);
        /// <summary>
        /// Gets the target version of a migration script
        /// </summary>
        /// <param name="scriptFileName"></param>
        /// <returns></returns>
        private Version GetMigrationTargetVersion(string scriptFileName)
        {
            var versions = scriptFileName.ExtractVersions();

            if (versions.Length == 1)
            {
                return versions[0];
            }
            if (versions.Length >= 2)
            {
                return versions[1];
            }
            return highVersion;
        }



        private XDocument ApplyUpgrade(XDocument sourceFile, string resourceName)
        {
            logger.LogInformation("Applying IDS Migration script {migrationScript}", Path.GetFileName(resourceName));
            var xlstTransformer = GetXsltTransformer(resourceName);
            XDocument target = new XDocument();
            using (XmlReader oldDocumentReader = sourceFile.CreateReader())
            {
                using (XmlWriter newDocumentWriter = target.CreateWriter())
                {
                    xlstTransformer.Transform(oldDocumentReader, newDocumentWriter);
                }
            }
            return target;
        }

        private static IEnumerable<string> GetUpgradeScripts()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var expected = $"{EmbeddedPath}.IDS";
            return assembly.GetManifestResourceNames().Where(m => m.StartsWith(expected)).OrderBy(s => s);


        }
        const string EmbeddedPath = "Xbim.IDS.Validator.Core.Migration";
        private readonly ILogger<IdsSchemaMigrator> logger;

        private static XslCompiledTransform GetXsltTransformer(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourceName);

            using var xsltReader = XmlReader.Create(stream);
            var transformer = new XslCompiledTransform();
            transformer.Load(xsltReader);
            return transformer;

        }
    }
}
