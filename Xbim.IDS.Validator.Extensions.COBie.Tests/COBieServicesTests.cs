using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.IDS.Validator.Extensions.COBie.Binders;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Extensions.COBie.Tests
{
    [Collection(nameof(COBieTestEnvironment))]
    public class COBieServicesTests
    {
        private static readonly IServiceProvider provider;
        static COBieServicesTests()
        {
            provider = COBieTestEnvironment.ServiceProvider;
        }


        [InlineData(typeof(IfcTypeFacet), typeof(COBieTableFacetBinder))]              // Maps to COBie Entities
        [InlineData(typeof(AttributeFacet), typeof(COBieColumnFacetBinder))]          // Custom version for COBie
        [InlineData(typeof(IfcPropertyFacet), typeof(COBieAttributesSheetFacetBinder))]          // Custom binder mapping to Attributes
        [InlineData(typeof(MaterialFacet), typeof(NotSupportedBinder<MaterialFacet>))]             // No means of working on COBie2.4
        [InlineData(typeof(IfcClassificationFacet), typeof(NotSupportedBinder<IfcClassificationFacet>))]    // TODO: Redirect to Category Attribute
        [InlineData(typeof(PartOfFacet), typeof(NotSupportedBinder<PartOfFacet>))]               // TODO: Could query relations, Assemblies etc
        [Theory]
        public void ModelBinderFactoryResolvesBindersForCOBie(Type facetType, Type expectedType)
        {
            // Arrange
            var factory = provider.GetRequiredService<IIdsFacetBinderFactory>();
            var facet = (IFacet)Activator.CreateInstance(facetType);
            var context = new BinderContext();
            // Act
            var result = factory.Create(facet, context, Xbim.Common.Step21.XbimSchemaVersion.Cobie2X4);
            result.Should().NotBeNull().And.BeOfType(expectedType);
        }


        [Fact]
        public void CanResolveEnumerableIValueMapProviders()
        {
            var collection = provider.GetRequiredService<IEnumerable<IValueMapProvider>>();

            collection.Should().NotBeNull();
            collection.Should().HaveCount(2);

            
        }

    }
}
