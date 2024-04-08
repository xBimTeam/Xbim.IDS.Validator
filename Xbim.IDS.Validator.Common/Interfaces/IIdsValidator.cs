using IdsLib;
using System.IO;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    /// <summary>
    /// Represents a class used to audit the contents of BuildingSMART IDS specifications.
    /// </summary>
    public interface IIdsValidator
    {
        /// <summary>
        /// Validate an IDS file from the provided stream
        /// </summary>
        /// <param name="idsFileStream"></param>
        /// <returns></returns>
        Audit.Status ValidateIDS(Stream idsFileStream);

        /// <summary>
        /// Validate a specific IDS file
        /// </summary>
        /// <param name="idsFile"></param>
        /// <returns></returns>
        Audit.Status ValidateIDS(string idsFile);
        /// <summary>
        /// Validate all IDS files in the provided folder
        /// </summary>
        /// <param name="idsFolder"></param>
        /// <param name="idsSchemaFile"></param>
        /// <returns></returns>
        Audit.Status ValidateIdsFolder(string idsFolder, string? idsSchemaFile = null);
    }
}