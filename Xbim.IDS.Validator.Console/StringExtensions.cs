using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.IDS.Validator.Console
{
    public static class StringExtensions
    {
        public static string SplitClauses(this string message)
        {
            return message.Replace("AND", "\n    -- AND", StringComparison.InvariantCulture);
        }
    }
}
