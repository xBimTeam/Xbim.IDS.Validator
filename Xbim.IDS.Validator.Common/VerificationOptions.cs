namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Additional options that can be passed to the IDS validator
    /// </summary>
    public class VerificationOptions
    {
        /// <summary>
        /// Indicates Entity queries should include Entity subtypes
        /// </summary>
        /// <remarks>e.g. IfcWall includes IfcWallStandardCase. Only required while there is no agreed standard to provide this flag on an entity</remarks>
        public bool IncludeSubtypes { get; set; }

        /// <summary>
        /// Determines if the full IFC entity is returned in the <see cref="IdsValidationResult"/>.
        /// </summary>
        public bool OutputFullEntity { get; set; }
    }
}
