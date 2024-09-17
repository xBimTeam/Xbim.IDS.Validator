﻿using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Xbim.Common;
using Xbim.IDS.Validator.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.IDS.Validator.Core;
using Xbim.IO.Memory;
using Xbim.InformationSpecifications;
using static System.ConsoleColor;
using Microsoft.Extensions.Options;

namespace Xbim.IDS.Validator.Console
{
    /// <summary>
    /// Verifies IFC and COBie models against a set of IDS files.
    /// </summary>
    internal class VerifyIfcCommand: ICommand
    {
        private readonly IIdsModelValidator idsValidator;
        private readonly ILogger<VerifyIfcCommand> logger;
        private readonly IdsConfig config;
        private ConsoleLogger console = new ConsoleLogger(Verbosity.Normal);

        /// <summary>
        /// Constructs a new <see cref="VerifyIfcCommand"/>
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public VerifyIfcCommand(IIdsModelValidator validator, ILogger<VerifyIfcCommand> logger, IOptions<IdsConfig> config)
        {
            this.idsValidator = validator;
            this.logger = logger;
            this.config = config.Value;
        }

        public async Task<int> ExecuteAsync(InvocationContext ctx)
        {
            var idsFiles = ctx.ParseResult.GetValueForOption(CliOptions.IdsFilesOption);
            var modelFiles = ctx.ParseResult.GetValueForOption(CliOptions.ModelFilesOption);
            var verbosity = ctx.ParseResult.GetValueForOption(CliOptions.VerbosityOption);
            var specNameFilter = ctx.ParseResult.GetValueForOption(CliOptions.IdsFilterOption);
            
            return await Execute(idsFiles, modelFiles, verbosity, specNameFilter);
        }


        private async Task<int> Execute(string[] idsFiles, string[] modelFiles, Verbosity verbosity, string specNameFilter)
        {
            var failedSpecs = 0;
            console = new ConsoleLogger(verbosity);

            foreach (var modelFile in modelFiles)
            {
                IModel? model = default;
                try
                {


                    console.WriteImportantLine("IFC File: {0}", modelFile);
                    console.WriteInfoLine("Loading Model...");
                    var sw = new Stopwatch();
                    sw.Start();
#if SQLite

                IModel model = BuildModelSqlLite(modelFile);
                var ifcFlexDb = model as IfcFlexDb;
                using var tc = ifcFlexDb!.BeginTypeCaching();
                using var inverseCache = ifcFlexDb.BeginInverseCaching();
                using var entityeCache = ifcFlexDb.BeginEntityCaching();
                OptimiseActivationStrategy(ifcFlexDb);
#else
                    model = BuildModel(modelFile);
                    using var ic = model.BeginInverseCaching();
                    using var ec = model.BeginEntityCaching();
#endif
                    console.WriteInfoLine("Model loaded in {0}s", sw.Elapsed.TotalSeconds);
                    // Normally we'd inject rather than service discovery

                    foreach (var ids in idsFiles)
                    {
                        console.WriteImportantLine("IDS File: {0}", ids);
                        console.WriteInfoLine("Validating...");
                        var options = new VerificationOptions
                        {
                            IncludeSubtypes = false,
                            OutputFullEntity = true,
                            AllowDerivedAttributes = true,
                            PerformInPlaceSchemaUpgrade = true,
                            PermittedIdsAuditStatuses = VerificationOptions.AnyState,
                            SkipIncompatibleSpecification = true,
                            SpecExecutionFilter = (s => string.IsNullOrEmpty(specNameFilter) || s?.Name?.Contains(specNameFilter, StringComparison.InvariantCultureIgnoreCase) != false),
                            RuntimeTokens = config.Detokenise ? config.Tokens : new(),
                        };

                        var results = await idsValidator.ValidateAgainstIdsAsync(model, ids, logger, OutputRequirement, options);

                        sw.Stop();
                        failedSpecs += results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Fail);

                        WriteSummary(results, sw, Path.GetFileName(modelFile), Path.GetFileName(ids));

                        if (results.Status == ValidationStatus.Error)
                        {
                            console.WriteImportantLine($"Validation failed to run: {results.Message}");
                            return -1;
                        }
                    }

                }
                finally
                {
                    model?.Dispose();
                }
            }
            return failedSpecs;
        }

        


        private void WriteSummary(ValidationOutcome results, Stopwatch sw, string ifcFile, string idsFile)
        {
            console?
                .WriteInfo(Blue, "IDS Validation summary\n")
                .WriteInfo(Gray, $"IDS:   ")
                .WriteInfoLine(White, idsFile)
                .WriteInfo(Gray, "Model: ")
                .WriteInfoLine(White, ifcFile)
                .WriteInfoLine(Gray, "---------------------------------------------------------------------------")
                ;
            var totalRun = results.ExecutedRequirements.Count;
            var totalPass = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Pass);
            var totalInconclusive = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Inconclusive);
            var totalSkipped = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Skipped);
            var totalFail = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Fail);
            var totalError = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Error);

            var totalElementsTested = results.ExecutedRequirements.Sum(r => r.ApplicableResults.Count(r => r.ValidationStatus != ValidationStatus.Error));
            var totalPassedResults = results.ExecutedRequirements.Sum(r => r.PassedResults.Count());
            var totalFailedResults = results.ExecutedRequirements.Sum(r => r.FailedResults.Count());
            var totalPercent = totalElementsTested > 0 ? ((float)totalPassedResults) / totalElementsTested * 100 : 0;
            console?.WriteInfoLine(White, $"Detailed Results:");
            console?.WriteInfoLine(Gray, " no     Pass /Total  %age   #Fail  Specification");
            console?.WriteInfoLine(Gray, "----- -------------- ------ -----  ---------------------------------------------");
            int i = 1;

            foreach (var req in results.ExecutedRequirements)
            {
                int? passed = req.PassedResults.Count();
                passed = passed == 0 ? null : passed;
                int? failed = req.FailedResults.Count();
                failed = failed == 0 ? null : failed;
                var total = req.ApplicableResults.Count(r => r.ValidationStatus != ValidationStatus.Error);
                float? percent = req.ApplicableResults.Count() > 0 ? ((float)(passed ?? 0)) / req.ApplicableResults.Count() * 100 : 0;
                if (req.Status == ValidationStatus.Pass && req.ApplicableResults.Count() == 0)
                {
                    percent = null;
                }
                console?.WriteInfo(White, $"{i++,3}");
                console?.WriteInfo(console.GetColorForStatus(req.Status), $"{StatusIcon(req.Status)}");
                console?.WriteInfo(White, $" [{passed,5}")
                    .WriteInfo(Gray, $" /")
                    .WriteInfo(White, $"{total,5}]")
                //.WriteInfo(Gray, $" passed ")
                //   .WriteInfo(Gray, $" failed ")
                    .WriteInfo(DarkYellow, $" {percent,5:0.0}% ")
                    .WriteInfo(Red, $"{failed,5}")
                    .WriteInfo(Gray, $"  {req.Specification.Name}\n", Gray);
                // [{passed} passed from {req.ApplicableResults.Count}]", 
            }
            console?.WriteInfoLine(Gray, "----- -------------- ------ -----  ---------------------------------------------");
            console?
                .WriteInfo(console.GetColorForStatus(results.Status), "   " + StatusIcon(results.Status))
                .WriteInfo(White, $" [{totalPassedResults,5}")
                .WriteInfo(Gray, $" /")
                .WriteInfo(White, $"{totalElementsTested,5}]")
                .WriteInfo(DarkYellow, $" {totalPercent,5:0.0}% ")
                //.WriteInfo(Gray, $" tested ")
                .WriteInfo(Red, $"{totalFailedResults,5}  ")
                //.WriteInfo(Gray, $" failed ")
                ;

            console?.WriteImportant(White, $"Specifications Run: {totalRun} ")
            .WriteImportant(Green, $"Pass: {totalPass} ")
            .WriteImportant(Red, $"Fail: {totalFail} ")
            .WriteImportant(Cyan, $"Not Run: {totalSkipped} ")
            .WriteImportant(Yellow, $"Incomplete: {totalInconclusive} ")
            .WriteImportant(DarkRed, $"Error: {totalError}")
            .WriteImportant(DarkGreen, $" in {sw.Elapsed.TotalSeconds} secs\n");
            console?.WriteInfo(White, "==========================================================================\n");
        }

        private Task OutputRequirement(ValidationRequirement req)
        {
            var passed = req.PassedResults.Count();

            console?.WriteInfo(console.GetColorForStatus(req.Status), req.Status.ToString())
                .WriteInfo(Gray, $" : {req.Specification.Name} [{passed} passed from {req.ApplicableResults.Count}]")
                .WriteInfo(Cyan, $" {req.Specification.Cardinality.Description} Requirement\n");
            console?.WriteDetailLine(Blue, $"  🔎  For {req.Specification.Applicability.GetApplicabilityDescription().SplitClauses()}\n");
            if (req.Specification.Cardinality.AllowsRequirements)
                console?.WriteDetailLine(DarkGreen, $"  📏  It is {req.Specification.Cardinality.Description} that elements {req.Specification.Requirement?.GetRequirementDescription().SplitClauses()}\n");

            
            foreach (var itm in req.ApplicableResults)
            {
                if (req.Status == ValidationStatus.Error)
                {
                    console?.WriteImportantLine(console.GetColorForStatus(itm.ValidationStatus), "  " + StatusIcon(itm.ValidationStatus));
                    foreach (var msg in itm.Messages.Where(m => m.Status != ValidationStatus.Pass))
                    {
                        console?.WriteImportant(DarkRed, $": {msg?.Reason}\n")
                        .WriteImportant(DarkGray, $"     {msg}\n");
                    }

                }
                else if (req.IsFailure(itm))
                {
                    console?.WriteInfo(console.GetColorForStatus(itm.ValidationStatus), "  " + StatusIcon(itm.ValidationStatus))
                        .WriteInfo(Red, $"{itm.Requirement?.Name} {itm.Requirement?.Description}")
                        .WriteInfo(Gray, $"{itm.FullEntity}\n");
                    foreach (var msg in itm.Messages.Where(m => m.Status != ValidationStatus.Pass))
                    {
                        var msgtxt = msg.ToString()
                        .Replace("{", "{{")
                            .Replace("}", "}}");
                        console?.WriteInfo(DarkRed, $"     {msgtxt}\n");
                    }
                }
                else
                {
                    console?.WriteDetail(console.GetColorForStatus(itm.ValidationStatus), "  " + StatusIcon(itm.ValidationStatus))
                        .WriteDetail(DarkGray, $"{itm.Requirement?.Name} {itm.Requirement?.Description}")
                        .WriteDetail(Gray, $"{itm.FullEntity}\n");
                    foreach (var msg in itm.Messages.Where(m => m.Status == ValidationStatus.Pass))
                    {
                        console?.WriteTrace(DarkGray, $"     {msg}\n");
                    }
                }
                //Console.Write(".");
            }
            console?.WriteInfoLine("");
            return Task.CompletedTask;
        }

        private static string StatusIcon(ValidationStatus status)
        {
            return status switch
            {
                ValidationStatus.Pass => "✔️",
                ValidationStatus.Inconclusive => "❓",
                ValidationStatus.Fail => "❌",
                ValidationStatus.Error => "⚠️",
                ValidationStatus.Skipped => "💤",
                _ => throw new NotImplementedException(),
            };
        }


        private static IModel BuildModel(string modelFile)
        {
            if (modelFile.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return OpenCOBie(modelFile);
            }
#if SQLite

        if (modelFile.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            // return Flex DB
            var flexdb = new IfcFlexDb();
            flexdb.Open(modelFile);
            flexdb.BeginTypeCaching();

            flexdb.AddActivationDepth<IIfcRelDefinesByProperties>(2);
            flexdb.AddActivationDepth<IIfcRelDefinesByType>(2);
            flexdb.AddActivationDepth<IIfcRelAssociatesClassification>(2);
            flexdb.AddActivationDepth<IIfcRelAggregates>(2);

            return flexdb;
        }
#endif

            return MemoryModel.OpenRead(modelFile);
        }

        private static IModel OpenCOBie(string file)
        {
            var mapping = Xbim.IO.CobieExpress.CobieModel.GetMapping();
            mapping.ClassMappings.RemoveAll(m => m.Class == "System");
            mapping.ClassMappings.RemoveAll(m => m.Class.StartsWith("Attribute"));
            mapping.ClassMappings.RemoveAll(m => m.Class.StartsWith("Zone"));
            //foreach(var map in mapping.ClassMappings)
            //{
            //    Console.WriteLine(map.Class);
            //}
            var model = Xbim.IO.CobieExpress.CobieModel.ImportFromTable(file, out string report, mapping);

            return model;
        }

        


#if SQLite
    private static IModel BuildModelSqlLite(string ifcFile)
    {
        if(!File.Exists(ifcFile))
        {
            throw new FileNotFoundException(ifcFile);
        }
        using (var ifcStream = File.Open(ifcFile, FileMode.Open))
        {
            var file = Path.ChangeExtension(ifcFile, "db");
            var flexDb = new IfcFlexDb(file);

            flexDb.Open(file);
            flexDb.ImportStep21(ifcStream);

            return flexDb;
        }
    }

    private static void OptimiseActivationStrategy(IfcFlexDb model)
    {
        model.AddActivationDepth<IIfcRelDefinesByProperties>(2);
        
        model.AddActivationDepth<IIfcRelDefinesByType>(2);
        model.AddActivationDepth<IIfcRelAssociatesClassification>(2);
        model.AddActivationDepth<IIfcRelAggregates>(2);
    }
#endif

    }
}
