using DataCore.Adapter.KeyValueStore.Sqlite;

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// The [VendorInfo] attribute is used to add vendor information to the adapters in this assembly,
// as well as the host information for the application.
[assembly: DataCore.Adapter.VendorInfo("My Company", "https://my-company.com")]

var builder = WebApplication.CreateBuilder(args);

// Our adapter settings are stored in adaptersettings.json.
builder.Configuration
    .AddJsonFile(ExampleHostedAdapter.Constants.AdapterSettingsFilePath, false, true);

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
    .AddAdapterOptions<ExampleHostedAdapter.ExampleHostedAdapterOptions>(
        // The adapter will look for an instance of the options with a name that matches its ID.
        ExampleHostedAdapter.Constants.AdapterId,
        // Bind the adapter options against the application configuration and ensure that they are
        // valid at startup.
        opts => opts
            .Bind(builder.Configuration.GetSection("AppStoreConnect:Adapter:Settings"))
            .ValidateDataAnnotations()
            .ValidateOnStart()
    )
    // Register the adapter. We specify the adapter ID as an additional constructor parameter
    // since this will not be supplied by the service provider.
    .AddAdapter<ExampleHostedAdapter.ExampleHostedAdapter>(ExampleHostedAdapter.Constants.AdapterId);

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
    .AddGrpc()
    .AddDataCoreAdapterGrpc();

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapDataCoreAdapterHubs();
app.MapDataCoreGrpcServices();
app.MapHealthChecks("/health");
app.MapRazorPages();

// Fallback route that redirects to the UI home page
app.MapFallback("/{*url}", context => {
    context.Response.Redirect("/");
    return Task.CompletedTask;
});

app.Run();
