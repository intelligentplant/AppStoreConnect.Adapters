using System.Collections.Generic;
using DataCore.Adapter.Example;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DataCore.Adapter.NetFxExample {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            // Register our adapter as a singleton.
            services.AddSingleton<IAdapter, ExampleAdapter>();

            // Add adapter services
            services.AddDataCoreAdapterServices(options => {
                options.HostInfo = new Common.Models.HostInfo(
                    "Example .NET Framework Host",
                    "An example App Store Connect Adapters host running on ASP.NET Core 2.2 on .NET Framework",
                    GetType().Assembly.GetName().Version.ToString(),
                    new Common.Models.VendorInfo("Intelligent Plant", "https://appstore.intelligentplant.com"),
                    new Dictionary<string, string>() {
                        { "Project URL", "https://github.com/intelligentplant/app-store-connect-adapters" }
                    }
                );

                // To add authentication and authorization options for adapter API operations, extend 
                // the FeatureAuthorizationHandler class and call options.UseFeatureAuthorizationHandler
                // to register your handler.

                //options.UseFeatureAuthorizationHandler<MyAdapterFeatureAuthHandler>();
            });

            // Add the adapter API controllers to the MVC registration.
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddDataCoreAdapterMvc();

            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseSignalR(route => {
                route.MapDataCoreAdapterHubs();
            });
        }
    }
}
