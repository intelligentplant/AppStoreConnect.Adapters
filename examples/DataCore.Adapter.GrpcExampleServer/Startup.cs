using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.GrpcExampleServer {
    public class Startup {
        

        public void ConfigureServices(IServiceCollection services) {
            // Register our adapters.

            services.AddSingleton<IAdapter, Csv.CsvAdapter>(sp => {
                return new Csv.CsvAdapter(
                    new Csv.CsvAdapterOptions() {
                        Id = "sensor-csv",
                        Name = "Sensor CSV",
                        Description = "CSV adapter with dummy sensor data",
                        IsDataLoopingAllowed = true,
                        CsvFile = "DummySensorData.csv"
                    },
                    sp.GetRequiredService<ILoggerFactory>()
                );
            });

            services.AddSingleton<IAdapter, Csv.CsvAdapter>(sp => {
                return new Csv.CsvAdapter(
                    new Csv.CsvAdapterOptions() {
                        Id = "acoustic-probe-csv",
                        Name = "Acoustic Probe CSV",
                        Description = "CSV adapter with dummy acoustic probe data",
                        IsDataLoopingAllowed = true,
                        CsvFile = "DummyAcousticProbeData.csv"
                    },
                    sp.GetRequiredService<ILoggerFactory>()
                );
            });

            // Add adapter services
            services.AddDataCoreAdapterServices(options => {
                options.HostInfo = new Common.Models.HostInfo(
                    "Example gRPC Host",
                    "An example App Store Connect Adapters gRPC host",
                    GetType().Assembly.GetName().Version.ToString(),
                    new Common.Models.VendorInfo("Intelligent Plant", "https://appstore.intelligentplant.com"),
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
                endpoints.MapDataCoreGrpcServices();
            });
        }
    }
}
