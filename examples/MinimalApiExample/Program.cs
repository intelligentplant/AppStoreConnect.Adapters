using DataCore.Adapter.WaveGenerator;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

[assembly: DataCore.Adapter.VendorInfo("Intelligent Plant", "https://appstore.intelligentplant.com")]

const string AdapterId = "$default";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLocalization();

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo(
        name: "ASP.NET Core Minimal API Example",
        description: "Example ASP.NET Core adapter host using minimal API syntax"
     )
    .AddServices(svc => svc.Configure<WaveGeneratorAdapterOptions>(
        builder.Configuration.GetSection("AppStoreConnect:Adapter:Settings")
     ))
    .AddAdapter(sp => ActivatorUtilities.CreateInstance<WaveGeneratorAdapter>(sp, AdapterId));

builder.Services
    .AddMvc()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true)
    .AddDataCoreAdapterMvc();

builder.Services
    .AddSignalR()
    .AddDataCoreAdapterSignalR();

builder.Services
    .AddGrpc()
    .AddDataCoreAdapterGrpc();

builder.Services
    .AddHealthChecks()
    .AddAdapterHealthChecks();

builder.Services.AddOpenTelemetryTracing(otel => otel
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddDataCoreAdapterApiService())
    .AddAspNetCoreInstrumentation()
    .AddDataCoreAdapterInstrumentation()
    .AddJaegerExporter()
    .AddConsoleExporter());

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

app.MapFallback("/{*url}", context => {
    context.Response.Redirect($"/api/app-store-connect/v2.0/host-info/");
    return Task.CompletedTask;
});

app.Run();
