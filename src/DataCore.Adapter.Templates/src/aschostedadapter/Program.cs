﻿using DataCore.Adapter.KeyValueStore.Sqlite;

using ExampleHostedAdapter;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// The [VendorInfo] attribute is used to add vendor information to the adapters in this assembly,
// as well as the host information for the application.
[assembly: DataCore.Adapter.VendorInfo("My Company", "https://my-company.com")]

var builder = WebApplication.CreateBuilder(args);

// Our adapter settings are stored in adaptersettings.json.
builder.Configuration.AddJsonFile(Constants.AdapterSettingsFilePath, false, true);

// Parent PID. If specified, we will gracefully shut down if the parent process exits.
var pid = builder.Configuration.GetValue<int>("AppStoreConnect:Adapter:Host:ParentPid");
if (pid > 0) {
    builder.Services.AddDependentProcessWatcher(pid);
}

// Host instance ID.
var instanceId = builder.Configuration.GetValue<string>("AppStoreConnect:Adapter:Host:InstanceId");
if (string.IsNullOrWhiteSpace(instanceId)) {
    instanceId = System.Net.Dns.GetHostName();
}

builder.Services.AddLocalization();
#if (UseMinimalApi)

// Allows failed requests to generate RFC 7807 responses.
builder.Services.AddProblemDetails();
#endif

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
#if (UseMinimalApi)
    .AddDataCoreAdapterApiServices()
#endif
    .AddHostInfo(hostInfo => hostInfo
        .WithName("My Adapter Host")
        .WithDescription("App Store Connect adapter host for My Adapter")
        .WithInstanceId(instanceId))
    // Add a SQLite-based key-value store service. This can be used by our adapter to persist data
    // between restarts.
    //
    // NuGet packages are also available for other store types, including file system and Microsoft
    // FASTER-based stores.
    .AddKeyValueStore(sp => {
        var path = Path.Combine(AppContext.BaseDirectory, "kvstore.db");
        var options = new SqliteKeyValueStoreOptions() {
            ConnectionString = $"Data Source={path};Cache=Shared"
        };

        return ActivatorUtilities.CreateInstance<SqliteKeyValueStore>(sp, options);
    })
    // Register the adapter options
    .AddAdapterOptions<MyAdapterOptions>(
        // The adapter will look for an instance of the options with a name that matches its ID.
        Constants.AdapterId,
        // Bind the adapter options against the application configuration and ensure that they are
        // valid at startup.
        opts => opts
            .Bind(builder.Configuration.GetSection("AppStoreConnect:Adapter:Settings"))
            .ValidateDataAnnotations()
            .ValidateOnStart())
    // Register the adapter. We specify the adapter ID as an additional constructor parameter
    // since this will not be supplied by the service provider.
    .AddAdapter<MyAdapter>(Constants.AdapterId);

#if (!UseMinimalApi)
// Register adapter MVC controllers. Adding MVC to the application also registers the services 
// required to serve the Razor Pages UI.
builder.Services.AddMvc()
    .AddDataCoreAdapterMvc();
#endif

// Register adapter SignalR hub.
builder.Services.AddSignalR()
    .AddDataCoreAdapterSignalR();

// Register adapter gRPC services.
builder.Services.AddGrpc()
    .AddDataCoreAdapterGrpc();

#if (UseMinimalApi)
// Register the Razor Pages services used by the UI.
builder.Services.AddRazorPages();
#endif

// Register adapter health checks. See https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
// for more information about ASP.NET Core health checks.
builder.Services.AddHealthChecks()
    .AddAdapterHealthChecks();

// Register OpenTelemetry services. This can be safely removed if not required.
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resourceBuilder => resourceBuilder.AddDataCoreAdapterApiService(instanceId))
    .WithTracing(otel => otel
        // Records incoming HTTP requests made to the adapter host.
        .AddAspNetCoreInstrumentation()
        // Records outgoing HTTP requests made by the adapter host.
        .AddHttpClientInstrumentation()
        // Records gRPC client requests made by the adapter host. 
        .AddGrpcClientInstrumentation()
        // Records queries made by System.Data.SqlClient and Microsoft.Data.SqlClient.
        .AddSqlClientInstrumentation()
        // Records activities created by adapters and adapter hosting packages.
        .AddDataCoreAdapterInstrumentation()
        // Exports traces in OTLP format using default settings (i.e. http://localhost:4317 using OTLP/gRPC format).
        .AddOtlpExporter())
    .WithMetrics(otel => otel
        // Observe instrumentation for the .NET runtime.
        .AddRuntimeInstrumentation()
        // Observe ASP.NET Core instrumentation.
        .AddAspNetCoreInstrumentation()
        // Observe HTTP client instrumentation.
        .AddHttpClientInstrumentation()
        // Observe instrumentation generated by the adapter support libraries.
        .AddDataCoreAdapterInstrumentation()
        // Exports metrics in Prometheus format using default settings. Prometheus metrics are
        // served via the scraping endpoint registered below.
        .AddPrometheusExporter());

// Build the app and the request pipeline.
var app = builder.Build();

#if (UseMinimalApi)
app.UseExceptionHandler();
app.UseStatusCodePages();
#endif

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

#if (UseMinimalApi)
app.MapDataCoreAdapterApiRoutes();
#else
app.MapControllers();
#endif
app.MapDataCoreAdapterHubs();
app.MapDataCoreGrpcServices();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapRazorPages();

// Fallback route that redirects to the UI home page
app.MapFallback("/{*url}", context => {
    context.Response.Redirect("/");
    return Task.CompletedTask;
});

app.Run();
