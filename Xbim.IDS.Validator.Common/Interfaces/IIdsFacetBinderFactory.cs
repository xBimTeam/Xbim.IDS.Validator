using Xbim.Common.Step21;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    /// <summary>
    /// Interface for a Factory creating <see cref="IFacetBinder"/>s for a given <see cref="IFacet"/>
    /// </summary>
    public interface IIdsFacetBinderFactory
    {
        /// <summary>
        /// Create the appropriate FacetBinder for the Facet
        /// </summary>
        /// <param name="facet">A <see cref="IFacet"/></param>
        /// <param name="context">The Binder context</param>
        /// <param name="schema">The target Schema</param>
        /// <returns></returns>
        IFacetBinder Create(IFacet facet,  IBinderContext context, XbimSchemaVersion schema = XbimSchemaVersion.Ifc2X3);
        /// <summary>
        /// Generic method to create the appropriate FacetBinder for the Facet
        /// </summary>
        /// <typeparam name="TFacet"></typeparam>
        /// <param name="facet">A <see cref="IFacet"/></param>
        /// <param name="context">The Binder context</param>
        /// <param name="schema">The target Schema</param>
        /// <returns></returns>
        IFacetBinder<TFacet> Create<TFacet>(TFacet facet, IBinderContext context, XbimSchemaVersion schema = XbimSchemaVersion.Ifc2X3) where TFacet : IFacet;
    }
}