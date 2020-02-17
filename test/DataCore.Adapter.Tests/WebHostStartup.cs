using System.Net.Http;
using DataCore.Adapter.Json;
using IntelligentPlant.BackgroundTasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using GrpcNet = Grpc.Net;

namespace DataCore.Adapter.Tests {

    public class WebHostStartup {

        public const string DefaultUrl = "https://localhost:31415";

        public const string AdapterId = "sensor-csv";

        public const string TestTagId = "Sensor_001";

        public const string HttpClientName = "AdapterHttpClient";


        public IConfiguration Configuration { get; }


        public WebHostStartup(IConfiguration configuration) {
            Configuration = configuration;
        }

        
        public void ConfigureServices(IServiceCollection services) {
            services.AddHttpClient(HttpClientName).ConfigureHttpClient(client => {
                client.BaseAddress = new System.Uri(DefaultUrl + "/");
            });
            services.AddHttpClient<Http.Client.AdapterHttpClient>(HttpClientName);

            services.AddTransient(sp => {
                return GrpcNet.Client.GrpcChannel.ForAddress(DefaultUrl, new GrpcNet.Client.GrpcChannelOptions() { 
                    HttpClient = sp.GetService<IHttpClientFactory>().CreateClient(HttpClientName)
                });
            });

            // Register an in-memory event manager for use by our adapter. We register this 
            // separately just to that we can resolve it from test classes to insert required 
            // test data.
            services.AddSingleton<Events.InMemoryEventMessageManagerOptions>();
            services.AddSingleton<Events.InMemoryEventMessageManager>(sp => {
                return ActivatorUtilities.CreateInstance<Events.InMemoryEventMessageManager>(sp, sp.GetService<ILogger<Events.InMemoryEventMessageManager>>());
            });

            // Register our adapter as a singleton.

            services.AddSingleton<IAdapter, Csv.CsvAdapter>(sp => {
                var adapter = ActivatorUtilities.CreateInstance<Csv.CsvAdapter>(
                    sp, 
                    new Csv.CsvAdapterOptions() {
                        Id = AdapterId,
                        Name = "Sensor CSV",
                        Description = "CSV adapter with dummy sensor data",
                        IsDataLoopingAllowed = true,
                        GetCsvStream = () => GetType().Assembly.GetManifestResourceStream(GetType(), "DummySensorData.csv")
                    }
                );

                // Add in-memory event message management
                adapter.AddFeatures(sp.GetService<Events.InMemoryEventMessageManager>());

                return adapter;
            });

            // Add adapter services
            services.AddDataCoreAdapterServices(options => {
                options.HostInfo = Common.HostInfo.Create(
                    "Example .NET Core Host",
                    "An example App Store Connect Adapters host running on ASP.NET Core 3.0",
                    GetType().Assembly.GetName().Version.ToString(),
                    Common.VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
                    Common.AdapterProperty.Create("Project URL", "https://github.com/intelligentplant/app-store-connect-adapters")
                );
            });

            services.AddGrpc();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
                .AddDataCoreAdapterMvc();

            services.AddSignalR()
                .AddDataCoreAdapterSignalR()
                .AddMessagePackProtocol();
        }

        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapDataCoreGrpcServices();
                endpoints.MapControllers();
                endpoints.MapDataCoreAdapterHubs();
            });

            WebHostInitializer.ApplicationServices = app.ApplicationServices;
        }
    }
}
