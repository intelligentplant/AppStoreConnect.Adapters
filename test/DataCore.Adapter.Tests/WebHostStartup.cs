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
                .AddDataCoreAdapterAspNetCoreServices()
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
                    svc.AddSingleton<Diagnostics.ConfigurationChangesOptions>();
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

                    // Add configuration change notifier.
                    adapter.AddStandardFeatures(
                        ActivatorUtilities.CreateInstance<Diagnostics.ConfigurationChanges>(sp, sp.GetService<ILogger<Csv.CsvAdapter>>())
                    );

                    // Add ping-pong extension
                    adapter.AddExtensionFeatures(new PingPongExtension(adapter.BackgroundTaskService, sp.GetServices<Common.IObjectEncoder>()));

                    return adapter;
                });
            //.AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();

            // To add authentication and authorization for adapter API operations, extend 
            // the FeatureAuthorizationHandler class and call AddAdapterFeatureAuthorization
            // above to register your handler.

#if NETCOREAPP
            services.AddGrpc();
#endif

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
#if NETCOREAPP
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
#endif
                .AddDataCoreAdapterMvc();

#if NETCOREAPP
            services
                .AddSignalR()
                .AddDataCoreAdapterSignalR()
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });
#endif

            services
                .AddHealthChecks()
                .AddAdapterHealthChecks();
        }


#if NETCOREAPP
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
#else
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env) {
#endif
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRequestLocalization();

#if NETCOREAPP
            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapDataCoreGrpcServices();
                endpoints.MapControllers();
                endpoints.MapDataCoreAdapterHubs();
                endpoints.MapHealthChecks("/health");
            });
#else
            app.UseMvc();
#endif
        }
    
    }
}
