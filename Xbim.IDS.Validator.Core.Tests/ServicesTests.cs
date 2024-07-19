using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xbim.IDS.Validator.Common.Interfaces;
using Xbim.IDS.Validator.Core.Binders;
using Xbim.IDS.Validator.Core.Interfaces;
using Xbim.IDS.Validator.Tests.Common;
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
            var context = new BinderContext();
            // Act
            var result = factory.Create(facet, context);
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

        [Fact]
        public void CanResolveIValueMapper()
        {
            var service = provider.GetRequiredService<IValueMapper>();

            service.Should().NotBeNull();
        }


        [Fact]
        public void CanResolveIValueMapProviders()
        {
            var service = provider.GetRequiredService<IValueMapProvider>();

            service.Should().NotBeNull();
        }

        [Fact]
        public void CanResolveEnumerableIValueMapProviders()
        {
            var collection = provider.GetRequiredService<IEnumerable<IValueMapProvider>>();

            collection.Should().NotBeNull();
            collection.Should().HaveCount(1);
        }

    }
}
