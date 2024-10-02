using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Xbim.IDS.Validator.Console.Internal;

namespace Xbim.IDS.Validator.Console
{
    public partial class CliOptions
    {
        public abstract class BaseCommand : Command
        {
            private readonly IServiceProvider provider;

            protected BaseCommand(IServiceProvider provider, string name, string description) : base(name, description)
            {
                this.provider = provider;
            }

            /// <summary>
            /// Sets the handler up to execute the provided <see cref="ICommand"/>
            /// </summary>
            /// <typeparam name="T"></typeparam>
            internal void SetCommandHandler<T>() where T : ICommand
            {
                this.SetHandler(async (ctx) =>
                {
                    var command = provider.GetRequiredService<T>();
                    var result = await command.ExecuteAsync(ctx);
                    ctx.ExitCode = result;
                });
            }
        }
    }
}
