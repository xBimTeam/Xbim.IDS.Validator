using IdsLib.IdsSchema.IdsNodes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.IDS.Validator.Common;
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
        private readonly IIdsSchemaMigrator idsSchemaMigrator;

        /// <summary>
        /// Constructs a new <see cref="IdsModelValidator"/>
        /// </summary>
        /// <param name="modelBinder"></param>
        /// <param name="idsSchemaMigrator"></param>
        public IdsModelValidator(IIdsModelBinder modelBinder, IIdsSchemaMigrator idsSchemaMigrator)
        {
            ModelBinder = modelBinder;
            this.idsSchemaMigrator = idsSchemaMigrator;
        }

        private IIdsModelBinder ModelBinder { get; }

        /// <inheritdoc/>
        public ValidationOutcome ValidateAgainstIds(IModel model, string idsFile, ILogger logger, VerificationOptions? options = default)
        {
            // Plan to obsolete the Synchronous
            return ValidateAgainstIdsAsync(model, idsFile, logger, null, options).Result;
        }

        /// <inheritdoc/>
        public Task<ValidationOutcome> ValidateAgainstXidsAsync(IModel model, Xids idsSpec, ILogger logger, Action<ValidationRequirement>? requirementCompleted, VerificationOptions? verificationOptions = null,
            CancellationToken token = default)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {


                ModelBinder.SetOptions(verificationOptions);

                var outcome = new ValidationOutcome(idsSpec);


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
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to complete validation");
                var badOutcome = new ValidationOutcome(new Xids());
                badOutcome.MarkCompletelyFailed(ex.Message);
                return Task.FromResult(badOutcome);
            }
        }

        /// <inheritdoc/>
        public async Task<ValidationOutcome> ValidateAgainstIdsAsync(IModel model, string idsFile, ILogger logger, Action<ValidationRequirement>? requirementCompleted, VerificationOptions? verificationOptions = null,
            CancellationToken token = default)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {
                ModelBinder.SetOptions(verificationOptions);

                if(!Xids.CanLoad(new FileInfo(idsFile)))
                {
                    var outcome = new ValidationOutcome(new Xids());
                    outcome.MarkCompletelyFailed($"Unable to open IDS file '{idsFile}'");
                    logger.LogError("Unable to open IDS file '{idsFile}", idsFile);
                    return outcome;
                }
                
                Xids? idsSpec = LoadIdsFile(idsFile, logger);

                return await ValidateAgainstXidsAsync(model, idsSpec, logger, requirementCompleted, verificationOptions, token);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to complete validation");
                var badOutcome = new ValidationOutcome(new Xids());
                badOutcome.MarkCompletelyFailed(ex.Message);
                return await Task.FromResult(badOutcome);
            }
        }

        private Xids? LoadIdsFile(string idsFile, ILogger logger)
        {
            if (idsSchemaMigrator.HasMigrationsToApply(idsFile))
            {
                // Do an in place upgrade to latest schema
                // Note: won't support zipped IDS upgrades, JSON etc.
                var targetVersion = IdsVersion.Ids0_9_7;
                var currentVersion = idsSchemaMigrator.GetIdsVersion(idsFile);
                logger.LogWarning("IDS schema {oldVersion} is out of date for {file}. Applying in-place upgrade to latest {version} schema.", 
                    currentVersion, idsFile, targetVersion);
                if(idsSchemaMigrator.MigrateToIdsSchemaVersion(idsFile, out var upgraded, targetVersion))
                {
                    logger.LogInformation("IDS file {file} upgraded in-place to latest schema", idsFile);
                    return Xids.LoadBuildingSmartIDS(upgraded.Root, logger);
                }
                else
                {
                    logger.LogWarning("Failed to update IDS to latest schema. Using original version");
                }

            }
            return Xids.LoadBuildingSmartIDS(idsFile, logger);
        }

        private ValidationRequirement ValidateRequirement(Specification spec, IModel model, ILogger logger, CancellationToken token)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            
            var requirementResult = new ValidationRequirement(spec);

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
                IEnumerable<IPersistEntity> items = ModelBinder.SelectApplicableEntities(model, spec).Distinct();
                token.ThrowIfCancellationRequested();
                logger.LogInformation("          Checking {count} applicable items", items.Count());
                foreach (var item in items)
                {
                    var i = item as IIfcRoot;

                    var result = ModelBinder.ValidateRequirement(item, spec.Requirement, logger);
                    GetLogLevel(result.ValidationStatus, out LogLevel level, out int pad);
                    logger.Log(level, "{pad}           [{result}]: {entity} because {short}", "".PadLeft(pad, ' '),
                        result.ValidationStatus.ToString().ToUpperInvariant(), item, spec.Requirement?.Short() ?? "No requirement");
                    foreach (var message in result.Messages)
                    {
                        GetLogLevel(message.Status, out level, out pad, LogLevel.Debug);
                        logger.Log(level, "{pad}              #{entity} {message}", "".PadLeft(pad, ' '), item.EntityLabel, message.ToString());
                    }
                    requirementResult.ApplicableResults.Add(result);
                    token.ThrowIfCancellationRequested();
                }

                var relAggregations = model.Instances.OfType<IIfcRelAggregates>(true);
                //add the reverse lookup
                var aggregationReverseLookup = new XbimMultiValueDictionary<int, int>();
                foreach (var relAggregation in relAggregations.Where(rel => rel.RelatingObject != null && requirementResult.ApplicableResults.Select(x => x.Entity).Contains(rel.RelatingObject.EntityLabel))) //only take top level assemblies that are in the filter
                {
                    foreach (var relObject in relAggregation.RelatedObjects)
                    {

                        var result = requirementResult.ApplicableResults.FirstOrDefault(x => x.Entity == relObject.EntityLabel);
                        if (result != null)
                            result.ParentEntity = relAggregation.EntityLabel;
                    }
                }

                XbimMultiValueDictionary<int, int> _openingsLookup = new XbimMultiValueDictionary<int, int>();

                var opening = model.Instances.OfType<IIfcGeometricRepresentationItem>(true);
                var voids = model.Instances.OfType<IIfcRelVoidsElement>(true).ToList();
                foreach (var v in voids)
                {
                    var result = requirementResult.ApplicableResults.FirstOrDefault(x => x.Entity == v.RelatedOpeningElement.EntityLabel);
                    if(result != null)
                        result.ParentEntity = v.RelatingBuildingElement.EntityLabel;
                }
                

            }
            catch (Exception ex)
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
                if (simpleCard.AllowsRequirements) // Required or Optional
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
                    if (validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Pass || r.ValidationStatus == ValidationStatus.Inconclusive))
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
                if (cardinality.AllowsRequirements)
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

   
}
