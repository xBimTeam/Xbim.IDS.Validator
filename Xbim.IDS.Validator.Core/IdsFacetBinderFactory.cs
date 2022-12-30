using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{

    public class IdsBinderFactoryOptions
    {
        public IDictionary<Type, Type> Types { get; } = new Dictionary<Type, Type>();

        public void Register<TFacet, TBinder>() 
            where TFacet : IFacet 
            where TBinder: IFacetBinder<TFacet>
        {
            Types.Add(typeof(TFacet), typeof(TBinder));
        }
    }

    /// <summary>
    /// An abstract factory implementation enabling 
    /// </summary>
    public class IdsFacetBinderFactory : IIdsFacetBinderFactory
    {
        private readonly IServiceProvider _provider;
        private readonly IDictionary<Type, Type> _types;
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
            _types = options.Value.Types;
            _logger = logger;
        }

        public IFacetBinder Create(IFacet facet)
        {
            if (_types.TryGetValue(facet.GetType(), out var type))
            {
                return (IFacetBinder)_provider.GetRequiredService(type);
            }
            throw new NotImplementedException(facet.GetType().Name);
        }

        public IFacetBinder<TFacet> Create<TFacet>(TFacet facet) where TFacet : IFacet
        {
            if (_types.TryGetValue(facet.GetType(), out var type))
            {
                return (IFacetBinder<TFacet>)_provider.GetRequiredService(type);
            }
            _logger.LogError("Did not find a binder registered for facet type {tyneName}", facet.GetType().Name);
            throw new NotImplementedException(facet.GetType().Name);
        }


    }
}
