using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using Xbim.IDS.Validator.Console.Internal;
using Xbim.IDS.Validator.Core.Interfaces;

namespace Xbim.IDS.Validator.Console.Commands
{

    /// <summary>
    /// Upgrades an IDS file to the latest 1.0 schema
    /// </summary>
    internal class IdsMigratorCommand : ICommand
    {
        private readonly IIdsSchemaMigrator migrator;
        private readonly ILogger<IdsMigratorCommand> logger;

        public IdsMigratorCommand(IIdsSchemaMigrator migrator, ILogger<IdsMigratorCommand> logger)
        {
            this.migrator = migrator;
            this.logger = logger;
        }


        public async Task<int> ExecuteAsync(InvocationContext context)
        {
            var ids = context.ParseResult.GetValueForArgument(CliOptions.IdsFilesArgument);
            var verbosity = context.ParseResult.GetValueForOption(CliOptions.VerbosityOption);

            return await Execute(ids, verbosity);
        }

        public Task<int> Execute(FileInfo[] idsFiles, Verbosity verbosity)
        {
            var console = new ConsoleLogger(verbosity);
            int filesUpdated = 0;
            foreach (var idsFileInfo in idsFiles)
            {
                var idsFile = idsFileInfo.FullName;
                console.WriteInfoLine(ConsoleColor.White, "Migrating IDS file {0}", idsFile);
                if (migrator.HasMigrationsToApply(idsFile))
                {
                    if (migrator.MigrateToIdsSchemaVersion(idsFile, out var xdoc, IdsLib.IdsSchema.IdsNodes.IdsVersion.Ids1_0))
                    {
                        var newFile = $"{Path.GetFileNameWithoutExtension(idsFile)}-updated.ids";
                        xdoc.Save(newFile);
                        console.WriteInfoLine(ConsoleColor.White, "Output to {0}", newFile);
                        filesUpdated++;
                    }
                }
                else
                {
                    console.WriteInfoLine("IDS {0} is already on the latest schema", idsFile);
                }


            }

            return Task.FromResult(filesUpdated);
        }
    }
}
