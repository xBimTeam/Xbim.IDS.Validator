using System.CommandLine.Invocation;

namespace Xbim.IDS.Validator.Console
{
    internal interface ICommand
    {
        Task<int> ExecuteAsync(InvocationContext context);
    }
}
