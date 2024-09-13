using static IdsLib.Audit;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Additional options that can be passed to the IDS validator
    /// </summary>
    public class VerificationOptions
    {
        /// <summary>
        /// All IDS Audit states
        /// </summary>
        public const Status AnyState = Status.Ok | Status.NotImplementedError | Status.InvalidOptionsError | Status.NotFoundError | Status.IdsStructureError |
        Status.IdsContentError | Status.XsdSchemaError | Status.UnhandledError | Status.IdsStructureWarning;

        /// <summary>
        /// IDS Audit States for relaxed validation
        /// </summary>
        public const Status Relaxed =  Status.Ok | Status.IdsContentError | Status.IdsStructureWarning;

        /// <summary>
        /// IDS Audit States for Strict validation
        /// </summary>
        public const Status Strict = Status.Ok;

        /// <summary>
        /// Indicates Entity queries should include Entity subtypes
        /// </summary>
        /// <remarks>e.g. IfcWall includes IfcWallStandardCase. Only required while there is no agreed standard to provide this flag on an entity</remarks>
        public bool IncludeSubtypes { get; set; }

        /// <summary>
        /// Determines if the full IFC entity is returned in the <see cref="IdsValidationResult"/>.
        /// </summary>
        public bool OutputFullEntity { get; set; }

        /// <summary>
        /// Determines if Derived Attributes can be used for applicability and requirements
        /// </summary>
        public bool AllowDerivedAttributes { get; set; }

        /// <summary>
        /// Determines which IDS Audit statuses are allowed. The default is <see cref="Relaxed"/>
        /// </summary>
        /// <remarks>Setting to <see cref="AnyState"/> will disable Schema Validation</remarks>
        public Status PermittedIdsAuditStatuses { get; set; } = Relaxed;

        /// <summary>
        /// Determines if the service will attempt to upgrade the IDS schema during the validation
        /// </summary>
        public bool PerformInPlaceSchemaUpgrade { get; set; } = true;

        /// <summary>
        /// Determines if verification will skip over specifications when the model schema is not compatible with the ifcVersion
        /// </summary>
        public bool SkipIncompatibleSpecification { get; set; } = false;
    }

    
}
