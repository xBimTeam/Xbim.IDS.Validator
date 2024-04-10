using IdsLib;
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

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Class that will validate a model against a set of IDS specification requirements
    /// </summary>
    public class IdsModelValidator : IIdsModelValidator
    {
        private readonly IIdsSchemaMigrator idsSchemaMigrator;
        private readonly IIdsValidator schemaValidator;

        /// <summary>
        /// Constructs a new <see cref="IdsModelValidator"/>
        /// </summary>
        /// <param name="modelBinder"></param>
        /// <param name="idsSchemaMigrator"></param>
        /// <param name="schemaValidator"></param>
        public IdsModelValidator(IIdsModelBinder modelBinder, IIdsSchemaMigrator idsSchemaMigrator, IIdsValidator schemaValidator)
        {
            ModelBinder = modelBinder;
            this.idsSchemaMigrator = idsSchemaMigrator;
            this.schemaValidator = schemaValidator;
        }

        private IIdsModelBinder ModelBinder { get; }

        /// <inheritdoc/>
        public ValidationOutcome ValidateAgainstIds(IModel model, string idsFile, ILogger logger, VerificationOptions? options = default)
        {
            // Plan to obsolete the Synchronous
            return ValidateAgainstIdsAsync(model, idsFile, logger, null, options).Result;
        }

        /// <inheritdoc/>
        public async Task<ValidationOutcome> ValidateAgainstXidsAsync(IModel model, Xids idsSpec, ILogger logger, Func<ValidationRequirement, Task>? requirementCompleted, VerificationOptions? verificationOptions = null,
            CancellationToken token = default)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {
                verificationOptions ??= new VerificationOptions();
                ModelBinder.SetOptions(verificationOptions);

                var outcome = new ValidationOutcome(idsSpec);
                if(verificationOptions.PermittedIdsAuditStatuses != VerificationOptions.AnyState)
                {
                    // Validate the IDS
                    Audit.Status schemaStatus = ValidateIdsSchema(idsSpec);

                    if(!verificationOptions.PermittedIdsAuditStatuses.HasFlag(schemaStatus))
                    {
                        outcome.MarkCompletelyFailed($"IDS Validation failed: {schemaStatus}");
                        var name = idsSpec.AllSpecifications().Select(x => x.Name).FirstOrDefault();
                        logger.LogError("IDS '{idsName}' was invalid: {auditstatus}", name, schemaStatus);
                        return outcome;
                    }
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
                            await requirementCompleted(requirementResult);
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
                return outcome;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to complete validation");
                var badOutcome = new ValidationOutcome(new Xids());
                badOutcome.MarkCompletelyFailed(ex.Message);
                return badOutcome;
            }
        }

        private Audit.Status ValidateIdsSchema(Xids idsSpec)
        {
            using (var memStream = new MemoryStream())
            {
                idsSpec.ExportBuildingSmartIDS(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                return schemaValidator.ValidateIDS(memStream);
            }
        }

        /// <inheritdoc/>
        public async Task<ValidationOutcome> ValidateAgainstIdsAsync(IModel model, string idsFile, ILogger logger, Func<ValidationRequirement, Task>? requirementCompleted, VerificationOptions? verificationOptions = null,
            CancellationToken token = default)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            try
            {
                ModelBinder.SetOptions(verificationOptions);

                if (!Xids.CanLoad(new FileInfo(idsFile)))
                {
                    var outcome = new ValidationOutcome(new Xids());
                    outcome.MarkCompletelyFailed($"Unable to open IDS file '{idsFile}'");
                    logger.LogError("Unable to open IDS file '{idsFile}", idsFile);
                    return outcome;
                }

                Xids? idsSpec = LoadIdsFile(idsFile, logger, verificationOptions);

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

        private Xids? LoadIdsFile(string idsFile, ILogger logger, VerificationOptions? verificationOptions)
        {
            if (verificationOptions?.PerformInPlaceSchemaUpgrade == true && idsSchemaMigrator.HasMigrationsToApply(idsFile))
            {
                // Do an in place upgrade to latest schema
                // Note: won't support zipped IDS upgrades, JSON etc.
                var targetVersion = IdsVersion.Ids0_9_7;
                var currentVersion = idsSchemaMigrator.GetIdsVersion(idsFile);
                logger.LogWarning("IDS schema {oldVersion} is out of date for {file}. Applying in-place upgrade to latest {version} schema.",
                    currentVersion, idsFile, targetVersion);
                if (idsSchemaMigrator.MigrateToIdsSchemaVersion(idsFile, out var upgraded, targetVersion))
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
                var specCardinality = spec.Cardinality;
                logger.LogInformation(" -- {cardinality} Spec '{spec}' : versions {ifcVersions}", specCardinality.Description, spec.Name, spec.IfcVersion);

                var modelSchema = model.SchemaVersion.ToString();
                if (!spec.IfcVersion.Any(s => s.ToString().Equals(modelSchema, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var versions = spec.IfcVersion.Any() ? spec.IfcVersion.Select(s => s.ToString()).Aggregate((a, b) => $"{a},{b}") : "";
                    logger.LogWarning("Specification may not be compatible for this model. Spec is for {versions}, while model is {modelVersion}", versions, modelSchema);
                }

                logger.LogInformation("    Applicable to : {applicable}", spec.Applicability.GetApplicabilityDescription());
                foreach (var applicableFacet in spec.Applicability.Facets)
                {
                    logger.LogDebug("       - {facetType}: where {description} ", applicableFacet.GetType().Name, applicableFacet.Short());
                }

                if (specCardinality.AllowsRequirements)
                {
                    var facetReqs = spec.Requirement?.GetRequirementDescription();
                    logger.LogInformation("    Requirements {reqCount}: {expectation}", spec.Requirement?.Facets.Count, facetReqs);
                }



                // Get the applicable items
                IEnumerable<IPersistEntity> items = ModelBinder.SelectApplicableEntities(model, spec);
                logger.LogInformation("          Found {count} applicable items", items.Count());

                foreach (var item in items)
                {
                    if (specCardinality.NoMatchingEntities) // Prohibited items
                    {
                        var result = new IdsValidationResult(item, spec.Applicability);
                        var message = ValidationMessage.Prohibited(item);
                        result.Fail(message);
                        GetLogLevel(result.ValidationStatus, out LogLevel level, out int pad);
                        if(logger.IsEnabled(level))
                            logger.Log(level, "{pad}           [{result}]: {entity} because {short}", "".PadLeft(pad, ' '),
                                result.ValidationStatus.ToString().ToUpperInvariant(), item, spec.Applicability?.Short() ?? "No applicability");
                        requirementResult.ApplicableResults.Add(result);
                    }
                    else
                    {
                        // Test requirements are met
                        var result = ModelBinder.ValidateRequirement(item, spec.Requirement, logger);
                        GetLogLevel(result.ValidationStatus, out LogLevel level, out int pad);
                        if (logger.IsEnabled(level))
                            logger.Log(level, "{pad}           [{result}]: {entity} because {short}", "".PadLeft(pad, ' '),
                                result.ValidationStatus.ToString().ToUpperInvariant(), item, spec.Requirement?.Short() ?? "No requirement");
                        foreach (var message in result.Messages)
                        {
                            GetLogLevel(message.Status, out level, out pad, LogLevel.Debug);
                            if (logger.IsEnabled(level))
                                logger.Log(level, "{pad}              #{entity} {message}", "".PadLeft(pad, ' '), item.EntityLabel, message.ToString());
                        }
                        requirementResult.ApplicableResults.Add(result);
                    }
                    token.ThrowIfCancellationRequested();
                }
                PostProcessAssembliesAndVoids(model, requirementResult);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run specification: {reason}", ex.Message);
                requirementResult.Status = ValidationStatus.Error;
                var errorResult = new IdsValidationResult(null, null);
                errorResult.FailWithError(ValidationMessage.Error(ex.Message));
                requirementResult.ApplicableResults.Add(errorResult);
            }


            return requirementResult;
        }

        private static void PostProcessAssembliesAndVoids(IModel model, ValidationRequirement requirementResult)
        {
            // Optimisations to help elements thart may not have representation to a parent that can be visualised

            // Assign parents to child items. E.g IfcRoof Composed of IfcSlabs
            var relAggregations = model.Instances.OfType<IIfcRelAggregates>(true);
            foreach (var relAggregation in relAggregations.Where(rel => rel.RelatingObject != null && requirementResult.ApplicableResults.Select(x => x.Entity).Contains(rel.RelatingObject.EntityLabel))) //only take top level assemblies that are in the filter
            {
                foreach (var relObject in relAggregation.RelatedObjects)
                {

                    var result = requirementResult.ApplicableResults.FirstOrDefault(x => x.Entity == relObject.EntityLabel);
                    if (result != null)
                        result.ParentEntity = relAggregation.EntityLabel;
                }
            }

            // Link Openings to their parent
            var voids = model.Instances.OfType<IIfcRelVoidsElement>(true).ToList();
            foreach (var v in voids)
            {
                var result = requirementResult.ApplicableResults.FirstOrDefault(x => x.Entity == v.RelatedOpeningElement.EntityLabel);
                if (result != null)
                    result.ParentEntity = v.RelatingBuildingElement.EntityLabel;
            }
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
            
            var cardinality = specification.Cardinality;
            if (cardinality.NoMatchingEntities)  // Prohibited
            {
                if (validation.ApplicableResults.Any())
                {
                    validation.Status = ValidationStatus.Fail;
                }
                else
                {
                    validation.Status = ValidationStatus.Pass;
                }
            }
            else if (cardinality.AllowsRequirements) // Required or Optional
            {
                if (validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Fail))
                {
                    validation.Status = ValidationStatus.Fail;
                }
                else
                {
                    if (cardinality.IsModelConstraint) // Required Items - None should fail the requirements
                    {
                        validation.Status = validation.ApplicableResults.Any(r => r.ValidationStatus == ValidationStatus.Pass || r.ValidationStatus == ValidationStatus.Inconclusive)
                            ? ValidationStatus.Pass
                            : ValidationStatus.Fail;
                    }
                    else
                    {
                        // Optional pass so long as we had no failure
                        validation.Status = ValidationStatus.Pass;
                    }
                }

            }
            
        }

    }


}
