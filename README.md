# Intelligent Plant App Store Connect Adapters

Industrial processes generate a variety of different types of data, such as real-time instrument readings, messages from alarm & event systems, and so on. Real-time data typically needs to be archived to a time-based data store for long-term storage, from where it can be visualized, aggregated, and monitored to ensure that the process remains healthy.

Intelligent Plant's [Industrial App Store](https://appstore.intelligentplant.com) securely connects your industrial plant historian and alarm & event systems to apps using [App Store Connect](https://appstore.intelligentplant.com/Welcome/AppProfile?appId=a73c453df5f447a6aa8a08d2019037a5). App Store Connect comes with built-in drivers for many 3rd party systems (including OSIsoft PI and Asset Framework, OPC DA/HDA/AE, MQTT, and more). App Store Connect can also integrate with 3rd party systems using an adapter.

## What is an Adapter?

An adapter is a component can expose real-time process data and/or alarm & event data to App Store Connect. This data can then be used by apps such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e).

Different systems expose different features. For example, an MQTT broker can be used to transmit sensor readings from IoT devices to interested subscribers, but it is not capable of long-term storage of these readings, or of performing ad hoc aggregation of the data (e.g. find the average value of instrument X at 1 hour intervals over the previous calendar day). Some alarm & event systems may allow historical querying of event messages, where as others (such as OPC AE) can only emit ephemeral event messages. Some systems may be read-only, others may be write-only, and some may allow data to flow in both directions. An adapter allows you to integrate with a system (or multiple systems), and expose the system's capabilities as simple, discrete features. 

Typically, an ASP.NET Core application is used to host and run one or more adapters, which App Store Connect can then query via an HTTP- or SignalR-based API. gRPC-based hosting and client packages are also defined, but cannot currently be used with App Store Connect.


# Getting Started

Install the project template for creating App Store Connect adapters for Visual Studio 2022, Visual Studio 2019, and the [dotnet new](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new) command by following the instructions [here](./src/DataCore.Adapter.Templates).

Using the project template, you can easily create a working adapter in just a couple of minutes. See [here](./docs/writing-an-adapter.md) for information about writing an adapter.


# Summary of Projects

Some of the core projects in the repository are:

* `DataCore.Adapter.Core` ([source](./src/DataCore.Adapter.Core)) - a library containing request and response types used by adapters.
* `DataCore.Adapter.Abstractions` ([source](./src/DataCore.Adapter.Abstractions)) - a library that describes adapters themselves, and the features that they can expose.
* `DataCore.Adapter` ([source](./src/DataCore.Adapter)) - a library that contains base classes and utility classes for simplifying the implementation of adapters and adapter features.
* `DataCore.Adapter.DependencyInjection` ([source](./src/DataCore.Adapter.DependencyInjection)) - types and extension methods for `Microsoft.Extensions.DependencyInjection`, to simplify the registration of required services with a dependency injection container.
* `DataCore.Adapter.Json.Newtonsoft` ([source](./src/DataCore.Adapter.Json.Newtonsoft)) - converters for model types for the `Newtonsoft.Json` JSON serializer.
* `DataCore.Adapter.KeyValueStore.FASTER` ([source](./src/DataCore.Adapter.KeyValueStore.FASTER)) - a key-value store for an adapter that uses [Microsoft FASTER](https://microsoft.github.io/FASTER/) to efficiently store data.

## Hosting

The following projects provide support for hosting adapters in ASP.NET Core applications:

* `DataCore.Adapter.AspNetCore.Common` ([source](./src/DataCore.Adapter.AspNetCore.Common)) - a library containing concrete implementations of various types to provide integration with ASP.NET Core 6.0 applications.
* `DataCore.Adapter.AspNetCore.HealthChecks` ([source](./src/DataCore.Adapter.AspNetCore.HealthChecks)) - allows adapter runtime states to be reported as ASP.NET Core [health checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks).
* `DataCore.Adapter.AspNetCore.Mvc` ([source](./src/DataCore.Adapter.AspNetCore.Mvc)) - a library containing API controllers for use with with ASP.NET Core 6.0 applications.
* `DataCore.Adapter.AspNetCore.SignalR` ([source](./src/DataCore.Adapter.AspNetCore.SignalR)) - a library containing SignalR hubs for use with with ASP.NET Core 6.0 applications.
* `DataCore.Adapter.AspNetCore.Grpc` ([source](./src/DataCore.Adapter.AspNetCore.Grpc)) - a library to assist with hosting adapter gRPC services in ASP.NET Core 6.0 applications.
* `DataCore.Adapter.OpenTelemetry` ([source](./src/DataCore.Adapter.OpenTelemetry)) - extensions related to creating OpenTelemetry-compatible tracing in applications that host adapters.

## Client Libraries

There are also projects that define client libraries capable of communicating with the ASP.NET Core endpoints described above:

* `DataCore.Adapter.AspNetCore.SignalR.Client` ([source](./src/DataCore.Adapter.AspNetCore.SignalR.Client)) - a strongly-typed client for querying remote adapters via ASP.NET Core SignalR.
* `DataCore.Adapter.Grpc.Client` ([source](./src/DataCore.Adapter.Grpc.Client)) - a strongly-typed client for querying remote adapters via gRPC.
* `DataCore.Adapter.Http.Client` ([source](./src/DataCore.Adapter.Http.Client)) - a strongly-typed client for querying remote adapters via the HTTP API.

Additionally, the [OpenAPI](https://swagger.io/) [swagger.json](/swagger.json) file can be used to create clients for the HTTP API, or imported into tools like [Postman](https://www.getpostman.com/) in order to test API calls.

## Adapter Implementations

The repository contains the following adapter implementations:

* `DataCore.Adapter.Csv` ([source](./src/DataCore.Adapter.Csv)) - a library containing an adapter that uses CSV files to serve real-time and historical tag values.
* `DataCore.Adapter.WaveGenerator` ([source](./src/DataCore.Adapter.WaveGenerator)) - an adapter that can generate sinusoid, sawtooth, triangle, and square waves.
* `DataCore.Adapter.AspNetCore.SignalR.Proxy` ([source](./src/DataCore.Adapter.AspNetCore.SignalR.Proxy)) - allows the creation of local proxy adapters that communicate with remote adapters using the strongly-typed SignalR client.
* `DataCore.Adapter.Grpc.Proxy` ([source](./src/DataCore.Adapter.Grpc.Proxy)) - allows the creation of local proxy adapters that communicate with remote adapters using the gRPC client.
* `DataCore.Adapter.Http.Proxy` ([source](./src/DataCore.Adapter.Http.Proxy)) - allows the creation of local proxy adapters that communicate with remote adapters using the HTTP client.
* `DataCore.Adapter.Proxy` ([source](./src/DataCore.Adapter.Proxy)) - shared types used in proxy implementation.

## Testing

The following libraries are available to assist with unit testing adapter implementations:

* `DataCore.Adapter.Tests.Helpers` ([source](./src/DataCore.Adapter.Tests.Helpers)) - base classes that provide MSTest unit tests for standard adapter features.

## Templates

* `DataCore.Adapter.Templates` ([source](./src/DataCore.Adapter.Templates)) - templates for `dotnet new` to provide quick start adapter implementations.

## Example Projects

The [examples](./examples) folder contains example host and client applications.


# NuGet Package References

Package versions are defined using [Central Package Management](https://devblogs.microsoft.com/nuget/introducing-central-package-management/) in [Directory.Packages.props](./Directory.Packages.props).


# Writing an Adapter

See [here](./docs/writing-an-adapter.md) for information about writing an adapter.


# Authentication

App Store Connect can authenticate with adapter hosts via bearer token, client certificate, or Windows authentication.


# Authorization

App Store Connect applies its own authorization before dispatching queries to an adapter, so a given Industrial App Store user will only be able to access data if they have been granted the appropriate permissions in App Store Connect.

To implement authorization in an ASP.NET Core host application, you can extend the [FeatureAuthorizationHandler](./src/DataCore.Adapter.AspNetCore.Common/Authorization/FeatureAuthorizationHandler.cs) class and register it when configuring adapter services at startup time. Your class then be automatically detected and used by the default `IAdapterAuthorizationService` implementation:

```csharp
services
    .AddDataCoreAdapterAspNetCoreServices()
    // - snip -
    .AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();
```

Additionally, all methods on adapter feature interfaces are passed an [IAdapterCallContext](./src/DataCore.Adapter.Abstractions/IAdapterCallContext.cs) object containing (among other things) the identity of the calling user. Adapters can apply their own custom authorization based on this information e.g. to apply per-tag authorization on historical tag data queries.


# Building

Run [build.ps1](./build.ps1) or [build.sh](./build.sh) to bootstrap and build the solution using [Cake](https://cakebuild.net/).

Signing of assemblies (by specifying the `--sign-output` flag when running the build script) requires additional bootstrapping not provided by this repository. A hint is provided to MSBuild that output should be signed by setting the `SignOutput` build property to `true`.


# Software Bill of Materials

To generate a Software Bill of Materials (SBOM) for the repository in [CycloneDX](https://cyclonedx.org/) XML format, run [build.ps1](./build.ps1) or [build.sh](./build.sh) with the `--target BillOfMaterials` parameter.

The resulting SBOM is written to the `artifacts/bom` folder.

> The CycloneDX tool makes calls to GitHub's API to retrieve licence information for referenced packages. GitHub enforces strict [rate limits](https://docs.github.com/en/rest/overview/resources-in-the-rest-api#rate-limiting) on unauthenticated requests. To make authenticated API requests using a GitHub username and personal access token, specify the `--github-username` and `--github-token` parameters when running the `BillOfMaterials` target.
