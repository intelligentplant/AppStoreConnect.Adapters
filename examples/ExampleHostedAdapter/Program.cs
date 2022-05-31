using DataCore.Adapter.KeyValueStore.Sqlite;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// The [VendorInfo] attribute is used to add vendor information to the adapters in this assembly,
// as well as the host information for the application.
[assembly: DataCore.Adapter.VendorInfo("My Company", "https://my-company.com")]

// The ID of the hosted adapter.
const string AdapterId = "$default";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLocalization();

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo(
        name: "ExampleHostedAdapter Host",
        description: "ASP.NET Core adapter host"
     )
    // Add a SQLite-based key-value store service. This can be used by our adapter to persist data
    // between restarts.
    //
    // NuGet packages are also available for other store types, including file system annd Microsoft
    // FASTER-based stores.
    .AddKeyValueStore(sp => {
        var path = Path.Combine(AppContext.BaseDirectory, "kvstore.db");
        var options = new SqliteKeyValueStoreOptions() {
            ConnectionString = $"Data Source={path};Cache=Shared"
        };

        return ActivatorUtilities.CreateInstance<SqliteKeyValueStore>(sp, options);
    })
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

// Register adapter health checks. See https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
// for more information about ASP.NET Core health checks.
builder.Services
    .AddHealthChecks()
    .AddAdapterHealthChecks();

// Register OpenTelemetry trace instrumentation. This can be safely removed if not required.
builder.Services.AddOpenTelemetryTracing(otel => otel
    // Specify an OpenTelemetry service instance ID in AddDataCoreAdapterApiService below to
    // override the use of the DNS host name for the local machine.
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddDataCoreAdapterApiService())
    // Records incoming HTTP requests made to the adapter host.
    .AddAspNetCoreInstrumentation()
    // Records outgoing HTTP requests made by the adapter host.
    .AddHttpClientInstrumentation()
    // Records queries made by System.Data.SqlClient and Microsoft.Data.SqlClient.
    .AddSqlClientInstrumentation()
    // Records activities created by adapters and adapter hosting packages.
    .AddDataCoreAdapterInstrumentation()
    // Exports traces to Jaeger (https://www.jaegertracing.io/) using default settings.
    .AddJaegerExporter());

// Build the app and the request pipeline.
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
