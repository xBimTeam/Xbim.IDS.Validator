using Microsoft.Extensions.Logging;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;

namespace Xbim.IDS.Validator.Extensions.COBie.Binders
{

    /// <summary>
    /// A Facet binder that supports binding to an Excel Column/Cell value.
    /// </summary>
    /// <remarks>
    /// Employs exactly the same mechanism as IFC Attributes since in COBie columns are bound to first class .net fields
    /// e.g. CobieComponent.ExternalId == IfcProduct.GlobalId, CobieFloor.Elevation == IIfcBuildingStorey.Elevation
    /// </remarks>
    public class COBieColumnFacetBinder: AttributeFacetBinder
    {

        public COBieColumnFacetBinder(ILogger<COBieColumnFacetBinder> logger, IValueMapper valueMapper) : base(logger, valueMapper)
        {
        }

        // The base class fully handles querying COBie Attributes. For future expansion / customisation

    }
}
