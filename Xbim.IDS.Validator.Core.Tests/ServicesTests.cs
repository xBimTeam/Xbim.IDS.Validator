using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.InformationSpecifications;

namespace Xbim.IDS.Validator.Core.Tests
{
    [Collection(nameof(TestEnvironment))]
    public class ServicesTests
    {
        private static readonly IServiceProvider provider;
        static ServicesTests()
        {
            provider = TestEnvironment.ServiceProvider;
        }

        [Fact]
        public void CanResolveModelBinderFactory()
        {
            var factory = provider.GetRequiredService<IIdsFacetBinderFactory>();

            factory.Should().NotBeNull();
        }

        [InlineData(typeof(IfcTypeFacet), typeof(IfcTypeFacetBinder))]
        [InlineData(typeof(AttributeFacet), typeof(AttributeFacetBinder))]
        [InlineData(typeof(IfcPropertyFacet), typeof(PsetFacetBinder))]
        [InlineData(typeof(IfcClassificationFacet), typeof(IfcClassificationFacetBinder))]
        [InlineData(typeof(MaterialFacet), typeof(MaterialFacetBinder))]
        [InlineData(typeof(PartOfFacet), typeof(PartOfFacetBinder))]
        // Docs, relations
        [Theory]
        public void ModelBinderFactoryResolvesBindersForIfc(Type facetType, Type expectedType)
        {
            // Arrange
            var factory = provider.GetRequiredService<IIdsFacetBinderFactory>();
            var facet = (IFacet)Activator.CreateInstance(facetType);
            // Act
            var result = factory.Create(facet);
            result.Should().NotBeNull().And.BeOfType(expectedType);
        }

        [InlineData(typeof(IfcTypeFacet), typeof(IfcTypeFacetBinder))]              // Maps to COBie Entities
        [InlineData(typeof(AttributeFacet), typeof(AttributeFacetBinder))]          // Just works
        [InlineData(typeof(MaterialFacet), typeof(NotSupportedBinder<MaterialFacet>))]             // No means of working on COBie2.4
        [InlineData(typeof(IfcPropertyFacet), typeof(NotSupportedBinder<IfcPropertyFacet>))]          // TODO: Can use Attributes
        [InlineData(typeof(IfcClassificationFacet), typeof(NotSupportedBinder<IfcClassificationFacet>))]    // TODO: Redirect to Category Attribute
        [InlineData(typeof(PartOfFacet), typeof(NotSupportedBinder<PartOfFacet>))]               // TODO: Could query relations, Assemblies etc
        [Theory]
        public void ModelBinderFactoryResolvesBindersForCOBie(Type facetType, Type expectedType)
        {
            // Arrange
            var factory = provider.GetRequiredService<IIdsFacetBinderFactory>();
            var facet = (IFacet)Activator.CreateInstance(facetType);
            // Act
            var result = factory.Create(facet, Xbim.Common.Step21.XbimSchemaVersion.Cobie2X4);
            result.Should().NotBeNull().And.BeOfType(expectedType);
        }

        [Fact]
        public void CanResolveModelBinder()
        {
            var modelBinder = provider.GetRequiredService<IIdsModelBinder>();

            modelBinder.Should().NotBeNull();
        }

        [Fact]
        public void CanResolveModelValidator()
        {
            var modelValidator = provider.GetRequiredService<IIdsModelValidator>();

            modelValidator.Should().NotBeNull();
        }

    }
}
