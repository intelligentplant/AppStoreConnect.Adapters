using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.AspNetCoreExample {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            // Register our adapter as a singleton.
            services.AddSingleton<IAdapter, ExampleAdapter>();

            // Add adapter services
            services.AddDataCoreAdapterServices(options => {
                options.HostInfo = new Common.Models.HostInfo(
                    "Example Host",
                    "An example App Store Connect Adapters host",
                    GetType().Assembly.GetName().Version.ToString(),
                    new Common.Models.VendorInfo("Intelligent Plant", new Uri("https://appstore.intelligentplant.com")),
                    new Dictionary<string, string>() {
                        { "Project URL", "https://github.com/intelligentplant/app-store-connect-adapters" }
                    }
                );

                // To add authentication and authorization options for adapter API operations, extend 
                // the FeatureAuthorizationHandler class and call options.UseFeatureAuthorizationHandler
                // to register your handler.

                //options.UseFeatureAuthorizationHandler<MyAdapterFeatureAuthHandler>();
            });

            // Adapter API controllers require the API versioning service.
            services.AddApiVersioning(options => {
                options.ReportApiVersions = true;
            });

            // Add the adapter API controllers to the MVC registration.
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddDataCoreAdapterMvc();

            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapDataCoreAdapterHubs();
            });
        }
    }
}
