using System.CommandLine.Invocation;

namespace Xbim.IDS.Validator.Console.Internal
{
    internal interface ICommand
    {
        Task<int> ExecuteAsync(InvocationContext context);
    }
}
