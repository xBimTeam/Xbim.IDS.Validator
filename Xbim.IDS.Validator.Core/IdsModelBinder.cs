using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Helpers;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Binds an IDS to an <see cref="IModel"/>
    /// </summary>
    public class IdsModelBinder
    {
        private readonly IModel model;
        private IfcQuery ifcQuery;

        private PsetFacetBinder psetFacetBinder;
        private AttributeFacetBinder attrFacetBinder;
        private IfcTypeFacetBinder ifcTypeFacetBinder;

        public IdsModelBinder(IModel model)
        {
            this.model = model;
            ifcQuery = new IfcQuery();
            psetFacetBinder = new PsetFacetBinder(model);
            attrFacetBinder = new AttributeFacetBinder(model);
            ifcTypeFacetBinder = new IfcTypeFacetBinder(model);
        }

        

        /// <summary>
        /// Returns all entities in the model that apply to a specification
        /// </summary>
        /// <param name="facets"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IEnumerable<IPersistEntity> SelectApplicableEntities(Specification spec)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            var facets = spec.Applicability.Facets;
           
            var ifcFacet = facets.OfType<IfcTypeFacet>().FirstOrDefault();
            if(ifcFacet == null)
            {
                throw new InvalidOperationException("Expected a single IfcTypeFacet");
            }
            //var expressType = facetBinder.GetExpressType(ifcFacet);
            var expression = BindFilters(ifcQuery.InstancesExpression, ifcFacet);

            foreach (var facet in facets.Except(new[] { ifcFacet }))
            {
                expression = BindFilters(expression, facet);
            }

            return ifcQuery.Execute(expression, model);
        }

        // TODO: very crude validation - need testing
        /// <summary>
        /// Validate an IFC entity meets its requirements against the defined Constraints
        /// </summary>
        /// <param name="item"></param>
        /// <param name="facet"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public IdsValidationResult ValidateRequirement(IPersistEntity item, IFacet facet, ILogger? logger)
        {
            var facetBinder = new IfcTypeFacetBinder(model);
            var result = new IdsValidationResult()
            {
                ValidationStatus = ValidationStatus.Inconclusive,
                Entity = item
            };
            switch (facet)
            {
                case IfcTypeFacet f:
                    var entityType = model.Metadata.ExpressType(item);
                    if(entityType == null)
                    {
                        result.Failures.Add($"Invalid IFC Type '{item.GetType().Name}'");
                    }
                    var actual = entityType?.Name.ToUpperInvariant();

                    if (f?.IfcType?.IsSatisfiedBy(actual) == true)
                    {
                        result.Successful.Add("IfcType '" + actual + "' was " + f.IfcType.Short());
                    }
                    else
                    {
                        result.Failures.Add("IfcType '" + actual + "' was not " + f?.IfcType?.Short());
                    }
                    if(f?.PredefinedType?.HasAnyAcceptedValue() == true)
                    {
                        var preDefValue = facetBinder.GetPredefinedType(item);
                        if (f?.PredefinedType?.IsSatisfiedBy(preDefValue) == true)
                        {
                            result.Successful.Add("PredefinedType '" + preDefValue + "' was " + f.PredefinedType.Short());
                        }
                        else
                        {
                            result.Failures.Add("PredefinedType '" + preDefValue + "' was not " + f?.PredefinedType?.Short());
                        }
                    }
                    
                    break;

                case IfcPropertyFacet pf:
                    // Test the Constraints
                    // TODO: Should be callung IsSatisfiedBy() on Name, PsetName, but needs analysis.
                    if (pf?.PropertySetName?.IsValid() == true)
                        result.Successful.Add(pf.PropertySetName.Short());
                    else
                        result.Failures.Add(pf?.PropertySetName?.Short());

                    if (pf?.PropertyName?.IsValid() == true)
                        result.Successful.Add(pf.PropertyName.Short());
                    else
                        result.Failures.Add(pf?.PropertyName?.Short());

                    var value = psetFacetBinder.GetProperty(item.EntityLabel, pf.PropertySetName.SingleValue(),
                        pf.PropertyName.SingleValue());
                    
                    if(pf.PropertyValue != null && pf.PropertyValue.IsSatisfiedBy(value?.Value, logger))
                        result.Successful.Add(pf.PropertyValue.Short());
                    else
                        result.Successful.Add(pf?.PropertyValue?.Short());

                    break;
 

                default:
                    logger.LogWarning("Skipping unimplemented validation {type}", facet.GetType().Name);
                    break;
                    //throw new NotImplementedException($"Validation of Facet not implemented: '{facet.GetType().Name}' - {facet.Short()}");
            }
            if(result.Failures.Any())
            {
                result.ValidationStatus = ValidationStatus.Failed;
            }
            else if(result.Successful.Any())
            {
                result.ValidationStatus = ValidationStatus.Success;
            }
            return result;
        }

        /// <summary>
        /// Binds an <see cref="IFacet"/> to an Expression bound to filter on IModel.Instances
        /// </summary>
        /// <param name="baseExpression"></param>
        /// <param name="facet"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private Expression BindFilters(Expression baseExpression, IFacet facet)
        {
            switch (facet)
            {
                case IfcTypeFacet f:
                    return ifcTypeFacetBinder.BindFilterExpression(baseExpression, f);

                case AttributeFacet af:
                    return attrFacetBinder.BindFilterExpression(baseExpression, af);

                case IfcPropertyFacet pf:

                    return psetFacetBinder.BindFilterExpression(baseExpression, pf);

                case IfcClassificationFacet af:
                    // TODO: 
                    return baseExpression;

                case DocumentFacet df:
                    // TODO: 
                    return baseExpression;

                case IfcRelationFacet rf:
                    // TODO: 
                    return baseExpression;

                case PartOfFacet pf:
                    // TODO: 
                    return baseExpression;

                case MaterialFacet mf:
                    // TODO: 
                    return baseExpression;

                default:
                    throw new NotImplementedException($"Facet not implemented: '{facet.GetType().Name}'");
            }
        }
    }
}
