using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;

namespace Xbim.IDS.Validator.Core
{
    public class IdsValidationResult
    {
        public ValidationStatus ValidationStatus { get; set; }
        public IPersistEntity? Entity { get; set; }

        public IList<string> Successful { get; set; } = new List<string>();
        public IList<string> Failures { get; set; } = new List<string>();
    }

    public enum ValidationStatus
    {
        Success,
        Failed,
        Inconclusive
    }
}
