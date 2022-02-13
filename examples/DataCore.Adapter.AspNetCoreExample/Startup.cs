using System;
using DataCore.Adapter.Example;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

[assembly: DataCore.Adapter.VendorInfo("Intelligent Plant", "https://appstore.intelligentplant.com")]

namespace DataCore.Adapter.AspNetCoreExample {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddLocalization();

            // Add adapter services

            var version = GetType().Assembly.GetName().Version.ToString(3);

            services
                .AddDataCoreAdapterAspNetCoreServices()
                .AddHostInfo(
                    "Example .NET Core Host",
                    "An example App Store Connect Adapters host running on ASP.NET Core",
                    version,
                    properties: new [] {
                        Common.AdapterProperty.Create(
                            "Project URL",
                            new Uri("https://github.com/intelligentplant/AppStoreConnect.Adapters"),
                            "GitHub repository URL for the project"
                        )
                    }
                )
                .AddServices(svc => {
                    svc.AddSingleton<Events.InMemoryEventMessageStoreOptions>();
                    // Bind CSV adapter options against the application configuration.
                    svc.Configure<Csv.CsvAdapterOptions>(Configuration.GetSection("CsvAdapter:sensor-csv"));
                })
                .AddAdapter<ExampleAdapter>()
                .AddAdapter(sp => ActivatorUtilities.CreateInstance<WaveGenerator.WaveGeneratorAdapter>(sp, "wave-generator", new WaveGenerator.WaveGeneratorAdapterOptions() {
                    Name = "Wave Generator",
                    Description = "Generates tag values using wave generator functions",
                    EnableAdHocGenerators = true
                }))
                .AddAdapter(sp => {
                    // Create CSV adapter.
                    var adapter = ActivatorUtilities.CreateInstance<Csv.CsvAdapter>(
                        sp, 
                        "sensor-csv", 
                        sp.GetRequiredService<IOptions<Csv.CsvAdapterOptions>>()
                    );

                    // Add in-memory event message management
                    adapter.AddStandardFeatures(
                        ActivatorUtilities.CreateInstance<Events.InMemoryEventMessageStore>(sp, sp.GetService<ILogger<Csv.CsvAdapter>>())    
                    );

                    return adapter;
                });
                //.AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();

            // To add authentication and authorization for adapter API operations, extend 
            // the FeatureAuthorizationHandler class and call AddAdapterFeatureAuthorization
            // above to register your handler.

            services.AddGrpc();

            services.AddMvc()
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.WriteIndented = true;
                })
                .AddDataCoreAdapterMvc();

            services
                .AddSignalR()
                .AddDataCoreAdapterSignalR();

            services
                .AddHealthChecks()
                .AddAdapterHealthChecks();

            // Add OpenTelemetry tracing
            services.AddOpenTelemetryTracing(builder => {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddDataCoreAdapterApiService(System.Net.Dns.GetHostName()))
                    .AddAspNetCoreInstrumentation()
                    .AddDataCoreAdapterInstrumentation()
                    .AddJaegerExporter();
                    //.AddConsoleExporter();
            });

            services.AddOpenApiDocument(options => {
                options.DocumentName = "v2.0";
                options.Title = "App Store Connect Adapters";
                options.Description = "HTTP API for querying an App Store Connect adapters host.";
                options.Version = "2.0.0";
                options.AddOperationFilter(context => {
                    // Don't include the legacy routes.
                    return !context.OperationDescription.Path.StartsWith("/api/data-core/");
                });
                options.OperationProcessors.Add(new NSwag.Generation.Processors.OperationProcessor(context => { 
                    string RemoveWhiteSpace(string s) {
                        return s.Replace("\n", "").Trim();
                    }

                    if (context.OperationDescription.Operation.Summary != null) {
                        context.OperationDescription.Operation.Summary = RemoveWhiteSpace(context.OperationDescription.Operation.Summary);
                    }

                    if (context.OperationDescription.Operation.Description != null) {
                        context.OperationDescription.Operation.Description = RemoveWhiteSpace(context.OperationDescription.Operation.Description);
                    }

                    foreach (var parameter in context.OperationDescription.Operation.Parameters) {
                        if (parameter.Description == null) {
                            continue;
                        }
                        parameter.Description = RemoveWhiteSpace(parameter.Description);
                    }

                    if (context.OperationDescription.Operation.RequestBody?.Description != null) {
                        context.OperationDescription.Operation.RequestBody.Description = RemoveWhiteSpace(context.OperationDescription.Operation.RequestBody.Description);
                    }

                    foreach (var response in context.OperationDescription.Operation.Responses.Values) {
                        if (response.Description == null) {
                            continue;
                        }
                        response.Description = RemoveWhiteSpace(response.Description);
                    }

                    return true;
                }));
            });
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
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRequestLocalization();
            app.UseRouting();

            app.UseEndpoints(endpoints => {
                endpoints.MapDataCoreGrpcServices();
                endpoints.MapControllers();
                endpoints.MapDataCoreAdapterHubs();
                endpoints.MapHealthChecks("/health");

                // Redirect all other requests to the host info API endpoint.
                endpoints.MapFallback("/{*url}", context => {
                    context.Response.Redirect($"/api/app-store-connect/v2.0/host-info/");
                    return System.Threading.Tasks.Task.CompletedTask;
                });
            });
        }
    }
}
