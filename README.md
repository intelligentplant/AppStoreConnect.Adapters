# Intelligent Plant App Store Connect Adapters

Intelligent Plant's [Industrial App Store](https://appstore.intelligentplant.com) securely connects your industrial plant historian and alarm & event systems to apps using [App Store Connect](https://appstore.intelligentplant.com/Welcome/AppProfile?appId=a73c453df5f447a6aa8a08d2019037a5). App Store Connect comes with built-in drivers for many 3rd party systems (including OSIsoft PI and Asset Framework, OPC DA/HDA/AE, and more). App Store Connect can also integrate with 3rd party systems using an adapter.

An adapter is a component that exposes real-time process data and/or alarm & event data to App Store Connect. This data can then be used by apps such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e).

An ASP.NET Core application is used to host and run one or more adapters, which App Store Connect can then query via an HTTP- or SignalR-based API.

Some of the key projects in the repository are:

* `DataCore.Adapter.Core` ([source](/src/DataCore.Adapter.Core)) - a .NET Standard 2.0 library containing request and response types used by adapters.
* `DataCore.Adapter.Abstractions` ([source](/src/DataCore.Adapter.Abstractions)) - a .NET Standard 2.0 library that describes adapters themselves, and the features that they can expose.
* `DataCore.Adapter` ([source](/src/DataCore.Adapter)) - a .NET Standard 2.0 library that contains base classes and utility classes for simplifying the implementation of adapter features.
* `DataCore.Adapter.Csv` ([source](/src/DataCore.Adapter.Csv)) - a .NET Standard 2.0 library containing an adapter that uses CSV files to serve real-time and historical tag values.
* `DataCore.Adapter.Grpc.Server` ([source](/src/DataCore.Adapter.Grpc/DataCore.Adapter.Grpc.Server)) - a .NET Standard 2.0 library containing C# implementations of services that can be used to expose adapters via [gRPC](https://grpc.io/).
* `DataCore.Adapter.Grpc.Client` ([source](/src/DataCore.Adapter.Grpc/DataCore.Adapter.Grpc.Client)) - a .NET Standard 2.0 library containing C# implementations of clients for querying adapters via [gRPC](https://grpc.io/).

The following projects provide support for hosting adapters in ASP.NET Core applications:

* `DataCore.Adapter.AspNetCore.Common` ([source](/src/DataCore.Adapter.AspNetCore.Common)) - a .NET Core library containing concrete implementations of various types to provide integration with ASP.NET Core 3.0 applications.
* `DataCore.Adapter.AspNetCore.Mvc` ([source](/src/DataCore.Adapter.AspNetCore.Mvc)) - a .NET Core library containing API controllers for use with with ASP.NET Core 3.0 applications.
* `DataCore.Adapter.AspNetCore.SignalR` ([source](/src/DataCore.Adapter.AspNetCore.SignalR)) - a library containing SignalR hubs for use with with ASP.NET Core 2.2 and 3.0 applications.
* `DataCore.Adapter.AspNetCore.Grpc` ([source](/src/DataCore.Adapter.AspNetCore.Grpc)) - a library to assist with hosting adapter [gRPC](https://grpc.io/) services.

There are also projects that allow the creation of proxy adapters:

* `DataCore.Adapter.AspNetCore.SignalR.Proxy` ([source](/src/DataCore.Adapter.AspNetCore.SignalR.Proxy)) - a library that allows creation of proxy adapters that communicate with remote adapters via SignalR hubs.
* `DataCore.Adapter.Grpc.Proxy` ([source](/src/DataCore.Adapter.Grpc/DataCore.Adapter.Grpc.Proxy)) - a library that allows creation of proxy adapters that communicate with remote adapters via gRPC.

The [examples](/examples) folder contains example host and client applications.


# ASP.NET Core Quick Start

1. Create a new ASP.NET Core 3.0 project.
2. Add references to `DataCore.Adapter.AspNetCore.Mvc` and `DataCore.Adapter.AspNetCore.SignalR` to your project.
3. Implement an [IAdapter](/src/DataCore.Adapter.Abstractions/IAdapter.cs) that can communicate with the system you want to connect App Store Connect to. The [AdapterBase](/src/DataCore.Adapter/AdapterBase.cs) class provides an abstract base class that you can inherit from.
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
        new VendorInfo("Intelligent Plant", "https://appstore.intelligentplant.com"),
        new Dictionary<string, string>() {
            { "Project URL", "https://github.com/intelligentplant/app-store-connect-adapters" }
        }
    );

    // To add authorization options for adapter API operations, extend 
    // the FeatureAuthorizationHandler class and call options.UseFeatureAuthorizationHandler
    // to register your handler.
    //options.UseFeatureAuthorizationHandler<MyFeatureAuthorizationHandler>();
});
	
// Add the adapter API controllers to the MVC registration.
services.AddMvc()
    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
    .AddDataCoreAdapterMvc();
```

7. In your `Startup.cs` file, add adapter Web API controller and SignalR hub endpoints in the `Configure` method:

```csharp
app.UseRouting();

app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
    endpoints.MapDataCoreAdapterHubs();
});
```


# Testing API Calls

The repository contains a [Postman collection](/postman_collection.json) that you can use to test API calls to your host.


# Authentication

At the moment, only anonymous and Windows authentication is supported at the App Store Connect end. Other authentication types (e.g. OAuth2 authentication flows) will be supported in future.


# Authorization

App Store Connect applies its own authorization before dispatching queries to an adapter, so a given user will only be able to access data if they have been granted the appropriate permissions in App Store Connect.

The [IAdapterAuthorizationService](/src/DataCore.Adapter.Abstractions/IAdapterAuthorizationService.cs) service can be used to authorize access the individual adapters and adapter features; this authorization is automatically applied by the [ASP.NET Core](/src/DataCore.Adapter.AspNetCore) host on incoming calls to API or SignalR endpoints.

Additionally, all methods on adapter feature interfaces are passed an [IAdapterCallContext](/src/DataCore.Adapter.Abstractions/IAdapterCallContext.cs) object containing (among other things) the identity of the calling user. Adapters can apply their own custom authorization based on this information e.g. to apply per-tag authorization on tag data queries.


# Development and Hosting Without .NET

If you want to write and host an adapter without using .NET, you can expose your adapter via [gRPC](https://grpc.io/). Protobuf definitions for the gRPC adapter services can be found [here](/src/DataCore.Adapter.Grpc/Protos).