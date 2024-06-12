using IdsLib;
using IdsLib.SchemaProviders;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.IDS.Validator.Core.Interfaces;


namespace Xbim.IDS.Validator.Core
{
    public class IdsValidator : IIdsValidator
    {
        private readonly ILogger<IdsValidator> logger;

        public IdsValidator(ILogger<IdsValidator> logger)
        {
            this.logger = logger;
        }

        public Audit.Status ValidateIDS(string idsFile)
        {
            if (File.Exists(idsFile))
            {
                using (var fileStream = File.OpenRead(idsFile))
                {
                    return ValidateIDS(fileStream);
                }
            }
            logger.LogWarning("IDS file not found {filename}", idsFile);
            return Audit.Status.InvalidOptionsError;
        }

        public Audit.Status ValidateIDS(Stream idsFileStream)
        {
            var options = new SingleAuditOptions
            {
                //OmitIdsContentAudit = true,
                XmlWarningAction = AuditProcessOptions.XmlWarningBehaviour.ReportAsWarning,
                IdsVersion = IdsLib.IdsSchema.IdsNodes.IdsVersion.Invalid,
                SchemaProvider = new FixedVersionSchemaProvider(IdsLib.IdsSchema.IdsNodes.IdsVersion.Ids1_0)

            };
            return Audit.Run(idsFileStream, options, logger);
        }

        public Audit.Status ValidateIdsFolder(string idsFolder, string? idsSchemaFile = default)
        {
            var batchOptions = new IdsFolderBatchOptions
            {
                InputSource = idsFolder,
                OmitIdsContentAuditPattern = ".*fail.*"
            };
            if (idsSchemaFile != null)
            {
                batchOptions.SchemaFiles = new List<string> { idsSchemaFile };
            }
            return Audit.Run(batchOptions, logger);

        }
    }

    public class IdsFolderBatchOptions : IBatchAuditOptions
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IEnumerable<string> SchemaFiles { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool AuditSchemaDefinition { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string InputExtension { get; set; } = "ids";

        /// <summary>
        /// <inheritdoc/>
        /// </summary>

        public string InputSource { get; set; } = string.Empty;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        
        public bool OmitIdsContentAudit { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        
        public string OmitIdsContentAuditPattern { get; set; } = string.Empty;
    }
}
