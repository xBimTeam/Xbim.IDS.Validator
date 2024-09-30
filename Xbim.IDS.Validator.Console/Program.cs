using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using Xbim.Common.Configuration;
using Xbim.IDS.Validator.Console;
using Xbim.IDS.Validator.Core;
using Xbim.IDS.Validator.Core.Configuration;

partial class Program
{

    static ILogger? logger;


    static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        logger = host.Services.GetRequiredService<ILogger<Program>>();
        var rootCommand = CliOptions.SetupCommands(host.Services);

        // Handle the supplied arguments
        var result = await rootCommand.InvokeAsync(args);

        //Console.ReadLine();

        return result;
    }

    [RequiresUnreferencedCode("Calls Microsoft.Extensions.DependencyInjection.OptionsBuilderConfigurationExtensions.Bind<TOptions>(IConfiguration)")]
    public static HostApplicationBuilder CreateHostBuilder(string[] args)
    {
        var host = Host.CreateApplicationBuilder(args);

        host.Services
            .AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .AddIdsValidation(cfg => cfg.AddCOBie())
            .AddXbimToolkit(opt => opt.AddMemoryModel())
            .AddTransient<VerifyIfcCommand>()
            .AddTransient<IdsAuditCommand>()
            .AddTransient<IdsMigratorCommand>()

            .AddOptions<IdsConfig>()
            .Bind(host.Configuration.GetSection(IdsConfig.SectionName));

             // register your services here.

        return host;

    }
}