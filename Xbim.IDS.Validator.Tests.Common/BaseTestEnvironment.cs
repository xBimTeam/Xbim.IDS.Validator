using Divergic.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if XbimV6
using Xbim.Common.Configuration;
#endif
using Xunit;
using Xunit.Abstractions;

namespace Xbim.IDS.Validator.Tests.Common
{
   
    /// <summary>
    /// Common infrastructure to sets up a singleton test environment and services for Tests
    /// </summary>
    public abstract class BaseTestEnvironment : IDisposable
    {
        private static ILoggerProvider xunitLoggerProvider = null;
#if XbimV6
        public static IServiceProvider ServiceProvider { get => XbimServices.Current.ServiceProvider; }

        public BaseTestEnvironment()
        {

            Console.WriteLine("Initialised Test Environment");

            XbimServices.Current.ConfigureServices(services => InitialiseEnvironment(services));
        }
#else
        static IServiceCollection services = new ServiceCollection();
        static IServiceProvider serviceProvider = null;

        public static IServiceProvider ServiceProvider { get => serviceProvider; }
        public BaseTestEnvironment()
        {
            
            Console.WriteLine("Initialised Test Environment");
            InitialiseEnvironment(services);
            serviceProvider = services.BuildServiceProvider();
        }
#endif

        public abstract void InitialiseEnvironment(IServiceCollection services);

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
