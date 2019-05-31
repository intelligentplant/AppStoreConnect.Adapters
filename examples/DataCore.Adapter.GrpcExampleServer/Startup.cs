using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataCore.Adapter.Grpc.Server.Services;

namespace DataCore.Adapter.GrpcExampleServer {
    public class Startup {
        

        public void ConfigureServices(IServiceCollection services) {
            // Register our adapter as a singleton.
            services.AddSingleton<IAdapter, ExampleAdapter>();

            // Add adapter services
            services.AddDataCoreAdapterServices(options => {
                options.HostInfo = new Common.Models.HostInfo(
                    "Example gRPC Host",
                    "An example App Store Connect Adapters gRPC host",
                    GetType().Assembly.GetName().Version.ToString(),
                    new Common.Models.VendorInfo("Intelligent Plant", new Uri("https://appstore.intelligentplant.com")),
                    new Dictionary<string, string>() {
                        { "Project URL", "https://github.com/intelligentplant/app-store-connect-adapters" }
                    }
                );
            });

            // Add gRPC
            services.AddGrpc();
        }

        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapGrpcService<HostInfoServiceImpl>();
                endpoints.MapGrpcService<AdaptersServiceImpl>();
                endpoints.MapGrpcService<TagSearchServiceImpl>();
                endpoints.MapGrpcService<TagValuesServiceImpl>();
                endpoints.MapGrpcService<EventsServiceImpl>();
            });
        }
    }
}
