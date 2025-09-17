using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine.Invocation;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Console.Internal;

namespace Xbim.IDS.Validator.Console.Commands
{
    /// <summary>
    /// Detokenises IDS files replacing 'Handlebar' style tokens with a value supplied at runtime
    /// </summary>
    internal class IdsDetokeniseFileCommand : TokenCommandBase, ICommand
    {
        private readonly IIdsDetokeniser detokeniser;
        private readonly ILogger<IdsDetokeniseFileCommand> logger;

        public IdsDetokeniseFileCommand(IIdsDetokeniser detokeniser, ILogger<IdsDetokeniseFileCommand> logger, IOptions<IdsConfig> config) : base(config)
        {
            this.detokeniser = detokeniser;
            this.logger = logger;
        }

        public async Task<int> ExecuteAsync(InvocationContext context)
        {
            var template = context.ParseResult.GetValueForArgument(CliOptions.DetokeniseFileCommand.IdsTemplateFileArgument);
            var outputFile = context.ParseResult.GetValueForArgument(CliOptions.DetokeniseFileCommand.IdsOutputFileArgument);
            var verbosity = context.ParseResult.GetValueForOption(CliOptions.VerbosityOption);

            if(!template.Exists)
            {
                logger.LogWarning("IDS template file {file} does not exist", template.FullName);
                return -1;
            }

            return await Execute(template, outputFile, verbosity);
        }

        public async Task<int> Execute(FileInfo templateFile, FileInfo outputFile, Verbosity verbosity)
        {
            var console = new ConsoleLogger(verbosity);

            var result = 0;

            WriteConfig(console);
            var doc = detokeniser.ReplaceTokens(templateFile, Config.Tokens);

            if(outputFile is null)
            {
                outputFile = BuildOutputFile(templateFile);
            }

            using var file = outputFile.CreateText();

            await doc.SaveAsync(file, System.Xml.Linq.SaveOptions.None, CancellationToken.None);
            console.WriteImportantLine("IDS File detokenised to '{0}'", outputFile.Name);
            return result;
        }

        
    }
}
