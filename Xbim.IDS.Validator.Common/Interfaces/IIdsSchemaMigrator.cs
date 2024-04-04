using IdsLib.IdsSchema.IdsNodes;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    /// <summary>
    /// Interfaces for managing version upgrades on IDS files
    /// </summary>
    public interface IIdsSchemaMigrator
    {
        /// <summary>
        /// Gets the Schema version of an IDS File
        /// </summary>
        /// <param name="idsFile">The IDS File</param>
        /// <returns></returns>
        IdsVersion GetIdsVersion(string idsFile);

        /// <summary>
        /// Gets the Schema version of an IDS Xml Document
        /// </summary>
        /// <param name="idsDoc">The IDS <see cref="XDocument"/></param>
        /// <returns></returns>
        IdsVersion GetIdsVersion(XDocument idsDoc);

        /// <summary>
        /// Determines if the IDS file can be upgraded
        /// </summary>
        /// <param name="sourceFile">The IDS File</param>
        /// <returns></returns>
        bool HasMigrationsToApply(string sourceFile);
        /// <summary>
        /// Determines if the IDS Xml Document can be upgraded
        /// </summary>
        /// <param name="idsDocument">The IDS <see cref="XDocument"/></param>
        /// <returns></returns>
        bool HasMigrationsToApply(XDocument idsDocument);
        /// <summary>
        /// Lists the available Migrations for this IDS File
        /// </summary>
        /// <param name="sourceFile">The IDS File</param>
        /// <param name="upperVersion">The optional target version</param>
        /// <param name="includeGlobalScripts">Determines whether migration global scripts are included</param>
        /// <returns></returns>
        IEnumerable<string> ListAvailableMigrations(string sourceFile, IdsVersion upperVersion = IdsVersion.Ids1_0, bool includeGlobalScripts = false);
        /// <summary>
        /// Lists the available Migrations for this IDS Xml Document
        /// </summary>
        /// <param name="idsDocument">The IDS <see cref="XDocument"/></param>
        /// <param name="upperVersion">The optional target version</param>
        /// <param name="includeGlobalScripts">Determines whether migration global scripts are included</param>
        /// <returns></returns>
        IEnumerable<string> ListAvailableMigrations(XDocument idsDocument, IdsVersion upperVersion = IdsVersion.Ids1_0, bool includeGlobalScripts = false);

        /// <summary>
        /// Migrates the IDS file to the latest target version
        /// </summary>
        /// <param name="sourceFile">The IDS File</param>
        /// <param name="target">The output <see cref="XDocument"/> with the upgraded IDS</param>
        /// <param name="targetIdsVersion">The optional target version</param>
        /// <returns></returns>
        bool MigrateToIdsSchemaVersion(string sourceFile, out XDocument target, IdsVersion targetIdsVersion = IdsVersion.Ids0_9_7);
    }
}