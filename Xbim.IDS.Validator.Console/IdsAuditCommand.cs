using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using Xbim.IDS.Validator.Core.Interfaces;

namespace Xbim.IDS.Validator.Console
{
    /// <summary>
    /// Audits IDS files against the IDS 1.0 standards using ids-lib
    /// </summary>
    internal class IdsAuditCommand: ICommand
    {
        private readonly IIdsValidator idsValidator;
        private readonly ILogger<IdsAuditCommand> logger;

        public IdsAuditCommand(IIdsValidator idsValidator, ILogger<IdsAuditCommand> logger)
        {
            this.idsValidator = idsValidator;
            this.logger = logger;
        }

        public async Task<int> ExecuteAsync(InvocationContext context)
        {
            var ids = context.ParseResult.GetValueForOption(CliOptions.IdsFilesOption);
            var verbosity = context.ParseResult.GetValueForOption(CliOptions.VerbosityOption);

            return await Execute(ids, verbosity);
        }

        public Task<int> Execute(string[] idsFiles, Verbosity verbosity)
        {
            var console = new ConsoleLogger(verbosity);
            IdsLib.Audit.Status result = IdsLib.Audit.Status.Ok;
            foreach (var idsFile in idsFiles) 
            {
                console.WriteInfoLine( ConsoleColor.White,"Auditing {0}", idsFile);
                var res = idsValidator.ValidateIDS(idsFile, logger);
                console.WriteInfoLine(ConsoleColor.White, "Result: {1} for {0}", idsFile, res);
                result |= res;
            }

            return Task.FromResult((int)result);
        }
    }
}
