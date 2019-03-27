# Intelligent Plant App Store Connect Adapters

Intelligent Plant's [Industrial App Store](https://appstore.intelligentplant.com) securely connects your industrial plant historian and alarm & event systems to apps using [App Store Connect](https://appstore.intelligentplant.com/Welcome/AppProfile?appId=a73c453df5f447a6aa8a08d2019037a5). App Store Connect comes with built-in drivers for many 3rd party systems (including OSIsoft PI and Asset Framework, OPC DA/HDA/AE, and more). App Store Connect can also integrate with 3rd party systems using App Store Connect Adapters, and query them as if they were e.g. industrial plant historians. An ASP.NET Core application is used to host and run one or more adapters, which App Store Connect can then query via an HTTP-based API.

The repository contains the following projects:

* `DataCore.Adapter` ([source](/src/DataCore.Adapter)) - a .NET Standard 2.0 library that contains interfaces and data transfer object definitions used by adapters.
* `DataCore.Adapter.AspNetCore` ([source](/src/DataCore.Adapter.AspNetCore)) - a .NET Core library containing API controllers, and concrete implementations of various types to provide integration with ASP.NET Core 2.2 applications.
* `DataCore.Adapter.AspNetCoreExample` ([source](/src/DataCore.AspNetCoreExample)) - an ASP.NET Core 2.2 web application that hosts an in-memory data source that uses a looping data set to serve up sensor-like data.


# ASP.NET Core Quick Start

1. Create a new ASP.NET Core 2.2 project.
2. Add a reference to `DataCore.Adapter.AspNetCore` to your project.
3. Implement an [IAdapter](/src/DataCore.Adapter/IAdapter.cs) that can communicate with the system you want to connect App Store Connect to.
4. Extend the [AdapterAccessor](/src/DataCore.Adapter.AspNetCore/AdapterAccessor.cs) class. If your adapter is registered as an `IHostedService`, you can use the built-in [HostedServiceAdapterAccessor](/src/DataCore.Adapter.AspNetCore/HostedServiceAdapterAccessor.cs) class instead.
5. If you want to apply custom authorization policies to the adapter or individual adapter features, extend the [FeatureAuthorizationHandler](/src/DataCore.Adapter.AspNetCore/Authorization/FeatureAuthorizationHandler.cs) class.
6. In your `Startup.cs` file, configure adapter services:

```csharp
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

    // Register our IAdapterAccessor class.
    options.UseAdapterAccessor<HostedServiceAdapterAccessor>();
            
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


# Testing API Calls

The repository contains a [Postman collection](/postman_collection.json) that you can use to test API calls to your host.


# Authentication

At the moment, only anonymous and Windows authentication is supported at the App Store Connect end. Other authentication types (e.g. OAuth2 authentication flows) will be supported in future.


# Authorization

App Store Connect applies its own authorization before dispatching queries to an adapter, so a given user will only be able to access data if they have been granted the appropriate permissions in App Store Connect.

Authorization can also be applied at the [adapter level](./DataCore.Adapter) and at the [API level](./DataCore.Adapter.AspNetCore).
