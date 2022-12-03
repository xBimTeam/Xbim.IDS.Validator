using System.Linq.Expressions;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{
    public interface IFacetBinder<T> where T: IFacet
    {
        Expression BindFilterExpression(Expression baseExpression, T facet);
    }
}
