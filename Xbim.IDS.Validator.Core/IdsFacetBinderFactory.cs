using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.InformationSpecifications;


namespace Xbim.IDS.Validator.Core
{

    public class IdsBinderFactoryOptions
    {
        public IDictionary<Type, Type> DefaultBinderMappings { get; } = new Dictionary<Type, Type>();
        public ConcurrentDictionary<XbimSchemaVersion, IDictionary<Type, Type>> SchemaOverrideMappings { get; } = new ConcurrentDictionary<XbimSchemaVersion, IDictionary<Type, Type>>();

        /// <summary>
        /// Registers a Facet against a FacetBinder, and permits an schema override.
        /// </summary>
        /// <typeparam name="TFacet"></typeparam>
        /// <typeparam name="TBinder"></typeparam>
        /// <param name="schemaVersion"></param>
        public void Register<TFacet, TBinder>(XbimSchemaVersion? schemaVersion) 
            where TFacet : IFacet 
            where TBinder: IFacetBinder<TFacet>
        {
            if(schemaVersion.HasValue)
            {
                var schemaMappings = SchemaOverrideMappings.GetOrAdd(schemaVersion.Value, _ => new Dictionary<Type, Type>());
                schemaMappings.Add(typeof(TFacet), typeof(TBinder));
            }
            else
            {

                DefaultBinderMappings.Add(typeof(TFacet), typeof(TBinder));
            }
        }
    }

    /// <summary>
    /// An abstract factory implementation enabling the correct binder to be created for a given Facet
    /// </summary>
    public class IdsFacetBinderFactory : IIdsFacetBinderFactory
    {
        private readonly IServiceProvider _provider;
        private readonly IdsBinderFactoryOptions _factoryOptions;
        private readonly ILogger<IdsFacetBinderFactory> _logger;

        /// <summary>
        /// Construct a new <see cref="IdsFacetBinderFactory"/>
        /// </summary>
        /// <param name="provider">The service provider used by DI</param>
        /// <param name="options">The mappiung register</param>
        /// <param name="logger"></param>
        public IdsFacetBinderFactory(IServiceProvider provider, IOptions<IdsBinderFactoryOptions> options, ILogger<IdsFacetBinderFactory> logger)
        {
            _provider = provider;
            _factoryOptions = options.Value;
            _logger = logger;
        }

        protected IDictionary<Type, Type> DefaultBinderMappings => _factoryOptions.DefaultBinderMappings;
        protected IDictionary<XbimSchemaVersion, IDictionary<Type, Type>> SchemaOverrideMappings => _factoryOptions.SchemaOverrideMappings;

        public IFacetBinder Create(IFacet facet, IBinderContext context, XbimSchemaVersion schema)
        {
            if (TryGetBinderType(schema, facet, out Type type))
            {
                var binder = (IFacetBinder)_provider.GetRequiredService(type);
                binder.Initialise(context);
                return binder;
            }
            throw new NotImplementedException(facet.GetType().Name);
        }

        public IFacetBinder<TFacet> Create<TFacet>(TFacet facet, IBinderContext context, XbimSchemaVersion schema) where TFacet : IFacet
        {
            if (TryGetBinderType(schema, facet, out Type type))
            {
                var binder = (IFacetBinder<TFacet>)_provider.GetRequiredService(type);
                binder.Initialise(context);
                return binder;
            }
            _logger.LogError("Did not find a binder registered for facet type {tyneName}", facet.GetType().Name);
            throw new NotImplementedException(facet.GetType().Name);
        }

        private bool TryGetBinderType(XbimSchemaVersion schema, IFacet facet, out Type value)
        {
            // First we look to see if there's a schema-based overide of the mapping. E.g. COBie replaces some of the Binders
            if(SchemaOverrideMappings.TryGetValue(schema, out var schemaOverrides))
            {
                if(schemaOverrides.TryGetValue(facet.GetType(), out value))
                {
                    // We have an override for this schema
                    return true;
                }
            }
            // otherwise, return the default mapping
            return DefaultBinderMappings.TryGetValue(facet.GetType(), out value);
        }


    }
}
