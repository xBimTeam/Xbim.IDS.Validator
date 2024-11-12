using Microsoft.Extensions.DependencyInjection;
using Xbim.Common.Step21;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Extensions.COBie.Binders;
using Xbim.IDS.Validator.Extensions.COBie.Configuration;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="IIdsConfigurationBuilder"/>
    /// </summary>
    public static class IdsConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds support for COBIe24 to the IdsValidator
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IIdsConfigurationBuilder AddCOBie(this IIdsConfigurationBuilder builder)
        {
            builder.Services
                .AddSingleton<IValueMapProvider, COBieValueMapProvider>()

                .RegisterIdsBinder<AttributeFacet, COBieColumnFacetBinder>(XbimSchemaVersion.Cobie2X4)
                .RegisterIdsBinder<IfcTypeFacet, COBieTableFacetBinder>(XbimSchemaVersion.Cobie2X4)
                .RegisterIdsBinder<IfcPropertyFacet, COBieAttributesSheetFacetBinder>(XbimSchemaVersion.Cobie2X4)
                // Rest are Not supported currently
                .RegisterIdsBinder<IfcClassificationFacet, NotSupportedBinder<IfcClassificationFacet>>(XbimSchemaVersion.Cobie2X4)
                .RegisterIdsBinder<MaterialFacet, NotSupportedBinder<MaterialFacet>>(XbimSchemaVersion.Cobie2X4)
                .RegisterIdsBinder<PartOfFacet, NotSupportedBinder<PartOfFacet>>(XbimSchemaVersion.Cobie2X4)
                ;

            return builder;
        }
    }
}
