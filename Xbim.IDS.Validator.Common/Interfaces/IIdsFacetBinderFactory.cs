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
        /// <param name="facet"></param>
        /// <returns></returns>
        IFacetBinder Create(IFacet facet);
        /// <summary>
        /// Generic method to create the appropriate FacetBinder for the Facet
        /// </summary>
        /// <typeparam name="TFacet"></typeparam>
        /// <param name="facet"></param>
        /// <returns></returns>
        IFacetBinder<TFacet> Create<TFacet>(TFacet facet) where TFacet : IFacet;
    }
}