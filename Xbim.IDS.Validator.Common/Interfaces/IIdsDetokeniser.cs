using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Xbim.IDS.Validator.Common.Interfaces
{
    /// <summary>
    /// Interface defining how IDS files can be detokenised
    /// </summary>
    public interface IIdsDetokeniser
    {
        /// <summary>
        /// Generate a new IDS file by detokeninsing from a template
        /// </summary>
        /// <param name="templateStream"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        XDocument ReplaceTokens(Stream templateStream, IDictionary<string, string> tokens);


        /// <summary>
        /// Generate a new IDS file by detokeninsing from a template
        /// </summary>
        /// <param name="template"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        XDocument ReplaceTokens(FileInfo template, IDictionary<string, string> tokens);

        /// <summary>
        /// Generate a new IDS file by detokeninsing from a template
        /// </summary>
        /// <param name="template"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        XDocument ReplaceTokens(XDocument template, IDictionary<string, string> tokens);
    }
}
