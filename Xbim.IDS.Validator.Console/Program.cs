
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Xbim.Common;
using Xbim.Common.Configuration;
#if SQLite
using Xbim.Flex.IO.Db.FlexDb;
#endif
using Xbim.IDS.Validator.Common;
using Xbim.IDS.Validator.Console;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Configuration;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.InformationSpecifications;
using Xbim.IO.Memory;

class Program
{

    static ILogger? logger;

    static readonly ServiceProvider provider;

    static Program()
    {
        provider = BuildServiceProvider();
    }

    static async Task<int> Main(string[] args)
    {
        var command = SetupParams();
        logger = provider.GetRequiredService<ILogger<Program>>();
        var result = await command.InvokeAsync(args);

        //Console.ReadLine();

        return result;
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(o => o
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning)
            );

        serviceCollection.AddIdsValidation(cfg => cfg.AddCOBie());
        serviceCollection.AddXbimToolkit(opt => opt.AddMemoryModel());

        var provider = serviceCollection.BuildServiceProvider();
        return provider;
    }

    private static RootCommand SetupParams()
    {
        var idsOption = new Option<string>
            (aliases: new[] { "--idsFile", "-ids" },
            description: "Path to an IDS file");

        var modelOption = new Option<string>
            (aliases: new[] { "--modelFile", "-m" },
            description: "Path to a model file in IFC or (experimentally) COBie xls format");

        idsOption.Arity = ArgumentArity.ExactlyOne;
        idsOption.IsRequired = true;
        modelOption.Arity = ArgumentArity.ExactlyOne;
        modelOption.IsRequired = true;

        var rootCommand = new RootCommand
        {
            idsOption,
            modelOption
        };

        rootCommand.Description = "IFC and COBie Model checker using BuildingSMART IDS";

        rootCommand.SetHandler(async (ids, model) =>
        {
            await RunIDSValidation(ids, model);

        }, idsOption, modelOption);
        return rootCommand;
    }

    private static async Task RunIDSValidation(string ids, string modelFile)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("IDS File: {0}", ids);
        Console.WriteLine("IFC File: {0}", modelFile);
        Console.WriteLine("Loading Model...");
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
        IModel model = BuildModel(modelFile);
        using var ic = model.BeginInverseCaching();
        using var ec = model.BeginEntityCaching();
#endif
        Console.WriteLine("Model loaded in {0}s", sw.Elapsed.TotalSeconds);
        // Normally we'd inject rather than service discovery
        var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

        Console.WriteLine("Validating...");
        var options = new VerificationOptions { IncludeSubtypes = true, OutputFullEntity = true, AllowDerivedAttributes = true, PerformInPlaceSchemaUpgrade=true };
        var results = await idsValidator.ValidateAgainstIdsAsync(model, ids, logger, OutputRequirement, options);

        sw.Stop();
        WriteSummary(results, sw, Path.GetFileName(modelFile), Path.GetFileName(ids));

        if (results.Status == ValidationStatus.Error)
        {
            Console.Error.WriteLine($"Validation failed to run: {results.Message}");
            return;
        }


        model.Dispose();
    }

    private static void WriteSummary(ValidationOutcome results, Stopwatch sw, string ifcFile, string idsFile)
    {
        WriteColored("IDS Validation summary\n", ConsoleColor.Blue);
        WriteColored($"IDS:   ", ConsoleColor.Gray);
        WriteColored(idsFile, ConsoleColor.White);
        WriteColored($"\nModel: ", ConsoleColor.Gray);
        WriteColored(ifcFile, ConsoleColor.White);
        WriteColored("\n---------------------------------------------------------------------------\n", ConsoleColor.Gray);
        var totalRun = results.ExecutedRequirements.Count;
        var totalPass = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Pass);
        var totalInconclusive = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Inconclusive);
        var totalFail = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Fail);
        var totalError = results.ExecutedRequirements.Count(r => r.Status == ValidationStatus.Error);

        var totalElementsTested = results.ExecutedRequirements.Sum(r => r.ApplicableResults.Count());
        var totalPassedResults = results.ExecutedRequirements.Sum(r => r.PassedResults.Count());
        var totalFailedResults = results.ExecutedRequirements.Sum(r => r.FailedResults.Count());
        var totalPercent = totalElementsTested > 0 ? ((float)totalPassedResults) / totalElementsTested * 100 : 0;


        foreach (var req in results.ExecutedRequirements)
        {
            var passed = req.PassedResults.Count();
            var failed = req.FailedResults.Count();
            var percent = req.ApplicableResults.Count() > 0 ? ((float)passed) / req.ApplicableResults.Count() * 100 : 0;
            WriteColored(req.Status, StatusIcon(req.Status));
            WriteColored($" {passed,5}", ConsoleColor.White);
            WriteColored($" /", ConsoleColor.Gray);
            WriteColored($"{req.ApplicableResults.Count,5}", ConsoleColor.White);
            WriteColored($" passed ", ConsoleColor.Gray);
            WriteColored($"{failed,5}", ConsoleColor.Red);
            WriteColored($" failed ", ConsoleColor.Gray);
            WriteColored($"{percent,5:0.0}% ", ConsoleColor.DarkYellow);
            WriteColored($": {req.Specification.Name}\n", ConsoleColor.Gray);
            // [{passed} passed from {req.ApplicableResults.Count}]", 
        }
        WriteColored("==========================================================================\n", ConsoleColor.White);
        WriteColored(results.Status, results.Status.ToString());
        WriteColored($" {totalPassedResults,5}", ConsoleColor.White);
        WriteColored($" /", ConsoleColor.Gray);
        WriteColored($"{totalElementsTested,5}", ConsoleColor.White);
        WriteColored($" tested ", ConsoleColor.Gray);
        WriteColored($"{totalFailedResults,5}", ConsoleColor.Red);
        WriteColored($" failed ", ConsoleColor.Gray);
        WriteColored($"{totalPercent,5:0.0}% ", ConsoleColor.DarkYellow);
        WriteColored($" in {sw.Elapsed.TotalSeconds} secs\n\n", ConsoleColor.DarkGreen);

        WriteColored($"Specifications Tested: {totalRun} ", ConsoleColor.White);
        WriteColored($"Pass: {totalPass} ", ConsoleColor.Green);
        WriteColored($"Fail: {totalFail} ", ConsoleColor.Red);
        WriteColored($"Incomplete: {totalInconclusive} ", ConsoleColor.Yellow);
        WriteColored($"Error: {totalError} \n\n", ConsoleColor.DarkRed);

    }

    private static Task OutputRequirement(ValidationRequirement req)
    {
        var passed = req.PassedResults.Count();

        WriteColored(req.Status, req.Status.ToString());
        WriteColored($" : {req.Specification.Name} [{passed} passed from {req.ApplicableResults.Count}]", ConsoleColor.Gray);
        WriteColored($" {req.Specification.Cardinality.Description} Requirement\n", ConsoleColor.Cyan);
        WriteColored($"  🔎  For {req.Specification.Applicability.GetApplicabilityDescription().SplitClauses()}\n", ConsoleColor.Blue);
        if(req.Specification.Cardinality.AllowsRequirements)
            WriteColored($"  📏  It is {req.Specification.Cardinality.Description} that elements {req.Specification.Requirement?.GetRequirementDescription().SplitClauses()}\n", ConsoleColor.DarkGreen);

        Console.ForegroundColor = ConsoleColor.White;
        foreach (var itm in req.ApplicableResults)
        {
            if (req.Status == ValidationStatus.Error)
            {
                WriteColored(itm.ValidationStatus, "  " + StatusIcon(itm.ValidationStatus));
                foreach (var msg in itm.Messages.Where(m => m.Status != ValidationStatus.Pass))
                {
                    WriteColored(ValidationStatus.Error, $": {msg?.Reason}\n");
                    WriteColored($"     {msg}\n", ConsoleColor.DarkGray);
                }

            }
            else if (req.IsFailure(itm))
            {
                WriteColored(itm.ValidationStatus, "  " + StatusIcon(itm.ValidationStatus));
                WriteColored($"{itm.Requirement?.Name} {itm.Requirement?.Description}", ConsoleColor.Red);
                WriteColored($"{itm.FullEntity}\n", ConsoleColor.Gray);
                foreach (var msg in itm.Messages.Where(m => m.Status != ValidationStatus.Pass))
                {
                    WriteColored($"     {msg}\n", ConsoleColor.DarkRed);
                }
            }
            else
            {
                WriteColored(itm.ValidationStatus, "  " + StatusIcon(itm.ValidationStatus));
                WriteColored($"{itm.Requirement?.Name} {itm.Requirement?.Description}", ConsoleColor.DarkGray);
                WriteColored($"{itm.FullEntity}\n", ConsoleColor.Gray);
                foreach (var msg in itm.Messages.Where(m => m.Status == ValidationStatus.Pass))
                {
                    WriteColored($"     {msg}\n", ConsoleColor.DarkGray);
                }
            }
            //Console.Write(".");
        }
        Console.WriteLine();
        Console.WriteLine("------------------------------");
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
            _ => throw new NotImplementedException(),
        };
    }

    private static void WriteColored(ValidationStatus status, string text)
    {

        switch (status)
        {
            case ValidationStatus.Pass:
                WriteColored(text, ConsoleColor.Green);
                break;
            case ValidationStatus.Inconclusive:
                WriteColored(text, ConsoleColor.Yellow);
                break;
            case ValidationStatus.Fail:
            default:
                WriteColored(text, ConsoleColor.Red);
                break;

        }

    }
    private static void WriteColored(string text, ConsoleColor color)
    {

        var originalColor = Console.ForegroundColor;
        try
        {

            Console.ForegroundColor = color;
            Console.Write(text);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
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