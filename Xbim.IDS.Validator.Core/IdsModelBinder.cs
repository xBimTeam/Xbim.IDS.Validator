using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Extensions;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    /// <summary>
    /// Binds an IDS Specification to an <see cref="IModel"/> to filter applicable entities and test against the requirements
    /// </summary>
    public class IdsModelBinder : IIdsModelBinder
    {
        
        private readonly BinderContext binderContext;
        private IfcQuery? ifcQuery;
        private VerificationOptions? options;

        public IIdsFacetBinderFactory FacetBinderFactory { get; }


        public IdsModelBinder(IIdsFacetBinderFactory facetBinderFactory, BinderContext binderContext)
        {
            FacetBinderFactory = facetBinderFactory;
            this.binderContext = binderContext;
        }


        public void Initialise(IModel model)
        {
            this.binderContext.Model = model;
            ifcQuery = new IfcQuery();
        }

        public XbimSchemaVersion Schema => binderContext.Model?.SchemaVersion ?? XbimSchemaVersion.Ifc2X3;

        /// <summary>
        /// Returns all entities in the model that apply to a specification
        /// </summary>
        /// <param name="model"></param>
        /// <param name="spec"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IEnumerable<IPersistEntity> SelectApplicableEntities(IModel model, Specification spec)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            Initialise(model);
           
            ApplyOptions(spec);
            
    

            var facets = spec.Applicability.Facets;

            //no facets specified, return empty entities list
            if (facets.Count == 0)
                return Enumerable.Empty<IPersistEntity>();

            var ifcFacet = facets.OfType<IfcTypeFacet>().FirstOrDefault();
            Expression expression;
            if (ifcFacet != null)
            {
                // If possible start with an IFCType to narrow the selection down most efficiently
                expression = BindSelection(ifcQuery?.InstancesExpression, ifcFacet);
                foreach (var facet in facets.Except(new[] { ifcFacet }))
                {
                    expression = BindFilters(expression, facet);
                }
            }
            else
            {
                var firstFacet = facets.First();
                expression = BindSelection(ifcQuery?.InstancesExpression, firstFacet);
                foreach (var facet in facets.Except(new[] { firstFacet }))
                {
                    expression = BindFilters(expression, facet);
                }
            }

            return ifcQuery.Execute(expression, model).Distinct();
        }

        private void ApplyOptions(Specification spec)
        {
            if (options == null)
            {
                return;
            }
            if(options.IncludeSubtypes)
            {
                foreach(var facet in spec.Applicability.Facets)
                {
                    if(facet is IfcTypeFacet ifc)
                    {
                        ifc.IncludeSubtypes = true;
                    }
                }
                if(spec.Requirement != null)
                {
                    foreach (var facet in spec.Requirement!.Facets)
                    {
                        if (facet is IfcTypeFacet ifc)
                        {
                            ifc.IncludeSubtypes = true;
                        }
                    }
                }


            }
        }


        /// <summary>
        /// Validate an IFC entity meets its requirements against the defined Constraints
        /// </summary>
        /// <param name="item"></param>
        /// <param name="requirement"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public IdsValidationResult ValidateRequirement(IPersistEntity item, FacetGroup requirement, ILogger? logger)
        {

            var result = new IdsValidationResult(item, requirement, options?.OutputFullEntity ?? false);

            if(requirement == null)
            {
                return result;
            }

            foreach (var facet in requirement.Facets)
            {
                var binder = FacetBinderFactory.Create(facet, Schema);

                if(binder is ISupportOptions opts)
                {
                    opts.SetOptions(options);
                }
                var card = requirement.GetCardinality(facet) ?? RequirementCardinalityOptions.Cardinality.Expected;
                binder.ValidateEntity(item, facet, card, result);
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
        private Expression BindSelection(Expression baseExpression, IFacet facet)
        {
            var binder = FacetBinderFactory.Create(facet, Schema);
            return binder.BindSelectionExpression(baseExpression, facet); 
           
        }

        private Expression BindFilters(Expression baseExpression, IFacet facet)
        {
            var binder = FacetBinderFactory.Create(facet, Schema);
            return binder.BindWhereExpression(baseExpression, facet);
        }

        public void SetOptions(VerificationOptions options)
        {
            this.options = options;
        }
    }
}
