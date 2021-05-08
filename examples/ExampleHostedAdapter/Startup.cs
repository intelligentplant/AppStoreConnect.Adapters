using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ExampleHostedAdapter {

    /// <summary>
    /// ASP.NET Core startup class.
    /// </summary>
    public class Startup {

        /// <summary>
        /// The ID of the hosted adapter.
        /// </summary>
        public const string AdapterId = "fdb421d7-03b2-49e8-880a-224e8e5f04ef";


        /// <summary>
        /// The <see cref="IConfiguration"/> for the application.
        /// </summary>
        public IConfiguration Configuration { get; }


        /// <summary>
        /// Creates a new <see cref="Startup"/> object.
        /// </summary>
        /// <param name="configuration">
        ///   The <see cref="IConfiguration"/> for the application.
        /// </param>
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }


        /// <summary>
        /// Configures application services.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/> to register the services with.
        /// </param>
        /// <remarks>
        ///   This method gets called by the ASP.NET Core runtime. Use this method to add services 
        ///   to the container. For more information on how to configure your application, visit 
        ///   https://go.microsoft.com/fwlink/?LinkID=398940
        /// </remarks>
        public void ConfigureServices(IServiceCollection services) {
            // Add adapter services

            var version = GetType().Assembly.GetName().Version.ToString(3);

            services
                .AddDataCoreAdapterAspNetCoreServices()
                .AddHostInfo("ExampleHostedAdapter Host", "A host application for an App Store Connect adapter")
                .AddServices(svc => {
                    // Bind adapter options against the application configuration.
                    svc.Configure<ExampleHostedAdapterOptions>(AdapterId, Configuration.GetSection($"AppStoreConnect:Settings:{AdapterId}"));
                })
                // Register our adapter with the DI container. The AdapterId parameter will be
                // used as the adapter ID.
                .AddAdapter(sp => ActivatorUtilities.CreateInstance<ExampleHostedAdapter>(sp, AdapterId));
            //.AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();

            // To add authentication and authorization for adapter API operations, extend 
            // the FeatureAuthorizationHandler class and call AddAdapterFeatureAuthorization
            // above to register your handler.

            services.AddGrpc();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.WriteIndented = true;
                })
                .AddDataCoreAdapterMvc();

            services.AddSignalR().AddDataCoreAdapterSignalR();

            // Add OpenTelemetry tracing. Refer to https://github.com/open-telemetry/opentelemetry-dotnet 
            // for more information about configuration and usage.
            services.AddOpenTelemetryTracing(builder => {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                        "IAS Adapter Host",
                        serviceVersion: version,
                        serviceInstanceId: AdapterId
                    ))
                    .AddAspNetCoreInstrumentation() // Records incoming HTTP requests made to the adapter host.
                    .AddHttpClientInstrumentation() // Records outgoing HTTP requests made by the adapter host.
                    .AddSqlClientInstrumentation() // Records queries made by System.Data.SqlClient and Microsoft.Data.SqlClient.
                    .AddDataCoreAdapterInstrumentation() // Records activities created by adapters and adapter hosting packages.
                    .AddJaegerExporter(); // Exports traces to Jaeger (https://www.jaegertracing.io/) using default settings.
            });
        }


        /// <summary>
        /// Configures the ASP.NET Core HTTP request pipeline.
        /// </summary>
        /// <param name="app">
        ///   The <see cref="IApplicationBuilder"/> for the application.
        /// </param>
        /// <param name="env">
        ///   The <see cref="IWebHostEnvironment"/> for the application.
        /// </param>
        /// <remarks>
        ///   This method gets called by the ASP.NET Core runtime. Use this method to configure 
        ///   the HTTP request pipeline.
        /// </remarks>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                // The default HSTS value is 30 days. You may want to change this for production
                // scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRequestLocalization();
            app.UseRouting();

            app.UseEndpoints(endpoints => {
                // Map API, SignalR, and gRPC endpoints.
                endpoints.MapControllers();
                endpoints.MapDataCoreAdapterHubs();
                endpoints.MapDataCoreGrpcServices();

                // Redirect all other requests to the API endpoint for retrieving details about the
                // adapter.
                endpoints.MapFallback("/{*url}", context => {
                    context.Response.Redirect($"/api/app-store-connect/v2.0/adapters/{AdapterId}");
                    return Task.CompletedTask;
                });
            });
        }
    }
}
