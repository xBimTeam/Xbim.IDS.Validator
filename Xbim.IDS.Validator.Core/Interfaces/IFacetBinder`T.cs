using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    public interface IFacetBinder<T> : IFacetBinder where T : IFacet
    {

        /// <summary>
        /// Binds the appropriate model filters to satisify the initial <paramref name="facet"/>'s criteria
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <remarks>Used to commence an Applicability Query based on a model</remarks>
        Expression BindSelectionExpression(Expression baseExpression, T facet);

        /// <summary>
        /// Binds the supplied <paramref name="baseExpression"/> ensuring that all results satsify the facet's predicate
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        Expression BindWhereExpression(Expression baseExpression, T facet);

        /// <summary>
        /// Validates the supplied <paramref name="item"/> satisfies the requirements of the supplied <paramref name="facet"/>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="requirement"></param>
        /// <param name="result"></param>
        /// <param name="facet"></param>
        void ValidateEntity(IPersistEntity item, T facet, FacetGroup requirement, IdsValidationResult result);

        /// <summary>
        /// Creates a validation context used for tracking validation progress
        /// </summary>
        /// <param name="requirement"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        ValidationContext<T> CreateValidationContext(FacetGroup requirement, T facet);
    }
}
