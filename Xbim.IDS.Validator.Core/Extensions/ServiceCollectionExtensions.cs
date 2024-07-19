using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Configuration;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers xbim IDS with the application's Service collection
        /// </summary>
        /// <param name="services"></param>
        /// <returns>The <see cref="IServiceCollection"/> so additional calls can be chained</returns>
        public static IServiceCollection AddIdsValidation(this IServiceCollection services)
        {
            return services.AddIdsValidation(conf => { });
        }

        /// <summary>
        /// Register and configure xbim IDS with the application's Service collection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns>The <see cref="IServiceCollection"/> so additional calls can be chained</returns>
        public static IServiceCollection AddIdsValidation(this IServiceCollection services, Action<IIdsConfigurationBuilder> configure)
        {
            var builder = new IdsConfigurationBuilder(services);

            services.AddTransient<IIdsModelBinder, IdsModelBinder>();
            services.AddTransient<IIdsModelValidator, IdsModelValidator>();
            services.AddTransient<IIdsValidator, IdsValidator>();
            services.AddTransient<IIdsSchemaMigrator, IdsSchemaMigrator>();
            
            services.AddSingleton<IIdsFacetBinderFactory, IdsFacetBinderFactory>();
            services.AddSingleton<IValueMapper, IdsValueMapper>();
            services.AddSingleton<IValueMapProvider, IdsValueMapProvider>();

            // Register the binders with the Factory
            services.RegisterIdsBinder<IfcTypeFacet, IfcTypeFacetBinder>();
            services.RegisterIdsBinder<AttributeFacet, AttributeFacetBinder>();
            services.RegisterIdsBinder<IfcClassificationFacet, IfcClassificationFacetBinder>();
            services.RegisterIdsBinder<MaterialFacet, MaterialFacetBinder>();
            services.RegisterIdsBinder<IfcPropertyFacet, PsetFacetBinder>();
            services.RegisterIdsBinder<PartOfFacet, PartOfFacetBinder>();


            configure(builder);

            return services;
        }

        /// <summary>
        /// Registers a binder to be used with a defined <see cref="IFacet"/>
        /// </summary>
        /// <remarks>This enables the abstract factory <see cref="IdsFacetBinderFactory"/> to discover binders</remarks>
        /// <typeparam name="TFacet"></typeparam>
        /// <typeparam name="TBinder"></typeparam>
        /// <param name="services"></param>
        /// <param name="overrideForSchema">The schema to override the binding</param>
        /// <returns></returns>
        public static IServiceCollection RegisterIdsBinder<TFacet, TBinder>(this IServiceCollection services, XbimSchemaVersion? overrideForSchema = null)
            where TFacet: IFacet
            where TBinder : class, IFacetBinder<TFacet>
        {
            
            services.TryAddTransient<TBinder>();
            services.Configure<IdsBinderFactoryOptions>(options => options.Register<TFacet,TBinder>(overrideForSchema));
            return services;
        }


    }


}
