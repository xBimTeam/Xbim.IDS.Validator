using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;

namespace Xbim.IDS.Validator.Core.Helpers
{
    internal static class LoggerExtensions
    {
        public static void LogNotImplemented(this ILogger? logger, string message, [CallerMemberName] string callerMethodName = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (logger == null)
            {
                throw new NotImplementedException(string.Format("In {0} at line {1} of {2}: {3}", callerMethodName, line, file, message));
            }
            else
            {
                logger.LogError("Not Implemented in method {file}.{method} at line {line}. {message}", file, callerMethodName, line, message);

            }
        }
    }
}
