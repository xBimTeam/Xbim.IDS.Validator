
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Xml;
using Xbim.Common;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;

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

            var passed = req.ApplicableResults.Count( a=> a.ValidationStatus == ValidationStatus.Success );
            WriteColored(req.Status, req.Status.ToString());
            WriteColored($" : {req.Specification.Name} [{passed}/{req.ApplicableResults.Count}]", ConsoleColor.Gray);
            WriteColored($" {req.Specification.Cardinality.Description} Requirement\n", ConsoleColor.Cyan);
            WriteColored($"  --Where {req.Specification.Applicability.Short()}\n", ConsoleColor.Blue);
            WriteColored($"  --Check {req.Specification.Requirement?.Short()}\n",ConsoleColor.Gray);

            Console.ForegroundColor = ConsoleColor.White;
            foreach (var itm in req.ApplicableResults)
            {
                if((req.Specification.Cardinality.ExpectsRequirements && itm.ValidationStatus != ValidationStatus.Success) ||
                    (req.Specification.Cardinality.NoMatchingEntities && itm.ValidationStatus != ValidationStatus.Failed))
                {
                    WriteColored(itm.ValidationStatus, "    " + itm.ValidationStatus.ToString());
                    WriteColored($":{itm.Requirement?.Name} - {itm.Requirement?.Description}", ConsoleColor.Red);
                    WriteColored($": {itm.Entity}\n", ConsoleColor.White);
                    foreach(var msg in itm.Messages.Where(m=> m.Status != ValidationStatus.Success))
                    {
                        WriteColored($"                [{msg?.Clause?.GetType().Name}.{msg?.ValidatedField}] {msg?.Expectation} to match {msg?.ExpectedResult} - but actually found '{msg?.ActualResult}'\n", ConsoleColor.DarkGray);
                    }
                }
                //Console.Write(".");
            }
            Console.WriteLine();
            Console.WriteLine("------------------------------");
        }

    }

    private static void WriteColored(ValidationStatus status, string text)
    {
        
        switch(status)
        {
            case ValidationStatus.Success:
                WriteColored(text,ConsoleColor.Green);
                break;
            case ValidationStatus.Inconclusive:
                WriteColored(text, ConsoleColor.Yellow);
                break;
            case ValidationStatus.Failed:
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