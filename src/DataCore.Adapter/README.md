# DataCore.Adapter

Contains base classes and utility classes for implementing an [IAdapter](/src/DataCore.Adapter.Abstractions/IAdapter.cs).

Extend from [AdapterBase](./AdapterBase.cs) for easy implementation or [AdapterBase&lt;T&gt;](./AdapterBaseT.cs) if you need to supply configurable options to your adapter.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter).


# Creating an Adapter

See [/docs/writing-an-adapter.md](here) for information about writing an adapter using the base classes in this project.
