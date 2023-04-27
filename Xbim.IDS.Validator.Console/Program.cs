
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using Xbim.Common;
using Xbim.IDS.Validator.Console;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xbim.InformationSpecifications;

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

        var rootCommand = new RootCommand();
        rootCommand.Add(idsOption);
        rootCommand.Add(modelOption);


     
        rootCommand.SetHandler((ids, model) =>
        {
            RunIDSValidation(ids, model);

        }, idsOption, modelOption);
        return rootCommand;
    }

    private static void RunIDSValidation(string ids, string modelFile)
    {
        Console.WriteLine("IDS File: {0}", ids);
        Console.WriteLine("IFC File: {0}", modelFile);
        Console.WriteLine("Loading Model..."); 
        IModel model = BuildModel(modelFile);

        // Normally we'd inject rather than service discovery
        var idsValidator = provider.GetRequiredService<IIdsModelValidator>();

        Console.WriteLine("Validating...");
        var results = idsValidator.ValidateAgainstIds(model, ids, logger);

        foreach (var req in results.ExecutedRequirements)
        {

            var passed = req.Specification.Cardinality.NoMatchingEntities ? req.ApplicableResults.Count(a => a.ValidationStatus == ValidationStatus.Fail) 
                :  req.ApplicableResults.Count( a=> a.ValidationStatus == ValidationStatus.Pass );
            
            WriteColored(req.Status, req.Status.ToString());
            WriteColored($" : {req.Specification.Name} [{passed} passed from {req.ApplicableResults.Count}]", ConsoleColor.Gray);
            WriteColored($" {req.Specification.Cardinality.Description} Requirement\n", ConsoleColor.Cyan);
            WriteColored($"  -- For {req.Specification.Applicability.GetApplicabilityDescription().SplitClauses()}\n", ConsoleColor.Blue);
            WriteColored($"  -- It is {req.Specification.Cardinality.Description} that elements {req.Specification.Requirement?.GetRequirementDescription().SplitClauses()}\n",ConsoleColor.DarkGreen);

            Console.ForegroundColor = ConsoleColor.White;
            foreach (var itm in req.ApplicableResults)
            {
                if((req.Specification.Cardinality.ExpectsRequirements && itm.ValidationStatus != ValidationStatus.Pass) ||
                    (req.Specification.Cardinality.NoMatchingEntities && itm.ValidationStatus != ValidationStatus.Fail))
                {
                    WriteColored(itm.ValidationStatus, "    " + itm.ValidationStatus.ToString());
                    WriteColored($":{itm.Requirement?.Name} - {itm.Requirement?.Description}", ConsoleColor.Red);
                    WriteColored($": {itm.Entity}\n", ConsoleColor.White);
                    foreach(var msg in itm.Messages.Where(m=> m.Status != ValidationStatus.Pass))
                    {
                        WriteColored($"               {msg?.Expectation} {msg?.Clause?.GetType().Name}.{msg?.ValidatedField} to be {msg?.ExpectedResult} - but actually found '{msg?.ActualResult}'\n", ConsoleColor.DarkGray);
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
        return IfcStore.Open(ifcFile);
    }

}