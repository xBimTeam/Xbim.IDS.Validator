using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine.Invocation;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Console.Internal;
using Xbim.IO.Parser;

namespace Xbim.IDS.Validator.Console.Commands
{
    /// <summary>
    /// Detokenises IDS files replacing 'Handlebar' style tokens with a value supplied at runtime
    /// </summary>
    internal class IdsDetokeniseFolderCommand : TokenCommandBase, ICommand
    {
        private readonly IIdsDetokeniser detokeniser;
        private readonly ILogger<IdsDetokeniseFolderCommand> logger;

        public IdsDetokeniseFolderCommand(IIdsDetokeniser detokeniser, ILogger<IdsDetokeniseFolderCommand> logger, IOptions<IdsConfig> config) : base(config)
        {
            this.detokeniser = detokeniser;
            this.logger = logger;
        }

        public async Task<int> ExecuteAsync(InvocationContext context)
        {
            var sourceFolder = context.ParseResult.GetValueForArgument(CliOptions.DetokeniseFolderCommand.IdsTemplateFolderArgument);
            var outputFolder = context.ParseResult.GetValueForArgument(CliOptions.DetokeniseFolderCommand.IdsOutputFolderArgument);
            var recursive = context.ParseResult.GetValueForOption(CliOptions.DetokeniseFolderCommand.SubFoldersOption);
            var verbosity = context.ParseResult.GetValueForOption(CliOptions.VerbosityOption);

            if(sourceFolder?.Exists != true)
            {
                logger.LogWarning("IDS template folder {folder} does not exist", sourceFolder?.FullName);
                return -1;
            }

            return await Execute(sourceFolder, outputFolder, recursive, verbosity);
        }

        public async Task<int> Execute(DirectoryInfo sourceFolder, DirectoryInfo targetFolder, bool recursive, Verbosity verbosity)
        {
            var console = new ConsoleLogger(verbosity);

            var result = 0;
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (targetFolder is null)
            {
                var target = Path.Combine(sourceFolder.FullName, "output");
                targetFolder = new DirectoryInfo(target);
            }

            WriteConfig(console);
            console.WriteInfoLine("Detokenising folder '{0}'", sourceFolder.Name);
            foreach (var templateFile in sourceFolder.GetFiles("*.ids", searchOption))
            {
                if (templateFile is null)
                    continue;

                if (templateFile.FullName.StartsWith(targetFolder.FullName))
                    continue;
                logger.LogDebug("Detokenising: {file}", templateFile);
                var doc = detokeniser.ReplaceTokens(templateFile, Config.Tokens);

                var relativePath = Path.GetDirectoryName(Path.GetRelativePath(sourceFolder.FullName, templateFile.FullName));

                var outputFile = BuildOutputFile(templateFile, targetFolder, relativePath);
                using var file = outputFile.CreateText();

                await doc.SaveAsync(file, System.Xml.Linq.SaveOptions.None, CancellationToken.None);
                console
                    //.WriteInfo(ConsoleColor.White, "Template ")
                    .WriteInfo(ConsoleColor.Green, "   '{0}'", Path.Combine(relativePath, templateFile.Name))
                    .WriteInfo(ConsoleColor.White, " detokenised as\n")
                    .WriteInfo(ConsoleColor.Cyan, "=> '{0}\\{1}'\n", targetFolder.Name, Path.Combine(relativePath, outputFile.Name));


            }
            return result;
        }
    }
}
