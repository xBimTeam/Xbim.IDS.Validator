using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.InformationSpecifications;
using static Xbim.InformationSpecifications.RequirementCardinalityOptions;

namespace Xbim.IDS.Validator.Core.Binders
{
    /// <summary>
    /// A binder used when a IDS FacetType is not supported.
    /// </summary>
    /// <remarks>For example, COBie schema does not support Material facets</remarks>
    public class NotSupportedBinder<TFacet> : FacetBinderBase<TFacet> where TFacet : IFacet
    {
        private readonly ILogger<FacetBinderBase<TFacet>> logger;

        public NotSupportedBinder(ILogger<NotSupportedBinder<TFacet>> logger) : base(logger)
        {
            this.logger = logger;
        }

        public override Expression BindSelectionExpression(Expression baseExpression, TFacet facet)
        {
            logger.LogWarning("Selection of facet type {facetType} is not supported for this model schema.", facet.GetType().Name);
            return BindNotFound(baseExpression);
        }

        public override Expression BindWhereExpression(Expression baseExpression, TFacet facet)
        {
            logger.LogWarning("Filtering by facet type {facetType} is not supported for this model schema.", facet.GetType().Name);
            return BindNotFound(baseExpression);
        }

        public override void ValidateEntity(IPersistEntity item, TFacet facet, Cardinality cardinality, IdsValidationResult result)
        {
            throw new NotSupportedException($"Validation not supported for facet type {facet.GetType().Name} for this model schema");
        }
    }
}
