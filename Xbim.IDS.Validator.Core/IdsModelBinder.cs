using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Binds an IDS Specification to an <see cref="IModel"/> to filter applicable entities and test against the requirements
    /// </summary>
    public class IdsModelBinder
    {
        private readonly IModel model;
        private IfcQuery ifcQuery;

        private PsetFacetBinder psetFacetBinder;
        private AttributeFacetBinder attrFacetBinder;
        private IfcTypeFacetBinder ifcTypeFacetBinder;
        private MaterialFacetBinder materialFacetBinder;
        private IfcClassificationFacetBinder classificationFacetBinder;

        public IdsModelBinder(IModel model)
        {
            this.model = model;
            ifcQuery = new IfcQuery();
            psetFacetBinder = new PsetFacetBinder(model);
            attrFacetBinder = new AttributeFacetBinder(model);
            ifcTypeFacetBinder = new IfcTypeFacetBinder(model);
            materialFacetBinder = new MaterialFacetBinder(model);
            classificationFacetBinder = new IfcClassificationFacetBinder(model);
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

   
        /// <summary>
        /// Validate an IFC entity meets its requirements against the defined Constraints
        /// </summary>
        /// <param name="item"></param>
        /// <param name="facet"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public IdsValidationResult ValidateRequirement(IPersistEntity item, FacetGroup requirement, IFacet facet, ILogger? logger)
        {
            
            var result = new IdsValidationResult()
            {
                ValidationStatus = ValidationStatus.Inconclusive,
                Entity = item
            };
            

            switch (facet)
            {
                case IfcTypeFacet f:
                    
                    
                    ifcTypeFacetBinder.ValidateEntity(item, requirement, logger, result, f);
                    break;
                    

                case IfcPropertyFacet pf:
                    
                    psetFacetBinder.ValidateEntity(item, requirement, logger, result, pf);
                    break;
                    

                case AttributeFacet af:
                    
                    attrFacetBinder.ValidateEntity(item, requirement, logger, result, af);
                    break;

                case MaterialFacet mf:

                    materialFacetBinder.ValidateEntity(item, requirement, logger, result, mf);
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

                case IfcClassificationFacet cf:
                    return classificationFacetBinder.BindFilterExpression(baseExpression, cf);

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
                    return materialFacetBinder.BindFilterExpression(baseExpression, mf);

                default:
                    throw new NotImplementedException($"Facet not implemented: '{facet.GetType().Name}'");
            }
        }
    }
}
