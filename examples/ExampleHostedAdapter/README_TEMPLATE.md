# App Store Connect Adapter Host: ExampleHostedAdapter

This App Store Connect adapter uses a [starter template](https://github.com/intelligentplant/AppStoreConnect.Adapters/src/DataCore.Adapter.Templates) from the [Industrial App Store](https://appstore.intelligentplant.com). The adapter is hosted by an ASP.NET Core application. You can connect an App Store Connect instance to your adapter via HTTP or gRPC.


# Getting Started

The `ExampleHostedAdapter` and `ExampleHostedAdapterOptions` classes define the adapter and its runtime options respectively. You can change the names of these classes as you wish. The `ExampleHostedAdapter` class is split across three separate code files (`ExampleHostedAdapter.cs`, `ExampleHostedAdapter.TagSearch.cs`, and `ExampleHostedAdapter.ReadSnapshotTagValues.cs`) and implements tag search and real-time value polling features.

For information about how to implement adapter features, as well as example projects, please visit the [App Store Connect adapters GitHub repository](https://github.com/intelligentplant/AppStoreConnect.Adapters).

The `Startup` class configures the dependency injection container and application pipeline for the ASP.NET Core application. The `appsettings.json` file provides configuration settings for the application, including the `ExampleHostedAdapterOptions` instance that is passed to the `ExampleHostedAdapter` instance at runtime.


# Unit Tests

Helpers are available for running adapter tests in MSTest unit test projects, via the [IntelligentPlant.AppStoreConnect.Adapter.Tests.Helpers](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Tests.Helpers) NuGet package.

See [here](https://github.com/intelligentplant/AppStoreConnect.Adapters/src/DataCore.Adapter.Tests.Helpers) for more information.


# Connecting App Store Connect to the Adapter

To connect a local App Store Connect instance to your adapter, configure a new `App Store Connect Adapter (HTTP Proxy)` data source in the App Store Connect UI, using the following settings:

- `Address`: https://localhost:44300/
- `Adapter ID`: fdb421d7-03b2-49e8-880a-224e8e5f04ef
