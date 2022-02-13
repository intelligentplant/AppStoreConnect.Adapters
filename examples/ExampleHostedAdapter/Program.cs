using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// The [VendorInfo] attribute is used to add vendor information to the adapters in this assembly,
// as well as the host information for the application.
[assembly: DataCore.Adapter.VendorInfo("My Company", "https://my-company.com")]

// The ID of the hosted adapter.
const string AdapterId = "fdb421d7-03b2-49e8-880a-224e8e5f04ef";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLocalization();

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo(
        name: "ExampleHostedAdapter Host",
        description: "ASP.NET Core adapter host"
     )
    // Bind adapter options against the application configuration.
    .AddServices(svc => svc.Configure<ExampleHostedAdapter.ExampleHostedAdapterOptions>(
        AdapterId,
        builder.Configuration.GetSection("AppStoreConnect:Adapter:Settings")
     ))
    // Register the adapter.
    .AddAdapter(sp => ActivatorUtilities.CreateInstance<ExampleHostedAdapter.ExampleHostedAdapter>(sp, AdapterId));

// Register adapter MVC controllers.
builder.Services
    .AddMvc()
    .AddDataCoreAdapterMvc();

// Register adapter SignalR hub.
builder.Services
    .AddSignalR()
    .AddDataCoreAdapterSignalR();

// Register adapter gRPC services.
builder.Services
    .AddGrpc();

// Register adapter health checks.
builder.Services
    .AddHealthChecks()
    .AddAdapterHealthChecks();

// Register OpenTelemetry trace instrumentation. This can be safely removed if not required.
builder.Services.AddOpenTelemetryTracing(otel => otel
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddDataCoreAdapterApiService(AdapterId))
    .AddAspNetCoreInstrumentation() // Records incoming HTTP requests made to the adapter host.
    .AddHttpClientInstrumentation() // Records outgoing HTTP requests made by the adapter host.
    .AddSqlClientInstrumentation() // Records queries made by System.Data.SqlClient and Microsoft.Data.SqlClient.
    .AddDataCoreAdapterInstrumentation() // Records activities created by adapters and adapter hosting packages.
    .AddJaegerExporter()); // Exports traces to Jaeger (https://www.jaegertracing.io/) using default settings.

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}
else {
    // The default HSTS value is 30 days. You may want to change this for production
    // scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();

app.MapControllers();
app.MapDataCoreAdapterHubs();
app.MapDataCoreGrpcServices();
app.MapHealthChecks("/health");

// Fallback route that redirects to the host information API call. This can be safely removed if
// not required.
app.MapFallback("/{*url}", context => {
    context.Response.Redirect($"/api/app-store-connect/v2.0/host-info/");
    return Task.CompletedTask;
});

app.Run();
