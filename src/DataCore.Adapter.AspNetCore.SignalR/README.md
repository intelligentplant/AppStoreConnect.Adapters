# DataCore.Adapter.AspNetCore.SignalR

This project contains SignalR hubs for querying adapters in an ASP.NET Core application.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR).


# Registering Adapters and Adapter Services

Adapter services must be added to the application in the `Startup.cs` file's `ConfigureServices` method. For example:

```csharp
services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo(HostInfo.Create(
        "My Host",
        "A brief description of the hosting application",
        "0.9.0-alpha", // SemVer v2
        VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
        AdapterProperty.Create("Project URL", "https://github.com/intelligentplant/AppStoreConnect.Adapters")
    ))
    .AddAdapter<MyAdapter>()
    .AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();
```

In most cases, adapters can be registered using the `AddAdapter` extension method on the `IAdapterConfigurationBuilder` class, which registers them as singleton services. If your application can dynamically add or remove adapters at runtime, you must handle the adapter lifetimes yourself.


# Registering SignalR Services

To register additional services (such as required `System.Text.Json` converters), add the following to your SignalR registration:

```csharp
services.AddSignalR().AddDataCoreAdapterSignalR();
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