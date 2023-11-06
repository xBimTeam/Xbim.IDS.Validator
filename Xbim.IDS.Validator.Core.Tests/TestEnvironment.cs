using Divergic.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if IFC4x3
using Xbim.Common.Configuration;
#endif
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Core.Tests
{
    [CollectionDefinition(nameof(TestEnvironment))]
    public class xUnitBootstrap : ICollectionFixture<TestEnvironment>
    {
        // Just to bootStrap TestEnvironment
    }

    /// <summary>
    /// Sets up singleton test environment and services
    /// </summary>
    public class TestEnvironment : IDisposable
    {
        private static ILoggerProvider xunitLoggerProvider = null;
#if !IFC4x3
        static IServiceCollection services = new ServiceCollection();
        static IServiceProvider serviceProvider = null;
#endif

        public TestEnvironment()
        {
            
            Console.WriteLine("Initialised Test Environment");
#if IFC4x3
            XbimServices.Current.ConfigureServices(services => 
                {
                    services.AddXbimToolkit(/*c => c.AddMemoryModel()*/)
                        .AddIdsValidation()
                        .AddLogging(s=> s.SetMinimumLevel(LogLevel.Debug));
                });
#else
            services
                .AddIdsValidation()
                .AddLogging(s => s.SetMinimumLevel(LogLevel.Debug));
            serviceProvider = services.BuildServiceProvider();
#endif
        }
#if IFC4x3
        public static IServiceProvider ServiceProvider { get => XbimServices.Current.ServiceProvider;  }
#else
        public static IServiceProvider ServiceProvider { get => serviceProvider; }
#endif

        /// <summary>
        ///  Registers the current tests xUnit OutputHelper with the Divergent xUnit Logger.
        /// </summary>
        /// <remarks>Enables Toolkit services to log to xUnit output.
        /// This is still brittle and will likely only produce reliable results when testing a single test fixture
        /// <</remarks>
        /// <param name="output"></param>
        internal static void InitialiseXunitLogger(ITestOutputHelper output)
        {
            bool firstTime = xunitLoggerProvider == null;

            // Dodgy practice when running tests in parallel
            xunitLoggerProvider = new TestOutputLoggerProvider(output, new LoggingConfig
            {
                IgnoreTestBoundaryException = true
            });
            if (firstTime)
            {
                var logFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
                logFactory.AddProvider(xunitLoggerProvider);
            }
        }

        public void Dispose()
        {

        }

        /// <summary>
        /// Gets an ILogger connected to the supplied <see cref="ITestOutputHelper"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="output"></param>
        /// <returns></returns>
        public static ILogger<T> GetXunitLogger<T>(ITestOutputHelper output)
        {
            var services = new ServiceCollection()
                        .AddLogging((builder) => builder.AddXunit(output,
                        new Divergic.Logging.Xunit.LoggingConfig { LogLevel = LogLevel.Debug }));

            IServiceProvider provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILogger<T>>();
            Assert.NotNull(logger);
            return logger;
        }
    }
}
