using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IIdsFacetBinderFactory
    {
        IFacetBinder Create(IFacet facet);
        IFacetBinder<TFacet> Create<TFacet>(TFacet facet) where TFacet : IFacet;
    }
}