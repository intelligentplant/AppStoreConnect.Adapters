# Intelligent Plant App Store Connect Adapters

Industrial processes generate a variety of different types of data, such as real-time instrument readings, messages from alarm & event systems, and so on. Real-time data typically needs to be archived to a time-based data store for long-term storage, from where it can be visualized, aggregated, and monitored to ensure that the process remains healthy.

Intelligent Plant's [Industrial App Store](https://appstore.intelligentplant.com) securely connects your industrial plant historian and alarm & event systems to apps using [App Store Connect](https://appstore.intelligentplant.com/Welcome/AppProfile?appId=a73c453df5f447a6aa8a08d2019037a5). App Store Connect comes with built-in drivers for many 3rd party systems (including AVEVA OSIsoft PI and Asset Framework, AVEVA Wonderware, Microsoft Azure IoT Hubs/Event Hubs, Modbus, MQTT, OPC UA, OPC Classic DA/HDA/AE, and more). App Store Connect can also integrate with 3rd party systems using an adapter.

## What is an Adapter?

An adapter is a component can expose real-time process data and/or alarm & event data to App Store Connect. This data can then be used by apps such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e). It can also be recorded to App Store Connect's Edge Historian if the source system does not support historical data queries.

Different systems expose different features. For example, an MQTT broker can be used to transmit sensor readings from IoT devices to interested subscribers, but it is not capable of long-term storage of these readings, or of performing ad hoc aggregation of the data (e.g. find the average value of instrument X at 1 hour intervals over the previous calendar day). Some alarm & event systems may allow historical querying of event messages, where as others (such as OPC AE) can only emit ephemeral event messages. Some systems may be read-only, others may be write-only, and some may allow data to flow in both directions. An adapter allows you to integrate with a system (or multiple systems), and expose the system's capabilities as simple, discrete features. 

Typically, an ASP.NET Core application is used to host and run one or more adapters, which App Store Connect can then query via an HTTP- or SignalR-based API. gRPC-based hosting and client packages are also defined, but require that App Store Connect is running on Windows 11 or Windows Server 2022 or higher.


# Getting Started

Install the project template for creating App Store Connect adapters for Microsoft Visual Studio and the [dotnet new](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new) command by following the instructions [here](./src/DataCore.Adapter.Templates).

Using the project template, you can easily create a working adapter in just a couple of minutes. See [here](./docs/writing-an-adapter.md) for information about writing an adapter.


## Example Adapter Implementations

The repository contains the following adapter implementations that can be used for reference when writing your own adapters:

* [Wave Generator](./src/DataCore.Adapter.WaveGenerator) - an adapter that can generate sinusoid, sawtooth, triangle, and square waves.
* [CSV](./src/DataCore.Adapter.Csv) - an adapter that uses CSV files to serve real-time and historical tag values.
* [HTTP](./src/DataCore.Adapter.Http.Proxy) - an adapter that connects to a remote adapter via the HTTP API, optionally using the SignalR API for subscriptions.
* [SignalR Proxy](./src/DataCore.Adapter.AspNetCore.SignalR.Proxy) - an adapter that connects to a remote adapter via the SignalR API.
* [gRPC Proxy](./src/DataCore.Adapter.Grpc.Proxy) - an adapter that connects to a remote adapter via the gRPC API.

A tutorial explaining how to build a simple MQTT adapter and host can also be found [here](./docs/tutorials/mqtt-adapter).


## Testing

A [library](./src/DataCore.Adapter.Tests.Helpers) is available to simplify testing standard adapter features using MSTest ([NuGet package](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Tests.Helpers)).


## Templates

A [templates package](./src/DataCore.Adapter.Templates) for Visual Studio and [dotnet new](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new) can be used to simplify creation of new adapters.


## Example Projects

The [examples](./examples) folder contains example host and client applications.


# NuGet Package References

Package versions are defined using [Central Package Management](https://devblogs.microsoft.com/nuget/introducing-central-package-management/) in [Directory.Packages.props](./Directory.Packages.props).


# Writing an Adapter

See [here](./docs/writing-an-adapter.md) for detailed information about writing an adapter.


# Authentication and Authorization

See [here](./docs/adapter-host-authn-authz.md) for detailed information about configuring authentication and authorization.

App Store Connect can authenticate with adapter hosts via client certificate, Windows Authentication, or bearer token. Bearer tokens can be issued by App Store Connect itself (using a shared secret to sign tokens) or by Azure Active Directory.

App Store Connect applies its own authorization before dispatching queries to an adapter, so a given Industrial App Store user will only be able to access data if they have been granted the appropriate permissions in App Store Connect.

You can also implement authorization in the adapter host (or directly in the adapter itself) to restrict access to certain features or operations. See the link at the start of this section for more details about this.


# Building

Run [build.ps1](./build.ps1) or [build.sh](./build.sh) to bootstrap and build the solution using [Cake](https://cakebuild.net/).

Signing of assemblies (by specifying the `--sign-output` flag when running the build script) requires additional bootstrapping not provided by this repository. A hint is provided to MSBuild that output should be signed by setting the `SignOutput` build property to `true`.


# Software Bill of Materials

To generate a Software Bill of Materials (SBOM) for the repository in [CycloneDX](https://cyclonedx.org/) XML format, run [build.ps1](./build.ps1) or [build.sh](./build.sh) with the `--target BillOfMaterials` parameter.

The resulting SBOM is written to the `artifacts/bom` folder.

> The CycloneDX tool makes calls to GitHub's API to retrieve licence information for referenced packages. GitHub enforces strict [rate limits](https://docs.github.com/en/rest/overview/resources-in-the-rest-api#rate-limiting) on unauthenticated requests. To make authenticated API requests using a GitHub username and personal access token, specify the `--github-username` and `--github-token` parameters when running the `BillOfMaterials` target.
