using Xbim.Common.Step21;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IIdsFacetBinderFactory
    {
        /// <summary>
        /// Create the appropriate FacetBinder for the Facet
        /// </summary>
        /// <param name="facet">A <see cref="IFacet"/></param>
        /// <param name="schema">The target Schema</param>
        /// <returns></returns>
        IFacetBinder Create(IFacet facet, XbimSchemaVersion schema = XbimSchemaVersion.Ifc2X3);
        /// <summary>
        /// Generic method to create the appropriate FacetBinder for the Facet
        /// </summary>
        /// <typeparam name="TFacet"></typeparam>
        /// <param name="facet">A <see cref="IFacet"/></param>
        /// <param name="schema">The target Schema</param>
        /// <returns></returns>
        IFacetBinder<TFacet> Create<TFacet>(TFacet facet, XbimSchemaVersion schema = XbimSchemaVersion.Ifc2X3) where TFacet : IFacet;
    }
}