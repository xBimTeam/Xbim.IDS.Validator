using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;

namespace Xbim.IDS.Validator.Extensions.COBie.Binders
{
    public class COBieAttributeFacetBinder: AttributeFacetBinder
    {

        public COBieAttributeFacetBinder(ILogger<COBieAttributeFacetBinder> logger, IValueMapper valueMapper) : base(logger, valueMapper)
        {
        }

        // The base class fully handles querying COBie Attributes. For future expansion / customisation

    }
}
