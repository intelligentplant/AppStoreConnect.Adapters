# Intelligent Plant App Store Connect Adapters

Intelligent Plant's [Industrial App Store](https://appstore.intelligentplant.com) securely connects your industrial plant historian and alarm & event systems to apps using [App Store Connect](https://appstore.intelligentplant.com/Welcome/AppProfile?appId=a73c453df5f447a6aa8a08d2019037a5). App Store Connect comes with built-in drivers for many 3rd party systems (including OSIsoft PI and Asset Framework, OPC DA/HDA/AE, and more). App Store Connect can also integrate with 3rd party systems using an adapter.

An adapter is a component that exposes real-time process data and/or alarm & event data to App Store Connect. This data can then be used by apps such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e).

An ASP.NET Core application is used to host and run one or more adapters, which App Store Connect can then query via an HTTP-based API.

This repository contains the following projects:

* `DataCore.Adapter.Core` ([source](/src/DataCore.Adapter.Core) - a .NET Standard 2.0 library containing request and response types used by adapters.
* `DataCore.Adapter` ([source](/src/DataCore.Adapter)) - a .NET Standard 2.0 library that describes adapters themselves, and the features that they can expose.
* `DataCore.Adapter.Utilities` ([source](/src/DataCore.Adapter.Utilities)) - a .NET Standard 2.0 library that contains utility classes for simplifying the implementation of adapter features.
* `DataCore.Adapter.AspNetCore` ([source](/src/DataCore.Adapter.AspNetCore)) - a .NET Core library containing API controllers, SignalR hubs, and concrete implementations of various types to provide integration with ASP.NET Core 2.2 applications.
* `DataCore.Adapter.Grpc.Server` ([source](/src/DataCore.Adapter.Grpc/DataCore.Adapter.Grpc.Server)) - a .NET Standard 2.0 library containing C# implementations of services that can be used to expose adapters via [gRPC](https://grpc.io/).
* `DataCore.Adapter.Grpc.Client` ([source](/src/DataCore.Adapter.Grpc/DataCore.Adapter.Grpc.Client)) - a .NET Standard 2.0 library containing C# implementations of clients for querying adapters via [gRPC](https://grpc.io/).

The [examples](/examples) folder contains example host and client applications.


# ASP.NET Core Quick Start

1. Create a new ASP.NET Core 2.2 project.
2. Add a reference to `DataCore.Adapter.AspNetCore` to your project.
3. Implement an [IAdapter](/src/DataCore.Adapter/IAdapter.cs) that can communicate with the system you want to connect App Store Connect to.
4. If you want to apply custom authorization policies to the adapter or individual adapter features, extend the [FeatureAuthorizationHandler](/src/DataCore.Adapter.AspNetCore/Authorization/FeatureAuthorizationHandler.cs) class.
5. In your `Startup.cs` file, configure adapter services in the `ConfigureServices` method:

```csharp
// Register the adapter
services.AddSingleton<IAdapter, MyAdapter>();

// Configure adapter services
services.AddDataCoreAdapterServices(options => {
    // Host information metadata.
    options.HostInfo = new Common.Models.HostInfo(
        "My Host",
        "A brief description of the hosting application",
        "0.9.0-alpha", // SemVer v2
        new VendorInfo("Intelligent Plant", new Uri("https://appstore.intelligentplant.com")),
        new Dictionary<string, string>() {
            { "Project URL", "https://github.com/intelligentplant/app-store-connect-adapters" }
        }
    );

    // To authorization options for adapter API operations, extend 
    // the FeatureAuthorizationHandler class and call options.UseFeatureAuthorizationHandler
    // to register your handler.
    //options.UseFeatureAuthorizationHandler<MyFeatureAuthorizationHandler>();
});
	
// Adapter API controllers require the API versioning service.
services.AddApiVersioning(options => {
    options.ReportApiVersions = true;
});

// Add the adapter API controllers to the MVC registration.
services.AddMvc()
    .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
    .AddDataCoreAdapterMvc();
```

7. In your `Startup.cs` file, configure adapter SignalR hubs in the `Configure` method:

```csharp
app.UseSignalR(route => {
    route.MapDataCoreAdapterHubs();
});
```


# Testing API Calls

The repository contains a [Postman collection](/postman_collection.json) that you can use to test API calls to your host.


# Authentication

At the moment, only anonymous and Windows authentication is supported at the App Store Connect end. Other authentication types (e.g. OAuth2 authentication flows) will be supported in future.


# Authorization

App Store Connect applies its own authorization before dispatching queries to an adapter, so a given user will only be able to access data if they have been granted the appropriate permissions in App Store Connect.

Authorization can also be applied at the [adapter level](./src/DataCore.Adapter) and at the [API level](./src/DataCore.Adapter.AspNetCore).


# Development and Hosting Without .NET

If you want to write and host an adapter without using .NET, you can expose your adapter via [gRPC](https://grpc.io/). Protobuf definitions for the gRPC adapter services can be found [here](/src/DataCore.Adapter.Grpc/Protos).