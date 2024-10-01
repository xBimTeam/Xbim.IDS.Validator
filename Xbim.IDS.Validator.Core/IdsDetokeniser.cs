using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Xbim.IDS.Validator.Common.Interfaces;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Class to replace tokens with runtime values in an IDS file
    /// </summary>
    public class IdsDetokeniser : IIdsDetokeniser
    {
        private readonly ILogger<IdsDetokeniser> logger;

        public IdsDetokeniser(ILogger<IdsDetokeniser> logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public XDocument ReplaceTokens(Stream templateStream, IDictionary<string, string> tokens)
        {
            XDocument ids = XDocument.Load(templateStream);
            return ReplaceTokens(ids, tokens);
        }

        /// <inheritdoc/>
        public XDocument ReplaceTokens(FileInfo template, IDictionary<string, string> tokens)
        {
            if (template.Exists)
            {
                XDocument ids = XDocument.Load(template.FullName);
                return ReplaceTokens(ids, tokens);
            }
            else
            {
                throw new FileNotFoundException("IDS template not found", template.FullName);
            }
        }

        /// <inheritdoc/>
        public XDocument ReplaceTokens(XDocument template, IDictionary<string, string> tokens)
        {
            var tokenKeys = tokens.Keys;
            logger.LogInformation("Replacing IDS tokens: {tokenKeys}", tokenKeys);
            var content = template.ToString();
            // Naive token replacement. We could be using something like HandleBars.net if we want to optimise or employ control flow concepts etc.
            foreach (var pair in tokens)
            {
                var key = "{{" + pair.Key + "}}";
                content = content.Replace(key, pair.Value);
            }

            return XDocument.Parse(content);
        }
    }
}
