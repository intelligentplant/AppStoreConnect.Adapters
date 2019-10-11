# DataCore.Adapter.AspNetCore.SignalR

This project contains SignalR hubs for querying adapters in an ASP.NET Core application.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR).


# Registering Adapters

In most cases, adapters should be registered as singleton services with the ASP.NET Core service collection:

```csharp
services.AddSingleton<IAdapter, MyAdapter>();
```

If your application can dynamically add or remove adapters at runtime, you must handle the adapter lifetimes yourself.


# Registering Adapter Services

Adapter services must be added to the application in the `Startup.cs` file's `ConfigureServices` method. For example:

```csharp
// Configure adapter services
services.AddDataCoreAdapterServices(options => {
    // Host information metadata.
    options.HostInfo = HostInfo.Create(
        "My Host",
        "A brief description of the hosting application",
        "0.9.0-alpha", // SemVer v2
        VendorInfo.Create("Intelligent Plant", new Uri("https://appstore.intelligentplant.com")),
        new Dictionary<string, string>() {
            { "Project URL", "https://github.com/intelligentplant/app-store-connect-adapters" }
        }
    );

    // Register our feature authorization handler.
    options.UseFeatureAuthorizationHandler<MyFeatureAuthorizationHandler>();
});
```


# Registering Endpoints

The adapter hub endpoint is registered in the `Startup.cs` file's `Configure` method as follows:

```csharp
app.UseRouting();

app.UseEndpoints(endpoints => {
    endpoints.MapDataCoreAdapterHubs();
});
```

The adapter hub will be mapped to `/signalr/data-core/v1.0`.