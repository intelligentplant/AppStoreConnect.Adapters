#if NETCOREAPP
using System;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

            services.AddMvc()
                .AddDataCoreAdapterMvc();

            services
                .AddSignalR(options => options.EnableDetailedErrors = true)
                .AddDataCoreAdapterSignalR();

            services
                .AddHealthChecks()
                .AddAdapterHealthChecks();
        }


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
