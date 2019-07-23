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
using System.IO;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.GrpcExampleServer {
    public class Startup {
        

        public void ConfigureServices(IServiceCollection services) {
            // Register our adapters.

            services.AddSingleton<IAdapter, Csv.CsvAdapter>(sp => {
                return new Csv.CsvAdapter(
                    new Common.Models.AdapterDescriptor("sensor-csv", "Sensor CSV", "CSV adapter with dummy sensor data"),
                    new Csv.CsvAdapterOptions() {
                        IsDataLoopingAllowed = true,
                        GetCsvStream = () => new FileStream(Path.Combine(AppContext.BaseDirectory, "DummySensorData.csv"), FileMode.Open)
                    },
                    sp.GetRequiredService<ILogger<Csv.CsvAdapter>>()
                );
            });

            services.AddSingleton<IAdapter, Csv.CsvAdapter>(sp => {
                return new Csv.CsvAdapter(
                    new Common.Models.AdapterDescriptor("acoustic-probe-csv", "Acoustic Probe CSV", "CSV adapter with dummy acoustic probe data"),
                    new Csv.CsvAdapterOptions() {
                        IsDataLoopingAllowed = true,
                        GetCsvStream = () => new FileStream(Path.Combine(AppContext.BaseDirectory, "DummyAcousticProbeData.csv"), FileMode.Open)
                    },
                    sp.GetRequiredService<ILogger<Csv.CsvAdapter>>()
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
