using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine.Invocation;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Console.Internal;
using Xbim.IDS.Validator.Core.Interfaces;

namespace Xbim.IDS.Validator.Console.Commands
{
    /// <summary>
    /// Detokenises IDS files replacing 'Handlebar' style tokens with a value supplied at runtime
    /// </summary>
    internal class IdsDetokeniseCommand : ICommand
    {
        private readonly IIdsDetokeniser detokeniser;
        private readonly ILogger<IdsDetokeniseCommand> logger;
        private readonly IdsConfig config;

        public IdsDetokeniseCommand(IIdsDetokeniser detokeniser, ILogger<IdsDetokeniseCommand> logger, IOptions<IdsConfig> config)
        {
            this.detokeniser = detokeniser;
            this.logger = logger;
            this.config = config.Value;
        }

        public async Task<int> ExecuteAsync(InvocationContext context)
        {
            var template = context.ParseResult.GetValueForArgument(CliOptions.DetokeniseCommand.IdsTemplateFileArgument);
            var outputFile = context.ParseResult.GetValueForArgument(CliOptions.DetokeniseCommand.IdsOutputFileArgument);
            var verbosity = context.ParseResult.GetValueForOption(CliOptions.VerbosityOption);

            return await Execute(template, outputFile, verbosity);
        }

        public async Task<int> Execute(FileInfo templateFile, FileInfo outputFile, Verbosity verbosity)
        {
            var console = new ConsoleLogger(verbosity);

            var result = 0;

            var doc = detokeniser.ReplaceTokens(templateFile, config.Tokens);

            using var file = outputFile.CreateText();

            await doc.SaveAsync(file, System.Xml.Linq.SaveOptions.None, CancellationToken.None);


            return result;
        }
    }
}
