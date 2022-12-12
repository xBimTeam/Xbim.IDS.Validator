
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using Xbim.Common;
using Xbim.IDS.Validator.Core;
using Xbim.Ifc;
using Xbim.InformationSpecifications;

class Program
{

    static ILogger? logger;
    static async Task<int> Main(string[] args)
    {

        var factory = LoggerFactory.Create(builder =>
        {
            //builder.AddConsole();
        });
        logger = factory.CreateLogger<Program>();
        var command = SetupParams();

        return await command.InvokeAsync(args);
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
        var modelBinder = new IdsModelBinder(model);
        
        var idsValidator = new IdsModelValidator(modelBinder);

        Console.WriteLine("Validating...");
        var results = idsValidator.ValidateAgainstIds(ids, logger);

        foreach(var req in results.ExecutedRequirements)
        {

            var passed = req.ApplicableResults.Count( a=> a.ValidationStatus == ValidationStatus.Success );
            WriteColored(req.Status, req.Status.ToString());
            Console.WriteLine($" : {req.Specification.Name} [{passed}/{req.ApplicableResults.Count}]");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"  --Where {req.Specification.Applicability.Short()}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  --Check {req.Specification.Cardinality.Description.ToUpper()} {req.Specification.Requirement?.Short()}");

            foreach (var itm in req.ApplicableResults)
            {
                Console.ForegroundColor= ConsoleColor.Red;
                if(itm.ValidationStatus != ValidationStatus.Success)
                {
                    WriteColored(req.Status, "    " + req.Status.ToString());
                    Console.WriteLine($":{itm.Requirement?.Name} - {itm.Requirement?.Description} : {itm.Entity} ");
                }
                Console.ForegroundColor = ConsoleColor.White;
                //Console.Write(".");
            }
            Console.WriteLine();
            Console.WriteLine("------------------------------");
        }

    }

    private static void WriteColored(ValidationStatus status, string text)
    {
        var color = Console.ForegroundColor; ;
        switch(status)
        {
            case ValidationStatus.Success:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case ValidationStatus.Inconclusive:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case ValidationStatus.Failed:
                Console.ForegroundColor = ConsoleColor.Red;
                break;

        }
        Console.Write(text);
        Console.ForegroundColor = color;
    }

    private static IModel BuildModel(string ifcFile)
    {
        return IfcStore.Open(ifcFile);
    }
}