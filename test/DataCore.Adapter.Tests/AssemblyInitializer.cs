﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

#if NETCOREAPP
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AssemblyInitializer {

        public const string AdapterId = "sensor-csv";

        public static IServiceProvider ApplicationServices { get; private set; }

#if NETCOREAPP

        private static readonly CancellationTokenSource s_cleanupTokenSource = new CancellationTokenSource();

        private static IDisposable s_webHost;

        private static Task s_webHostTask;


        [AssemblyInitialize]
        public static void Init(TestContext testContext) {
            if (s_webHost != null) {
                return;
            }

            var webHost = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder<WebHostStartup>(Array.Empty<string>())
                .UseUrls(WebHostConfiguration.DefaultUrl)
                .UseKestrel(options => {
                    options.ConfigureEndpointDefaults(listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
                })
                .ConfigureServices(services => AddAdapterServices(services))
                .Build();
            ApplicationServices = webHost.Services;
            webHost.Start();
            s_webHostTask = webHost.WaitForShutdownAsync(s_cleanupTokenSource.Token);
            s_webHost = webHost;
        }


        [AssemblyCleanup]
        public static async Task Cleanup() {
            s_cleanupTokenSource.Cancel();
            if (s_webHost is Process p) {
                p.Kill();
            }

            if (s_webHostTask != null) {
                await s_webHostTask.ConfigureAwait(false);
            }
            s_webHost?.Dispose();
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
                        ActivatorUtilities.CreateInstance<Events.InMemoryEventMessageStore>(sp, sp.GetService<ILogger<Csv.CsvAdapter>>())
                    );

                    // Add dummy tag value writing.
                    adapter.AddStandardFeatures(new NullValueWrite());

                    // Add configuration change notifier.
                    var configurationChanges = ActivatorUtilities.CreateInstance<Diagnostics.ConfigurationChanges>(sp, sp.GetService<ILogger<Csv.CsvAdapter>>());
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

                    // Add custom functions.
                    adapter.AddStandardFeatures(
                        ActivatorUtilities.CreateInstance<Extensions.CustomFunctions>(sp, adapter.TypeDescriptor.Id, sp.GetService<ILogger<Extensions.CustomFunctions>>())
                    );

                    // Add ping-pong extension
                    adapter.AddExtensionFeatures(new PingPongExtension(adapter.BackgroundTaskService, sp.GetServices<Common.IObjectEncoder>()));

                    adapter.Started += OnAdapterStarted;

                    return adapter;
                });
        }


        private static async Task OnAdapterStarted(IAdapter adapter) {
            var assetModelManager = adapter.GetFeature<AssetModel.IAssetModelBrowse>() as AssetModel.AssetModelManager;
            if (assetModelManager == null) {
                return;
            }

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

    }
}
