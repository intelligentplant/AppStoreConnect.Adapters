# Industrial App Store Data Adapter: Example.Adapter

This Industrial App Store data adapter project uses a [starter template](https://github.com/intelligentplant/AppStoreConnect.Adapters/src/DataCore.Adapter.Templates) from the [Industrial App Store](https://appstore.intelligentplant.com). 

This project is a .NET class library that implements your adapter. The adapter is hosted using an ASP.NET Core project that can be found [here](../Example.Adapter.Host). You can connect the adapter to the Industrial App Store using App Store Connect.


# Getting Started

The `RngAdapter` and `RngAdapterOptions` classes define the data adapter and its runtime options respectively. You can change the names of these classes as you wish. The adapter implements snapshot tag value polling, and uses helper classes from the data adapter toolkit to implement tag search and snapshot tag value subscription features.

For information about how to implement adapter features, as well as example projects, please visit the [Industrial App Store data adapters repository on GitHub](https://github.com/intelligentplant/AppStoreConnect.Adapters).


# Unit Tests

A unit test project that uses MSTest is included in the solution to test the adapter implementation. You can view the test project [here](../Example.Adapter.Tests).
