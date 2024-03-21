using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Core.Binders;
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
        /// <returns></returns>
        public static IServiceCollection AddIdsValidation(this IServiceCollection services)
        {

            services.AddTransient<IIdsModelBinder, IdsModelBinder>();
            services.AddTransient<IIdsModelValidator, IdsModelValidator>();
            services.AddScoped<BinderContext>();
            services.AddSingleton<IIdsFacetBinderFactory, IdsFacetBinderFactory>();

            // Register the binders with the Factory
            services.RegisterIdsBinder<IfcTypeFacet, IfcTypeFacetBinder>();
            services.RegisterIdsBinder<AttributeFacet, AttributeFacetBinder>();
            services.RegisterIdsBinder<IfcClassificationFacet, IfcClassificationFacetBinder>();
            services.RegisterIdsBinder<MaterialFacet, MaterialFacetBinder>();
            services.RegisterIdsBinder<IfcPropertyFacet, PsetFacetBinder>();
            services.RegisterIdsBinder<PartOfFacet, PartOfFacetBinder>();

            // TODO: Consider adding COBie as a Config builder option
            services.RegisterIdsBinder<IfcClassificationFacet, NotSupportedBinder<IfcClassificationFacet>>(XbimSchemaVersion.Cobie2X4);
            services.RegisterIdsBinder<IfcPropertyFacet, NotSupportedBinder<IfcPropertyFacet>>(XbimSchemaVersion.Cobie2X4);
            services.RegisterIdsBinder<MaterialFacet, NotSupportedBinder<MaterialFacet>>(XbimSchemaVersion.Cobie2X4);
            services.RegisterIdsBinder<PartOfFacet, NotSupportedBinder<PartOfFacet>>(XbimSchemaVersion.Cobie2X4);
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
            where TFacet : IFacet
            where TBinder : class, IFacetBinder<TFacet>
        {

            services.TryAddTransient<TBinder>();
            services.Configure<IdsBinderFactoryOptions>(options => options.Register<TFacet, TBinder>(overrideForSchema));
            return services;
        }


    }


}
