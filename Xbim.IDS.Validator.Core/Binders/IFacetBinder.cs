using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Binders
{
    public interface IFacetBinder<T> where T: IFacet
    {
        Expression BindFilterExpression(Expression baseExpression, T facet);
        void ValidateEntity(IPersistEntity item, FacetGroup requirement, ILogger logger, IdsValidationResult result, T facet);

        ValidationContext<T> CreateValidationContext(FacetGroup requirement, T facet);
    }
}
