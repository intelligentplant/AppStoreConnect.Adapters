# Intelligent Plant App Store Connect Adapters

App Store Connect Adapters allow App Store Connect to integrate with 3rd party systems, and query them as if they were e.g. industrial plant historians. An ASP.NET Core application is used to host and run one or more adapters, which App Store Connect can then query via an HTTP-based API.

The repository contains the following projects:

* `DataCore.Adapter` - a .NET Standard 2.0 library that contains interfaces and data transfer object definitions used by adapters.
* `DataCore.Adapter.AspNetCore` - a .NET Core library containing API controllers, and concrete implementations of various types to provide integration with ASP.NET Core 2.2 applications.
* `DataCore.Adapter.AspNetCoreExample` - an ASP.NET Core 2.2 web application that hosts an in-memory data source that uses a looping data set to serve up sensor-like data.


# Implementing an Adapter

All adapters implement the `IAdapter` interface. Each adapter implements a set of __features__, which are exposed via an `IAdapterFeaturesCollection`. Individual features are defined as interfaces, and must inherit from `IAdapterFeature`. The `AdapterFeaturesCollection` class provides a default implementation of `IAdapterFeaturesCollection` that can register and unregister features dynamically at runtime.

Adapter implementers can pick and choose which features they want to provide. For example, the `DataCore.Adapter.DataSource.Features` namespace defines interfaces for features related to real-time process data (searching for available tags, requesting snapshot tag values, performing various types of historical data queries, and so on). An individual adapter can implement features related to process data, alarm and event sources, and alarm and event sinks, as required. 

In addition to implementing the `IAdapter` interface for individual adapters, the hosting application must also provide an implementation of the `IAdapterAccessor` service. This service is used to access adapters at runtime. The service can choose to control access to adapters based on the identity of the calling user.


## Helper Classes

The `DataCore.Adapter` project contains a number of helper classes to simplify adapter implementation. For example, if an adapter only natively supports the retrieval of raw, unprocessed tag values, the `DataCore.Adapter.DataSource.Utilities.ReadHistoricalTagValuesHelper` class can be used to provide support for interpolated, plot, and aggregated data queries.


# Implementing Authorization

The `IDataCoreContext` interface is passed as a parameter to all methods associated with an adapter feature. It can be used by the adapter to identify the calling user (and therefore authorize access to adapter functions and/or individual tags). Implementation of this interface is delegated to the platform-specific integration library (i.e. `DataCore.Adapter.AspNetCore`).

The identity associated with the calling user is determined by the type of authentication used in the hosting ASP.NET Core application. At the moment, only anonymous and Windows authentication is supported at the App Store Connect end. Other authentication types (e.g. OAuth2 authentication flows) will be supported in future.

Note also, that App Store Connect applies its own authorization before dispatching queries to an adapter, so a given user will only be able to access data if they have been granted the appropriate permissions in App Store Connect.


# Wiring up an ASP.NET Core Application Host

In the `ConfigureServices` method in your `Startup.cs` file, add the following code to register the required services:

    // Add adapter services, including our IAdapterAccessor implementation.
    services.AddDataCoreAdapterServices<MyAdapterAccessor>();

    // Adapter API controllers require the API versioning service.
    services.AddApiVersioning();

    // Add the adapter API controllers to the MVC registration.
    services.AddMvc()
        .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
        .AddDataCoreAdapterMvc();


The ASP.NET Core application is responsible for managing the lifecycle of its adapters. When registering the adapter services with the application, an `IAdapterAccessor` implementation must be provided. If your adapters are registered with ASP.NET Core as hosted services (that is, they implement `IHostedService` and are registered as hosted services at application startup), you can use the `HostedServiceAdapterAccessor` implementation. You can extend this class if you want to control access to adapters based on the identity of the calling user.

The `IAdapterAccessor` implementation is registered as a transient service (i.e. it calls `services.AddTransient<IAdapterAccessor, MyImplementation>` under the hood). Note that this means that a new instance of the service will be created every time it is resolved!
