using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        /// <inheritdoc/>
        public ValidationOutcome ValidateAgainstIds(IModel model, string idsFile, ILogger logger, VerificationOptions? options = default)
        {
            // Plan to obsolete the Synchronous
            return ValidateAgainstIdsAsync(model, idsFile, logger, null, options).Result;
        }

        /// <inheritdoc/>
        public Task<ValidationOutcome> ValidateAgainstIdsAsync(IModel model, string idsFile, ILogger logger, Action<ValidationRequirement>? requirementCompleted, VerificationOptions? verificationOptions = null,
            CancellationToken token = default)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {


                ModelBinder.SetOptions(verificationOptions);

                var idsSpec = Xbim.InformationSpecifications.Xids.LoadBuildingSmartIDS(idsFile, logger);
                var outcome = new ValidationOutcome(idsSpec);
                if (idsSpec == null)
                {
                    outcome.MarkCompletelyFailed($"Unable to open IDS file '{idsFile}'");
                    logger.LogError("Unable to open IDS file '{idsFile}", idsFile);
                    return Task.FromResult(outcome);
                }


                foreach (var group in idsSpec.SpecificationsGroups)
                {
                    logger.LogInformation("opening '{group}'", group.Name);
                    foreach (var spec in group.Specifications)
                    {

                        var requirementResult = ValidateRequirement(spec, model, logger, token);



                        if (requirementResult.Status != ValidationStatus.Error)
                        {
                            SetResults(spec, requirementResult);
                        }
                        // else inconclusive

                        if (requirementCompleted != null)
                        {
                            // report progress
                            requirementCompleted(requirementResult);
                        }
                        outcome.ExecutedRequirements.Add(requirementResult);

                    }
                }

                if (outcome.ExecutedRequirements.Any(r => r.Status == ValidationStatus.Fail))
                {
                    outcome.Status = ValidationStatus.Fail;
                }
                else if (outcome.ExecutedRequirements.Any(r => r.Status == ValidationStatus.Pass))
                {
                    outcome.Status = ValidationStatus.Pass;
                }
                // TODO: Consider Inconclusive
                return Task.FromResult(outcome);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Failed to complete validation");
                var badOutcome = new ValidationOutcome(new Xids());
                badOutcome.MarkCompletelyFailed(ex.Message);
                return Task.FromResult(badOutcome);
            }
        }

        private ValidationRequirement ValidateRequirement(Specification spec, IModel model, ILogger logger, CancellationToken token)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            
            var requirementResult = new ValidationRequirement(spec);

            using var entityCache = model.BeginEntityCaching();
            // If Supported?
            //using var inverseCache = model.BeginInverseCaching();
            try
            {
                logger.LogInformation(" -- {cardinality} Spec '{spec}' : versions {ifcVersions}", spec.Cardinality.Description, spec.Name, spec.IfcVersion);
               
                logger.LogInformation("    Applicable to : {applicable}", spec.Applicability.GetApplicabilityDescription());
                foreach (var applicableFacet in spec.Applicability.Facets)
                {
                    logger.LogDebug("       - {facetType}: where {description} ", applicableFacet.GetType().Name, applicableFacet.Short());
                }
                var facetReqs = spec.Requirement?.GetRequirementDescription();
                logger.LogInformation("    Requirements {reqCount}: {expectation}", spec.Requirement?.Facets.Count, facetReqs);
              

                // Get the applicable items
                IEnumerable<IPersistEntity> items = ModelBinder.SelectApplicableEntities(model, spec);
                token.ThrowIfCancellationRequested();
                logger.LogInformation("          Checking {count} applicable items", items.Count());
                foreach (var item in items)
                {
                    var i = item as IIfcRoot;

                    var result = ModelBinder.ValidateRequirement(item, spec.Requirement, logger);
                    GetLogLevel(result.ValidationStatus, out LogLevel level, out int pad);
                    logger.Log(level, "{pad}           [{result}]: {entity} because {short}", "".PadLeft(pad, ' '),
                        result.ValidationStatus.ToString().ToUpperInvariant(), item, spec.Requirement.Short());
                    foreach (var message in result.Messages)
                    {
                        GetLogLevel(message.Status, out level, out pad, LogLevel.Debug);
                        logger.Log(level, "{pad}              #{entity} {message}", "".PadLeft(pad, ' '), item.EntityLabel, message.ToString());
                    }
                    requirementResult.ApplicableResults.Add(result);
                    token.ThrowIfCancellationRequested();
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Failed to run specification: {reason}", ex.Message);
                requirementResult.Status = ValidationStatus.Error;
                var errorResult = new IdsValidationResult(null, null);
                errorResult.Messages.Add(ValidationMessage.Error(ex.Message));
                errorResult.ValidationStatus = ValidationStatus.Error;
                requirementResult.ApplicableResults.Add(errorResult);
            }
            

            return requirementResult;
        }

        private static void GetLogLevel(ValidationStatus status, out LogLevel level, out int pad, LogLevel defaultLevel = LogLevel.Information)
        {
            level = defaultLevel;
            pad = 0;
            if (status == ValidationStatus.Inconclusive) { level = LogLevel.Warning; pad = 4; }
            if (status == ValidationStatus.Fail) { level = LogLevel.Error; pad = 6; }
        }

        private static void SetResults(Specification specification, ValidationRequirement validation)
        {
            // TODO: Check this logic
            if (specification.Cardinality is SimpleCardinality simpleCard)
            {
                if (simpleCard.ExpectsRequirements) // Required or Optional
                {
                    if (validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Fail))
                    {
                        validation.Status = ValidationStatus.Fail;
                    }
                    else
                    {
                        if (simpleCard.IsModelConstraint) // Definitely required
                        {
                            validation.Status = validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Pass)
                                ? ValidationStatus.Pass
                                : ValidationStatus.Fail;
                        }
                        else
                        {
                            // Optional
                            validation.Status = ValidationStatus.Pass;
                        }
                    }

                }
                if (simpleCard.NoMatchingEntities)  // Prohibited
                {
                    if (validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Pass))
                    {
                        validation.Status = ValidationStatus.Fail;
                    }
                    else
                    {
                        validation.Status = ValidationStatus.Pass;
                    }
                }
            }
            else if (specification.Cardinality is MinMaxCardinality cardinality)
            {
                if (cardinality.ExpectsRequirements)
                {
                    if (cardinality.IsModelConstraint)
                    {
                        var successes = validation.ApplicableResults.Count(r => r.ValidationStatus == ValidationStatus.Pass);
                        // If None have failed and we have the number expected successful is within bounds of min-max we succeed
                        validation.Status = cardinality.IsSatisfiedBy(successes) &&
                            !validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Fail)
                            ? ValidationStatus.Pass
                            : ValidationStatus.Fail;

                    }
                    else
                    {
                        validation.Status = ValidationStatus.Pass;
                    }
                }
                if (cardinality.NoMatchingEntities)
                {
                    if (cardinality.IsModelConstraint)
                    {
                        var failures = validation.ApplicableResults.Count(r => r.ValidationStatus == ValidationStatus.Fail);
                        // If None have suceeded and we have the number expected failed is within bounds of min-max we succeed
                        validation.Status = (cardinality.MinOccurs <= failures && cardinality.MaxOccurs >= failures) &&
                            !validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Pass)
                            ? ValidationStatus.Pass
                            : ValidationStatus.Fail;
                    }
                    else
                    {
                        validation.Status = ValidationStatus.Pass;
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

        internal void MarkCompletelyFailed(string mesg)
        {
            Status = ValidationStatus.Error;
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

        /// <summary>
        /// Gets the results where the requirement failed, accounting for Prohibited requirements
        /// </summary>
        public IEnumerable<IdsValidationResult> FailedResults 
        { 
            get
            {
                return Specification.Cardinality.NoMatchingEntities ?
                    ApplicableResults.Where(a => a.ValidationStatus == ValidationStatus.Pass)
                    : ApplicableResults.Where(a => a.ValidationStatus == ValidationStatus.Fail);
            }

        }

        /// <summary>
        /// Gets the results where the requirement passed, accounting for Prohibited requirements
        /// </summary>
        public IEnumerable<IdsValidationResult> PassedResults
        {
            get
            {
                return Specification.Cardinality.NoMatchingEntities ?
                    ApplicableResults.Where(a => a.ValidationStatus == ValidationStatus.Fail)
                    : ApplicableResults.Where(a => a.ValidationStatus == ValidationStatus.Pass);
            }

        }

        /// <summary>
        /// Indicates if the result has failed the requirements
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool IsFailure(IdsValidationResult result)
        {
            return (Specification.Cardinality.ExpectsRequirements && result.ValidationStatus != ValidationStatus.Pass) ||
                (Specification.Cardinality.NoMatchingEntities && result.ValidationStatus != ValidationStatus.Fail);
        }

        /// <summary>
        /// Indicates if the result has met the requirements
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool IsSuccess(IdsValidationResult result)
        {
            return !IsFailure(result);
        }

    }
}
