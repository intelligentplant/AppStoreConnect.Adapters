using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#if NETCOREAPP
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AssemblyInitializer {

        public const string AdapterId = "sensor-csv";

        public const string TestTagId = "Sensor_001";

        public const string TestAnnotationId = "test_annotation";

        public static IServiceProvider ApplicationServices { get; private set; }

#if NETCOREAPP

        private static WebApplication s_webHost;


        [AssemblyInitialize]
        public static async Task Init(TestContext testContext) {
            if (s_webHost != null) {
                return;
            }

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.UseUrls(WebHostConfiguration.DefaultUrl);
            builder.WebHost.ConfigureKestrel(options => {
                options.ConfigureEndpointDefaults(listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
            });

            AddAdapterServices(builder.Services);
            WebHostStartup.ConfigureServices(builder.Services);

            var app = builder.Build();
            WebHostStartup.Configure(app, app.Environment);

            ApplicationServices = app.Services;
            await app.StartAsync();
            s_webHost = app;
        }


        [AssemblyCleanup]
        public static async Task Cleanup() {
            if (s_webHost is null) {
                return;
            }

            await s_webHost.DisposeAsync().ConfigureAwait(false);
        }

#else

        [AssemblyInitialize]
        public static void Init(TestContext testContext) {
            var services = new ServiceCollection();
            AddAdapterServices(services);
            ApplicationServices = services.BuildServiceProvider();
        }

#endif


        internal static void AddAdapterServices(IServiceCollection services) {
            services
                .AddSingleton<Events.InMemoryEventMessageStoreOptions>()
                .AddSingleton<Diagnostics.ConfigurationChangesOptions>()
                .AddDataCoreAdapterServices()
                .AddAdapter(sp => {
                    var adapter = ActivatorUtilities.CreateInstance<Csv.CsvAdapter>(sp, AdapterId, new Csv.CsvAdapterOptions() {
                        Name = "Sensor CSV",
                        Description = "CSV adapter with dummy sensor data",
                        IsDataLoopingAllowed = true,
                        GetCsvStream = () => typeof(AssemblyInitializer).Assembly.GetManifestResourceStream(typeof(AssemblyInitializer), "DummySensorData.csv")
                    });

                    // Add in-memory event message management
                    adapter.AddStandardFeatures(
                        ActivatorUtilities.CreateInstance<Events.InMemoryEventMessageStore>(sp, sp.GetService<ILoggerFactory>())
                    );

                    // Add dummy tag value writing.
                    adapter.AddStandardFeatures(new NullValueWrite());

                    // Add configuration change notifier.
                    var configurationChanges = ActivatorUtilities.CreateInstance<Diagnostics.ConfigurationChanges>(sp, sp.GetService<ILogger<Diagnostics.ConfigurationChanges>>());
                    adapter.AddStandardFeatures(configurationChanges);

                    // Add asset model.
                    adapter.AddStandardFeatures(
                        ActivatorUtilities.CreateInstance<AssetModel.AssetModelManager>(sp, AssetModel.AssetModelManager.CreateConfigurationChangeDelegate(configurationChanges))
                    );

                    // Add tag annotations.
                    adapter.AddStandardFeatures(
                        ActivatorUtilities.CreateInstance<RealTimeData.InMemoryTagValueAnnotationManager>(sp, new RealTimeData.TagValueAnnotationManagerOptions() {
                            TagResolver = RealTimeData.InMemoryTagValueAnnotationManager.CreateTagResolverFromAdapter(adapter)
                        })
                    );

                    // Configure custom functions handler.
                    var customFunctions = (Extensions.CustomFunctions) adapter.GetFeature<Extensions.ICustomFunctions>().Unwrap();
                    customFunctions.DefaultAuthorizeHandler = null; // Authorization not required for tests

                    // Add ping-pong extension
                    adapter.AddExtensionFeatures(new PingPongExtension(adapter.BackgroundTaskService, sp.GetServices<Common.IObjectEncoder>()));

                    adapter.Started += OnAdapterStarted;

                    return adapter;
                });
        }


        private static async Task OnAdapterStarted(IAdapter adapter) {
            var assetModelManager = adapter.GetFeature<AssetModel.IAssetModelBrowse>().Unwrap() as AssetModel.AssetModelManager;
            if (assetModelManager != null) {
                await assetModelManager.InitAsync().ConfigureAwait(false);

                var root = new AssetModel.AssetModelNodeBuilder()
                    .WithName("Root")
                    .WithId("1")
                    .Build();

                await assetModelManager.AddOrUpdateNodeAsync(root).ConfigureAwait(false);

                var child = new AssetModel.AssetModelNodeBuilder()
                    .WithName("Child")
                    .WithId("2")
                    .WithParent("1")
                    .Build();

                await assetModelManager.AddOrUpdateNodeAsync(child).ConfigureAwait(false);

                var grandchild = new AssetModel.AssetModelNodeBuilder()
                    .WithName("Grandchild")
                    .WithId("3")
                    .WithParent("2")
                    .Build();

                await assetModelManager.AddOrUpdateNodeAsync(grandchild).ConfigureAwait(false);
            }

            var annotationManager = adapter.GetFeature<RealTimeData.IReadTagValueAnnotations>().Unwrap() as RealTimeData.InMemoryTagValueAnnotationManager;
            if (annotationManager != null) {
                await annotationManager.CreateOrUpdateAnnotationAsync(
                    TestTagId,
                    new RealTimeData.TagValueAnnotationBuilder()
                        .WithId(TestAnnotationId)
                        .WithValue("This is a test")
                        .WithUtcStartTime(DateTime.UtcNow.AddMinutes(-30))
                        .WithType(RealTimeData.AnnotationType.Instantaneous)
                        .Build(),
                    default
                ).ConfigureAwait(false);
            }
        }

    }
}
