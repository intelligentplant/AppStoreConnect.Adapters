#if NETCOREAPP
using System;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter.Tests {
    internal class WebHostStartup {


        public static void ConfigureServices(IServiceCollection services) {
            WebHostConfiguration.ConfigureDefaultServices(services);

            services.AddLocalization();

            // Add adapter services

            services
                .AddDataCoreAdapterAspNetCoreServices()
                .AddHostInfo(
                    "Unit Test Standalone Host",
                    "Unit Test App Store Connect Adapters host running on ASP.NET Core",
                    typeof(WebHostStartup).Assembly.GetName().Version.ToString(),
                    Common.VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
                    true,
                    true,
                    [
                        Common.AdapterProperty.Create(
                            "Project URL",
                            new Uri("https://github.com/intelligentplant/AppStoreConnect.Adapters"),
                            "GitHub repository URL for the project"
                        )
                    ]
                );

            services.AddSingleton<IAdapterLifetime>(new AdapterLifetime(async (adapter, ct) => {
                var customFunctions = adapter.GetFeature<Extensions.ICustomFunctions>().Unwrap() as Extensions.CustomFunctions;
                if (customFunctions != null) { 
                    await customFunctions.RegisterFunctionAsync<PingMessage, PongMessage>("Ping", null, (ctx, req, ct) => {
                        return Task.FromResult(new PongMessage() {
                            CorrelationId = req.CorrelationId,
                            UtcServerTime = DateTime.UtcNow
                        });
                    }, cancellationToken: ct);
                };
            }));

            services.AddGrpc();

#if NET7_0_OR_GREATER
            services.AddDataCoreAdapterApiServices();
#endif

            services.AddMvc()
                .AddDataCoreAdapterMvc();

            services
                .AddSignalR(options => options.EnableDetailedErrors = true)
                .AddDataCoreAdapterSignalR();

            services
                .AddHealthChecks()
                .AddAdapterHealthChecks();
        }


        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRequestLocalization();

            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapDataCoreGrpcServices();
#if NET7_0_OR_GREATER
                endpoints.MapDataCoreAdapterApiRoutes("/minimal-api/unit-tests");
#endif
                endpoints.MapControllers();
                endpoints.MapDataCoreAdapterHubs();
                endpoints.MapHealthChecks("/health");
            });
        }
    
    }
}
#endif
