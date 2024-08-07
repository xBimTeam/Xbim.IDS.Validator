﻿using System.Linq.Expressions;
using Xbim.Common;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Interfaces
{
    /// <summary>
    /// Interface defining how IDS <see cref="IFacet"/>s build queries against a xbim model.
    /// </summary>
    public interface IFacetBinder
    {
        /// <summary>
        /// Initialise the FacetBinder
        /// </summary>
        /// <param name="context"></param>
        void Initialise(IBinderContext context);

        /// <summary>
        /// Binds the appropriate model filters to satisify the initial <paramref name="facet"/>'s criteria
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <remarks>Used to commence an Applicability Query based on a model</remarks>
        Expression BindSelectionExpression(Expression baseExpression, IFacet facet);

        /// <summary>
        /// Binds the supplied <paramref name="baseExpression"/> ensuring that all results satsify the facet's predicate
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        Expression BindWhereExpression(Expression baseExpression, IFacet facet);

        /// <summary>
        /// Validates the supplied <paramref name="item"/> satisfies the requirements of the supplied <paramref name="facet"/>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="requirement"></param>
        /// <param name="result"></param>
        /// <param name="facet"></param>
        void ValidateEntity(IPersistEntity item, IFacet facet, Cardinality requirement, IdsValidationResult result);

    }
}
