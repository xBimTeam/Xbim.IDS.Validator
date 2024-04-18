using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;

namespace Xbim.IDS.Validator.Extensions.COBie.Binders
{
    /// <summary>
    /// A Cobie-specific Facet binder for IfcTypes
    /// </summary>
    /// <remarks>Makes use of Proprietary use of IFC Type factets where the EntityName corresponds to the COBie Type name implemented in COBieExpress. E.g. 
    /// IFCPRODUCT broadly equivalent to COBIECOMPONENT, IFCACTOR => COBIECONTACT table</remarks>
    public class COBieTableFacetBinder : IfcTypeFacetBinder
    {
        public COBieTableFacetBinder(BinderContext binderContext, ILogger<IfcTypeFacetBinder> logger, IValueMapper valueMapper) : base(binderContext, logger, valueMapper)
        {
        }
        // Provided for future use/expansion. E.g. mapping frm IFC Types
    }
}
