using Microsoft.Extensions.Logging;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    public class IdsModelValidator
    {
        public IdsModelValidator(IdsModelBinder modelBinder)
        {
            ModelBinder = modelBinder;
        }

        public IdsModelBinder ModelBinder { get; }

        public ValidationOutcome ValidateAgainstIds(string idsFile, ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var idsSpec = Xbim.InformationSpecifications.Xids.LoadBuildingSmartIDS(idsFile, logger);
            var outcome = new ValidationOutcome(idsSpec);
            if(idsSpec == null)
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

                    logger.LogInformation(" -- Spec {spec} : versions {ifcVersions}", spec.Name, spec.IfcVersion);
                    var applicableIfc = spec.Applicability.Facets.OfType<IfcTypeFacet>().FirstOrDefault();
                    if(applicableIfc == null) 
                    {
                        logger.LogWarning("Spec {spec} has no Applicability facets", spec.Name);
                        continue; 
                    }

                    logger.LogInformation("    Applicable to : {entity} with PredefinedType {predefined}", applicableIfc.IfcType.SingleValue(), applicableIfc.PredefinedType?.SingleValue());
                    foreach (var applicableFacet in spec.Applicability.Facets)
                    {
                        logger.LogDebug("       - {facetType}: where {description} ", applicableFacet.GetType().Name, applicableFacet.Short());
                    }

                    logger.LogInformation("    Requirements {reqCount}: {expectation}", spec.Requirement?.Facets.Count, spec.Requirement?.RequirementOptions?.FirstOrDefault().ToString() ?? "");
                    int idx = 1;
                    if (spec.Requirement?.Facets == null)
                    {
                        logger.LogWarning("Spec {spec} has no Requirement facets", spec.Name);
                        continue;
                    }

                    foreach (var reqFacet in spec.Requirement.Facets)
                    {
                        logger.LogInformation("       [{i}] {facetType}: check {description} ", idx++, reqFacet.GetType().Name, reqFacet.Short());
                    }
                    IEnumerable<IPersistEntity> items = ModelBinder.SelectApplicableEntities(spec);
                    logger.LogInformation("          Checking {count} applicable items", items.Count());
                    foreach (var item in items)
                    {
                        var i = item as IIfcRoot;
                        logger.LogInformation("        * {ID}: {Type} {Name} ", item.EntityLabel, item.GetType().Name, i?.Name);

                        idx = 1;
                        foreach (var facet in spec.Requirement.Facets)
                        {
                            var result = ModelBinder.ValidateRequirement(item, spec.Requirement, facet, logger);
                            LogLevel level = LogLevel.Information;
                            int pad = 0;
                            if (result.ValidationStatus == ValidationStatus.Inconclusive) { level = LogLevel.Warning; pad = 4; }
                            if (result.ValidationStatus == ValidationStatus.Failed) { level = LogLevel.Error; pad = 6; }
                            logger.Log(level, "{pad}           [{i}] {result}: Checking {short} : {req}", "".PadLeft(pad, ' '), idx++, result.ValidationStatus.ToString().ToUpperInvariant(), facet.Short(), facet.ToString());
                            foreach (var message in result.Messages)
                            {
                                logger.Log(level, "{pad}              #{entity} {message}", "".PadLeft(pad, ' '), item.EntityLabel, message.ToString());
                            }
                            requirementResult.ApplicableResults.Add(result);
                        }
                    }
                    if(requirementResult.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Failed) )
                    {
                        requirementResult.Status = ValidationStatus.Failed;
                    }
                    else if (requirementResult.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Success))
                    {
                        requirementResult.Status = ValidationStatus.Success;
                    }
                    // else inconclusive



                }
            }

            if (outcome.ExecutedRequirements.Any(r => r.Status == ValidationStatus.Failed))
            {
                outcome.Status = ValidationStatus.Failed;
            }
            else if(outcome.ExecutedRequirements.Any(r => r.Status == ValidationStatus.Success))
            {
                outcome.Status = ValidationStatus.Success;
            }
            // TODO: Consider Inconclusive
            return outcome;
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
