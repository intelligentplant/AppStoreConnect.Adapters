using DataCore.Adapter.WaveGenerator;

using Microsoft.AspNetCore.Http.Json;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

[assembly: DataCore.Adapter.VendorInfo("Intelligent Plant", "https://appstore.intelligentplant.com")]

const string AdapterId = "$default";

var builder = WebApplication.CreateBuilder(args);

// Parent PID. If specified, we will gracefully shut down if the parent process exits.
var pid = builder.Configuration.GetValue<int>("AppStoreConnect:Adapter:Host:ParentPid");
if (pid > 0) {
    builder.Services.AddDependentProcessWatcher(pid);
}

builder.Services
    .AddLocalization()
    .AddProblemDetails();

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddDataCoreAdapterApiServices()
    .AddHostInfo(
        name: "ASP.NET Core Minimal API Example",
        description: "Example ASP.NET Core adapter host using minimal API syntax"
     )
    .AddAdapterOptions<WaveGeneratorAdapterOptions>(options => options.Bind(builder.Configuration.GetSection("AppStoreConnect:Adapter:Settings")))
    .AddAdapter<WaveGeneratorAdapter>(AdapterId);

// Pretty-print JSON responses when running in development mode.
if (builder.Environment.IsDevelopment()) {
    builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.WriteIndented = true);
}

builder.Services
    .AddSignalR()
    .AddDataCoreAdapterSignalR();

builder.Services
    .AddGrpc()
    .AddDataCoreAdapterGrpc();

builder.Services
    .AddHealthChecks()
    .AddAdapterHealthChecks();

var otelResourceBuilder = ResourceBuilder.CreateDefault()
    .AddDataCoreAdapterApiService();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(otel => otel
        .SetResourceBuilder(otelResourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddDataCoreAdapterInstrumentation()
        .AddJaegerExporter())
    .WithMetrics(otel => otel
        .SetResourceBuilder(otelResourceBuilder)
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddDataCoreAdapterInstrumentation()
        .AddPrometheusExporter());
    
var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

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

app.MapDataCoreAdapterApiRoutes();
app.MapDataCoreAdapterHubs();
app.MapDataCoreGrpcServices();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.MapFallback("/{*url}", context => {
    context.Response.Redirect($"/api/app-store-connect/v2.0/host-info/");
    return Task.CompletedTask;
});

app.Run();
