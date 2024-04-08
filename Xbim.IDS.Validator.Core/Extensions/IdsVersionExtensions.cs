using IdsLib.IdsSchema.IdsNodes;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xbim.IDS.Validator.Core.Extensions
{

    public static class IdsVersionExtensions
    {
        /// <summary>
        /// Gets a <see cref="Version"/> for an <see cref="IdsVersion"/>
        /// </summary>
        /// <param name="version">The IDS version</param>
        /// <returns>a <see cref="Version"/></returns>
        public static Version GetVersion(this IdsVersion version)
        {
            if (version == IdsVersion.Invalid)
                return new Version(0, 0);

            var versionStr = version.ToString()
                .Replace("_", ".")
                .Replace("Ids", "");
            
            return new Version(versionStr);
        }

        static readonly Regex pattern = new Regex("v(?'version'((\\d)\\.?){1,3})");

        /// <summary>
        /// Extracts all <see cref="Version"/>s from a string in a format 'vx.y.z'
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        public static Version[] ExtractVersions(this string versionString) 
        {
            var matches = pattern.Matches(versionString);

            return matches.Select(m => m.Groups["version"].Value)
                .Select(s => new Version(s))
                .ToArray();
            
        }
    }
}
