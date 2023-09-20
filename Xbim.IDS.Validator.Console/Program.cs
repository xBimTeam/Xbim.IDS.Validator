
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Diagnostics;
using Xbim.Common;
using Xbim.Common.Model;
using Xbim.Flex.IO.Db.FlexDb;
using Xbim.Flex.IO.Db.Interfaces;
using Xbim.IDS.Validator.Common;
using Xbim.IDS.Validator.Console;
using Xbim.IDS.Validator.Core;
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
        return await command.InvokeAsync(args);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(/*o => o.AddConsole()*/);

        serviceCollection.AddIdsValidation();

        var provider = serviceCollection.BuildServiceProvider();
        return provider;
    }

    private static RootCommand SetupParams()
    {
        var idsOption = new Option<string>
            (aliases: new[] { "--idsFile", "-ids" },
            description: "Path to an IDS file");

        var modelOption = new Option<string>
            (aliases: new[] { "--modelFile", "-ifc" },
            description: "Path to an IFC file");

        idsOption.Arity = ArgumentArity.ExactlyOne;
        idsOption.IsRequired= true; 
        modelOption.Arity = ArgumentArity.ExactlyOne;
        modelOption.IsRequired= true;

        var rootCommand = new RootCommand
        {
            idsOption,
            modelOption
        };



        rootCommand.SetHandler(async (ids, model) =>
        {
            await RunIDSValidation(ids, model);

        }, idsOption, modelOption);
        return rootCommand;
    }

    private static async Task RunIDSValidation(string ids, string modelFile)
    {
        Console.WriteLine("IDS File: {0}", ids);
        Console.WriteLine("IFC File: {0}", modelFile);
        Console.WriteLine("Loading Model..."); 
        IModel model = BuildModel(modelFile);
        
        using var ic = model.BeginInverseCaching();
        using var ec = model.BeginEntityCaching();
        
        // Normally we'd inject rather than service discovery
        var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

        var w = Stopwatch.StartNew();
        Console.WriteLine("Validating...");
        var options = new VerificationOptions { IncludeSubtypes = true };
        var results = await idsValidator.ValidateAgainstIdsAsync(model, ids, logger, OutputRequirement, options);
        if(results.Status == ValidationStatus.Error)
        {
            Console.Error.WriteLine($"Validation failed to run: {results.Message}");
            return;
        }
        w.Stop();
        Console.WriteLine($"Validation executed in {w.Elapsed.TotalSeconds} seconds.");
    }

    private static void OutputRequirement(ValidationRequirement req)
    {
        var passed = req.PassedResults.Count();

        WriteColored(req.Status, req.Status.ToString());
        WriteColored($" : {req.Specification.Name} [{passed} passed from {req.ApplicableResults.Count}]", ConsoleColor.Gray);
        WriteColored($" {req.Specification.Cardinality.Description} Requirement\n", ConsoleColor.Cyan);
        WriteColored($"  -- For {req.Specification.Applicability.GetApplicabilityDescription().SplitClauses()}\n", ConsoleColor.Blue);
        WriteColored($"  -- It is {req.Specification.Cardinality.Description} that elements {req.Specification.Requirement?.GetRequirementDescription().SplitClauses()}\n", ConsoleColor.DarkGreen);

        Console.ForegroundColor = ConsoleColor.White;
        foreach (var itm in req.ApplicableResults)
        {
            if(req.Status == ValidationStatus.Error)
            {
                WriteColored(itm.ValidationStatus, "    " + itm.ValidationStatus.ToString());
                foreach(var msg in itm.Messages.Where(m => m.Status != ValidationStatus.Pass))
                {
                    WriteColored(ValidationStatus.Error, $": {msg?.Reason}\n");
                    WriteColored($"               {msg?.Expectation} {msg?.Clause?.GetType().Name}.{msg?.ValidatedField} to be {msg?.ExpectedResult} - but actually found '{msg?.ActualResult}'\n", ConsoleColor.DarkGray);
                }

            }
            else if(req.IsFailure(itm))
            {
                WriteColored(itm.ValidationStatus, "    " + itm.ValidationStatus.ToString());
                WriteColored($":{itm.Requirement?.Name} - {itm.Requirement?.Description}", ConsoleColor.Red);
                WriteColored($": {itm.Entity}\n", ConsoleColor.White);
                foreach (var msg in itm.Messages.Where(m => m.Status != ValidationStatus.Pass))
                {
                    WriteColored($"               {msg?.Expectation} {msg?.Clause?.GetType().Name}.{msg?.ValidatedField} to be {msg?.ExpectedResult} - but actually found '{msg?.ActualResult}' because {msg?.Reason}\n", ConsoleColor.DarkGray);
                }
            }
            //else
            //{
            //    WriteColored($":{itm.Requirement?.Name} - {itm.Requirement?.Description}", ConsoleColor.DarkGray);
            //    WriteColored($": {itm.Entity}\n", ConsoleColor.White);
            //    foreach (var msg in itm.Messages.Where(m => m.Status == ValidationStatus.Pass))
            //    {
            //        WriteColored($"                [{msg?.Clause?.GetType().Name}.{msg?.ValidatedField}] {msg?.Expectation} to find {msg?.ExpectedResult} - and found '{msg?.ActualResult}'\n", ConsoleColor.Gray);
            //    }
            //}
            Console.Write(".");
        }
        Console.WriteLine();
        Console.WriteLine("------------------------------");
    }

    private static void WriteColored(ValidationStatus status, string text)
    {
        
        switch(status)
        {
            case ValidationStatus.Pass:
                WriteColored(text,ConsoleColor.Green);
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
    private static IModel BuildModel(string ifcFile)
    {
        if (ifcFile.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            // return Flex DB
            var flexdb = new IfcFlexDb();
            flexdb.Open(ifcFile);
            flexdb.BeginTypeCaching();

            flexdb.AddActivationDepth<IIfcRelDefinesByProperties>(2);
            flexdb.AddActivationDepth<IIfcRelDefinesByType>(2);
            flexdb.AddActivationDepth<IIfcRelAssociatesClassification>(2);
            flexdb.AddActivationDepth<IIfcRelAggregates>(2);

            return flexdb;
        }

        return MemoryModel.OpenRead(ifcFile);
    }

}