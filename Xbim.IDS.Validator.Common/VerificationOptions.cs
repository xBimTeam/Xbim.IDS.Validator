using System;
using System.Collections.Generic;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.InformationSpecifications;
using static IdsLib.Audit;

using TokenDictionary = System.Collections.Generic.Dictionary<string, string>;

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
        public const Status Relaxed = Status.Ok | Status.IdsContentError | Status.IdsStructureWarning;

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

        /// <summary>
        /// Provides a dictionary of tokens that can be replace design-time tokens in the supplied IDS
        /// </summary>
        /// <remarks>This may be used to customise project-specific requirements while retaining a project agnostic.
        ///  Given an IDS file containing an Facet such as:
        ///  <code>
        /// <![CDATA[<attribute cardinality="required">
		///   <name>
		///     <simpleValue>Name</simpleValue>
		///   </name>
		///   <value>
		///     <simpleValue>{{ProjectName}}</simpleValue>
        ///   </value>
        /// </attribute>
        /// ]]>
        /// </code>
        /// the token <c>{{ProjectName}}</c> will be replaced with the string value from the <see cref="TokenDictionary"/>
        /// </remarks>
        /// <example>
        ///
        /// </example>
        public TokenDictionary RuntimeTokens { get; set; } = new TokenDictionary();


        /// <summary>
        /// A predicate that allows specifications to be filtered from execution
        /// </summary>
        public Func<Specification, bool> SpecExecutionFilter { get; set; } = (_ => true);

        /// <summary>
        /// An optional set of <see cref="ISpecificationExclusion"/>s identifying entities to exclude from validation checks
        /// </summary>
        /// <remarks>Permits applicable entities to be skipped over, e.g. when a user may have accepted the specification can be safely ignored for selected elements.</remarks>
        public IList<ISpecificationExclusion> EntityExclusions { get; set; } = new List<ISpecificationExclusion>();
    }

    
}
