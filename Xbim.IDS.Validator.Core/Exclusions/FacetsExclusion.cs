using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.Ifc;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Exclusions
{
    /// <summary>
    /// An exclusion policy using an IDS applicability group to filter out entities from testing
    /// </summary>
    /// <remarks><para>Enables expressions to be built to exclude entities using IDS grammar. A potential mechanism to support
    /// any future formal IDS exclusion strategy.</para>
    /// <para>Note: a <see cref="FacetsExclusion"/> instance must not be re-used across models</para></remarks>
    public class FacetsExclusion : ISpecificationExclusion
    {
        private readonly IModel _model;
        private readonly HashSet<IPersistEntity> _entities;

        /// <summary>
        /// Constructs and initialises a <see cref="FacetsExclusion"/> policy
        /// </summary>
        /// <param name="idsApplicability">A set of IDS facets to apply</param>
        /// <param name="model">The model being validated</param>
        /// <param name="idsModelBinder">A Model Binder instance</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidOperationException"/>
        public FacetsExclusion(FacetGroup idsApplicability, IModel model, IIdsModelBinder idsModelBinder)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if(idsApplicability.IsValid() == false)
            {
                throw new InvalidOperationException("IDS facets invalid");
            }

            this._model = model is IfcStore store ? store.Model : model;

            _entities = LoadExcludedEntities(model, idsApplicability, idsModelBinder);
        }

        public string PolicyType => "IDS Facet";

        private static HashSet<IPersistEntity> LoadExcludedEntities(IModel model, FacetGroup idsApplicability, IIdsModelBinder idsModelBinder)
        {
            var schema = model.SchemaVersion switch
            {
                Xbim.Common.Step21.XbimSchemaVersion.Ifc4 => IfcSchemaVersion.IFC4,
                Xbim.Common.Step21.XbimSchemaVersion.Ifc4x1 => IfcSchemaVersion.IFC4,
                Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3 => IfcSchemaVersion.IFC2X3,
                Xbim.Common.Step21.XbimSchemaVersion.Ifc4x3 => IfcSchemaVersion.IFC4X3,
                Xbim.Common.Step21.XbimSchemaVersion.Cobie2X4 => IfcSchemaVersion.Undefined,
                _ => IfcSchemaVersion.IFC2X3,
            };

            var exclusionIds = new Xids();

            var exclusionSpec = exclusionIds.PrepareSpecification(schema, idsApplicability);
            var options = new VerificationOptions() 
            {
                PerformInPlaceSchemaUpgrade = false,
                SkipIncompatibleSpecification = false 
            };
            idsModelBinder.SetOptions(options);

            return idsModelBinder.SelectApplicableEntities(model, exclusionSpec, NullLogger.Instance).ToHashSet();
        }


        //<inheritDoc/>
        public bool IsEntityMatching(Specification _, IPersistEntity entity)
        {
            var model = entity.Model;
            if(model != _model)
            {
                throw new InvalidOperationException("Exclusions have mis-matched models. Ensure the exclusion is set up for this model.");
            }

            return _entities.Contains(entity);

        }
    }
}
