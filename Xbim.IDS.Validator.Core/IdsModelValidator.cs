using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xbim.InformationSpecifications.Cardinality;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Class that will validate a model against a set of IDS specification requirements
    /// </summary>
    public class IdsModelValidator : IIdsModelValidator
    {
        public IdsModelValidator(IIdsModelBinder modelBinder)
        {
            ModelBinder = modelBinder;
        }

        public IIdsModelBinder ModelBinder { get; }

        public ValidationOutcome ValidateAgainstIds(IModel model, string idsFile, ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var idsSpec = Xbim.InformationSpecifications.Xids.LoadBuildingSmartIDS(idsFile, logger);
            var outcome = new ValidationOutcome(idsSpec);
            if (idsSpec == null)
            {
                outcome.MarkFailed($"Unable to open IDS file '{idsFile}'");
                logger.LogError("Unable to open IDS file '{idsFile}", idsFile);
                return outcome;
            }


            foreach (var group in idsSpec.SpecificationsGroups)
            {
                logger.LogInformation("opening '{group}'", group.Name);
                foreach (var spec in group.Specifications)
                {
                    var requirementResult = new ValidationRequirement(spec);
                    outcome.ExecutedRequirements.Add(requirementResult);

                    logger.LogInformation(" -- {cardinality} Spec '{spec}' : versions {ifcVersions}", spec.Cardinality.Description, spec.Name, spec.IfcVersion);
                    var applicableIfc = spec.Applicability.Facets.OfType<IfcTypeFacet>().FirstOrDefault();
                    if (applicableIfc == null)
                    {
                        logger.LogWarning("Spec {spec} has no Applicability facets", spec.Name);
                        continue;
                    }

                    logger.LogInformation("    Applicable to : {entity} with PredefinedType {predefined}", applicableIfc.IfcType.SingleValue(), applicableIfc.PredefinedType?.SingleValue());
                    foreach (var applicableFacet in spec.Applicability.Facets)
                    {
                        logger.LogDebug("       - {facetType}: where {description} ", applicableFacet.GetType().Name, applicableFacet.Short());
                    }
                    var facetReqs = string.Join(',', spec.Requirement?.RequirementOptions?.Select(r => r.ToString() != null ? r.ToString() : "") ?? new[] { "" });
                    logger.LogInformation("    Requirements {reqCount}: {expectation}", spec.Requirement?.Facets.Count, facetReqs);
                    int idx = 1;
                    if (spec.Requirement?.Facets == null)
                    {
                        logger.LogWarning("Spec {spec} has no Requirement facets", spec.Name);
                        continue;
                    }

                    foreach (var reqFacet in spec.Requirement.Facets)
                    {
                        logger.LogInformation("       [r{i}] {facetType}: check {description} ", idx++, reqFacet.GetType().Name, reqFacet.Short());
                    }
                    IEnumerable<IPersistEntity> items = ModelBinder.SelectApplicableEntities(model, spec);
                    logger.LogInformation("          Checking {count} applicable items", items.Count());
                    foreach (var item in items)
                    {
                        var i = item as IIfcRoot;
                        logger.LogInformation("          * {entity}", item);


                        var result = ModelBinder.ValidateRequirement(item, spec.Requirement, logger);
                        LogLevel level;
                        int pad;
                        GetLogLevel(result.ValidationStatus, out level, out pad);
                        logger.Log(level, "{pad}           {result}: Checking {short}", "".PadLeft(pad, ' '), 
                            result.ValidationStatus.ToString().ToUpperInvariant(), spec.Requirement.Short());
                        foreach (var message in result.Messages)
                        {
                            GetLogLevel(message.Status, out level, out pad, LogLevel.Debug);
                            logger.Log(level, "{pad}              #{entity} {message}", "".PadLeft(pad, ' '), item.EntityLabel, message.ToString());
                        }
                        requirementResult.ApplicableResults.Add(result);

                    }

                    SetResults(spec, requirementResult);
                    // else inconclusive



                }
            }

            if (outcome.ExecutedRequirements.Any(r => r.Status == ValidationStatus.Failed))
            {
                outcome.Status = ValidationStatus.Failed;
            }
            else if (outcome.ExecutedRequirements.Any(r => r.Status == ValidationStatus.Success))
            {
                outcome.Status = ValidationStatus.Success;
            }
            // TODO: Consider Inconclusive
            return outcome;
        }

        private static void GetLogLevel(ValidationStatus status, out LogLevel level, out int pad, LogLevel defaultLevel = LogLevel.Information)
        {
            level = defaultLevel;
            pad = 0;
            if (status == ValidationStatus.Inconclusive) { level = LogLevel.Warning; pad = 4; }
            if (status == ValidationStatus.Failed) { level = LogLevel.Error; pad = 6; }
        }

        private static void SetResults(Specification specification, ValidationRequirement validation)
        {
            // TODO: Check this logic
            if (specification.Cardinality is SimpleCardinality simpleCard)
            {
                if (simpleCard.ExpectsRequirements) // Required or Optional
                {
                    if (validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Failed))
                    {
                        validation.Status = ValidationStatus.Failed;
                    }
                    else
                    {
                        if (simpleCard.IsModelConstraint) // Definitely required
                        {
                            validation.Status = validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Success)
                                ? ValidationStatus.Success
                                : ValidationStatus.Failed;
                        }
                        else
                        {
                            // Optional
                            validation.Status = ValidationStatus.Success;
                        }
                    }

                }
                if (simpleCard.NoMatchingEntities)  // Prohibited
                {
                    if (validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Success))
                    {
                        validation.Status = ValidationStatus.Failed;
                    }
                    else
                    {
                        validation.Status = ValidationStatus.Success;
                    }
                }
            }
            else if (specification.Cardinality is MinMaxCardinality cardinality)
            {
                if (cardinality.ExpectsRequirements)
                {
                    if (cardinality.IsModelConstraint)
                    {
                        var successes = validation.ApplicableResults.Count(r => r.ValidationStatus == ValidationStatus.Success);
                        // If None have failed and we have the number expected successful is within bounds of min-max we succeed
                        validation.Status = cardinality.IsSatisfiedBy(successes) &&
                            !validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Failed)
                            ? ValidationStatus.Success
                            : ValidationStatus.Failed;

                    }
                    else
                    {
                        validation.Status = ValidationStatus.Success;
                    }
                }
                if (cardinality.NoMatchingEntities)
                {
                    if (cardinality.IsModelConstraint)
                    {
                        var failures = validation.ApplicableResults.Count(r => r.ValidationStatus == ValidationStatus.Failed);
                        // If None have suceeded and we have the number expected failed is within bounds of min-max we succeed
                        validation.Status = (cardinality.MinOccurs <= failures && cardinality.MaxOccurs >= failures) &&
                            !validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Success)
                            ? ValidationStatus.Success
                            : ValidationStatus.Failed;
                    }
                    else
                    {
                        validation.Status = ValidationStatus.Success;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents the top level outcome of an IDS validation run
    /// </summary>
    public class ValidationOutcome
    {
        public ValidationOutcome(Xids idsDocument)
        {
            IdsDocument = idsDocument;
        }
        public Xids IdsDocument { get; private set; }

        /// <summary>
        /// The high level results of the requirements defined in the executed as part of this validation run
        /// </summary>
        public IList<ValidationRequirement> ExecutedRequirements { get; private set; } = new List<ValidationRequirement>();

        /// <summary>
        /// The overall status of the Validation run
        /// </summary>
        public ValidationStatus Status { get; internal set; } = ValidationStatus.Inconclusive;

        public string? Message { get; private set; }

        internal void MarkFailed(string mesg)
        {
            Status = ValidationStatus.Failed;
            Message = mesg;
        }
    }

    /// <summary>
    /// Represents a requirement result after validation
    /// </summary>
    /// <remarks>E.g. all Doors must have a Firerating</remarks>
    public class ValidationRequirement
    {
        public ValidationRequirement(Specification spec)
        {
            Specification = spec;
        }

        /// <summary>
        ///  The status of this requirement
        /// </summary>
        public ValidationStatus Status { get; internal set; } = ValidationStatus.Inconclusive;


        /// <summary>
        /// The IDS specification of this Requirement
        /// </summary>
        public Specification Specification { get; }

        /// <summary>
        /// The results of testing this specification against applicable entities in the model
        /// </summary>
        public IList<IdsValidationResult> ApplicableResults { get; private set; } = new List<IdsValidationResult>();
    }
}
