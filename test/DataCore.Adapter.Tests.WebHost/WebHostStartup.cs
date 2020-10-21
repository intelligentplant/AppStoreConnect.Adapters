#if NETCOREAPP

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Tests {
    internal class WebHostStartup {

        public IConfiguration Configuration { get; }


        public WebHostStartup(IConfiguration configuration) {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }


        public void ConfigureServices(IServiceCollection services) {
            WebHostConfiguration.ConfigureDefaultServices(services);

            services.AddLocalization();

            // Add adapter services

            services
                .AddDataCoreAdapterServices()
                .AddHostInfo(
                    "Unit Test Standalone Host",
                    "Unit Test App Store Connect Adapters host running on ASP.NET Core",
                    GetType().Assembly.GetName().Version.ToString(),
                    Common.VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
                    true,
                    true,
                    new[] {
                        Common.AdapterProperty.Create(
                            "Project URL",
                            new Uri("https://github.com/intelligentplant/AppStoreConnect.Adapters"),
                            "GitHub repository URL for the project"
                        )
                    }
                )
                .AddServices(svc => {
                    svc.AddSingleton<Events.InMemoryEventMessageStoreOptions>();
                })
                .AddAdapter(sp => {
                    var adapter = ActivatorUtilities.CreateInstance<Csv.CsvAdapter>(sp, WebHostConfiguration.AdapterId, new Csv.CsvAdapterOptions() {
                        Name = "Sensor CSV",
                        Description = "CSV adapter with dummy sensor data",
                        IsDataLoopingAllowed = true,
                        GetCsvStream = () => GetType().Assembly.GetManifestResourceStream(GetType(), "DummySensorData.csv")
                    });

                    // Add in-memory event message management
                    adapter.AddStandardFeatures(
                        ActivatorUtilities.CreateInstance<Events.InMemoryEventMessageStore>(sp, sp.GetService<ILogger<Csv.CsvAdapter>>())
                    );

                    // Add dummy tag value writing.
                    adapter.AddStandardFeatures(new NullValueWrite());

                    // Add ping-pong extension
                    adapter.AddExtensionFeatures(new PingPongExtension(adapter));

                    return adapter;
                });
            //.AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();

            // To add authentication and authorization for adapter API operations, extend 
            // the FeatureAuthorizationHandler class and call AddAdapterFeatureAuthorization
            // above to register your handler.

            services.AddGrpc();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
                .AddDataCoreAdapterMvc();

            services
                .AddSignalR()
                .AddDataCoreAdapterSignalR()
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
                .AddMessagePackProtocol();

            services
                .AddHealthChecks()
                .AddAdapterHeathChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRequestLocalization();
            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapDataCoreGrpcServices();
                endpoints.MapControllers();
                endpoints.MapDataCoreAdapterHubs();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}

#endif
