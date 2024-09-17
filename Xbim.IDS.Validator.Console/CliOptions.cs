using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace Xbim.IDS.Validator.Console
{
    /// <summary>
    /// Defines the CLI options based on System.Commandline conventions
    /// </summary>
    public class CliOptions
    {
        public static readonly Option<string[]> IdsFilesOption = new Option<string[]>
            (aliases: new[] { "--idsFiles", "-ids" },
            description: "Path to one or more IDS files (*.ids|*.xml)")
            {
                Arity = ArgumentArity.OneOrMore,
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };

        public static readonly Option<string[]> ModelFilesOption = new Option<string[]>
            (aliases: new[] { "--modelFiles", "-m" },
            description: "Path to one or model files in IFC-SPF format [or experimentally COBie in xls format] (*.ifc|*.ifczip|*.ifcxml)")
            {
                Arity = ArgumentArity.OneOrMore,
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };

        public static readonly Option<Verbosity> VerbosityOption = new Option<Verbosity>
            (aliases: new[] { "--vebosity", "-v" },
            description: "Verbosity of the output")
            {
                Arity = ArgumentArity.ZeroOrOne,
                IsRequired = false,
            };

        public static readonly Option<string> IdsFilterOption = new Option<string>
            (aliases: new[] { "--filter", "-f" },
            description: "Only IDS specs matching this filter")
            {
                Arity = ArgumentArity.ZeroOrOne,
                IsRequired = false,
            };


        public static RootCommand SetupCommands(IServiceProvider provider)
        {
            var rootCommand = new RootCommand()
            {
                Description = "Tools for verifying information deliverables in BIM models using IDS",
            };

            VerbosityOption.SetDefaultValue(Verbosity.Detailed);
           
            var modelVerifyCommand = new Command("verify", "Verify IFC and COBie models using BuildingSMART IDS")
            {
                IdsFilesOption,
                ModelFilesOption,
                VerbosityOption,
                IdsFilterOption
            };

            modelVerifyCommand.SetHandler(async (ctx) =>
            {
                var command = provider.GetRequiredService<VerifyIfcCommand>();
                var result = await command.ExecuteAsync(ctx);
                ctx.ExitCode = result;
            });

            var idsAuditCommand = new Command("audit", "Check IDS files adhere to the latest standards")
            {
                IdsFilesOption,
                VerbosityOption
            };

            idsAuditCommand.SetHandler(async (ctx) =>
            {
                var command = provider.GetRequiredService<IdsAuditCommand>();
                var result = await command.ExecuteAsync(ctx);
                ctx.ExitCode = result;
            });

            var idsMigrateCommand = new Command("migrate", "Migrate IDS files from older schemas to IDS1.0")
            {
                IdsFilesOption,
                VerbosityOption
            };

            idsMigrateCommand.SetHandler(async (ctx) =>
            {
                var command = provider.GetRequiredService<IdsMigratorCommand>();
                var result = await command.ExecuteAsync(ctx);
                ctx.ExitCode = result;
            });


            rootCommand.Add(modelVerifyCommand);
            rootCommand.Add(idsAuditCommand);
            rootCommand.Add(idsMigrateCommand);

            return rootCommand;
        }
    }
}
