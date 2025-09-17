using System.CommandLine;
using Xbim.IDS.Validator.Console.Commands;
using Xbim.IDS.Validator.Console.Internal;

namespace Xbim.IDS.Validator.Console
{
    /// <summary>
    /// Defines the CLI options based on System.Commandline conventions
    /// </summary>
    public partial class CliOptions
    {

        public static readonly Argument<FileInfo[]> IdsFilesArgument = new Argument<FileInfo[]>("idsFiles", "Path to one or more IDS files (*.ids|*.xml)")
        {
            Arity = ArgumentArity.OneOrMore,
        };

        public static readonly Option<Verbosity> VerbosityOption = new Option<Verbosity>
            (aliases: new[] { "--verbosity", "-v" },
            description: "Verbosity of the output")
            {
                Arity = ArgumentArity.ZeroOrOne,
                IsRequired = false,
            };
        
        public static RootCommand SetupCommands(IServiceProvider provider)
        {
            VerbosityOption.SetDefaultValue(Verbosity.Detailed);
            var rootCommand = new RootCommand("Tools for verifying information deliverables in BIM models using IDS")
            {
                new VerifyCommand(provider),
                new AuditCommand(provider),
                new MigrateCommand(provider),
                new DetokeniseFileCommand(provider),
                new DetokeniseFolderCommand(provider),
            };

            rootCommand.AddGlobalOption(VerbosityOption);

            return rootCommand;
        }


        /// <summary>
        /// Defines the 'verify' SubCommand
        /// </summary>
        public class VerifyCommand : BaseCommand
        {
            public static readonly Argument<string[]> ModelFilesArgument = new Argument<string[]>
            ("models", "Path to one or model files in IFC-SPF format [or experimentally COBie in xls format] (*.ifc|*.ifczip|*.ifcxml)")
            {
                Arity = ArgumentArity.OneOrMore,
            };

            public static readonly Option<string[]> IdsFilesOption = new Option<string[]>
            (aliases: new[] { "--idsFiles", "-ids" },
            description: "Path to one or more IDS files (*.ids|*.xml)")
            {
                Arity = ArgumentArity.OneOrMore,
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            };

            public static readonly Option<string> IdsFilterOption = new Option<string>
                (aliases: new[] { "--filter", "-f" },
                description: "Only run IDS specs matching this filter. (* = matching spec 'Name', otherwise searches 'identifier')")
            {
                Arity = ArgumentArity.ZeroOrOne,
                IsRequired = false,
            };

            public VerifyCommand(IServiceProvider provider) : base(provider, "verify", "Verify IFC and COBie models using BuildingSMART IDS")
            {
                this.AddArgument(ModelFilesArgument);
                this.AddOption(IdsFilesOption);
                this.AddOption(IdsFilterOption);

                SetCommandHandler<VerifyIfcCommand>();
            }
        }

        /// <summary>
        /// Defines the 'audit' sub-command
        /// </summary>
        public class AuditCommand : BaseCommand
        {
            public AuditCommand(IServiceProvider provider) : base(provider, "audit", "Check IDS files adhere to the latest standards")
            {
                this.AddArgument(CliOptions.IdsFilesArgument);
                SetCommandHandler<IdsAuditCommand>();
            }
        }

        /// <summary>
        /// Defines the 'migrate' sub-command
        /// </summary>
        public class MigrateCommand : BaseCommand
        {
            public MigrateCommand(IServiceProvider provider) : base(provider,"migrate", "Migrate IDS files from older schemas to IDS1.0")
            {
                this.AddArgument(CliOptions.IdsFilesArgument);

                SetCommandHandler<IdsMigratorCommand>();
            }
        }

        /// <summary>
        /// Defines the 'detokenise' sub-command for files
        /// </summary>
        public class DetokeniseFileCommand : BaseCommand
        {
            public static readonly Argument<FileInfo> IdsTemplateFileArgument = new Argument<FileInfo>("template", "An IDS template file (*.ids|*.xml)");
            public static readonly Argument<FileInfo> IdsOutputFileArgument = new Argument<FileInfo>("output", "The name of the IDS file to output [optional]") { Arity = ArgumentArity.ZeroOrOne };

            public DetokeniseFileCommand(IServiceProvider provider) : base(provider, "detokenise", "Detokenise IDS files replacing tokens (e.g. {{ProjectCode}} ) in an IDS template with values of your choosing")

            {
                this.AddArgument(IdsTemplateFileArgument);
                this.AddArgument(IdsOutputFileArgument);

                SetCommandHandler<IdsDetokeniseFileCommand>();
            }
        }

        /// <summary>
        /// Defines the 'detokenise' sub-command for files
        /// </summary>
        public class DetokeniseFolderCommand : BaseCommand
        {
            public static readonly Argument<DirectoryInfo> IdsTemplateFolderArgument = new Argument<DirectoryInfo>("folder", "A folder containing IDS files");
            public static readonly Argument<DirectoryInfo> IdsOutputFolderArgument = new Argument<DirectoryInfo>("output", "The folder where the output should go [optional]") { Arity = ArgumentArity.ZeroOrOne };
            public static readonly Option<bool> SubFoldersOption = new Option<bool> (aliases: new[] { "--recursive", "-r" }, description: "Include sub folders"){ IsRequired = false };
            public DetokeniseFolderCommand(IServiceProvider provider) : base(provider, "detokenise-folder", "Detokenise a folder of IDS files replacing tokens with values of your choosing")

            {
                this.AddOption(SubFoldersOption);
                this.AddArgument(IdsTemplateFolderArgument);
                this.AddArgument(IdsOutputFolderArgument);

                SetCommandHandler<IdsDetokeniseFolderCommand>();
            }
        }
    }
}
