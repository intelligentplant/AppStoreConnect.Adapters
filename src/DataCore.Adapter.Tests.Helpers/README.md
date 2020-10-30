# DataCore.Adapter.Tests.Helpers

This project defines base MSTest classes that can be used to add stock unit tests for an App Store Connect adapter to a unit test project.


## Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Tests.Helpers](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Tests.Helpers) to your MSTest project.


## Usage

Add a test class to your unit test project that inherits from [AdapterTestsBase<TAdapter>](./AdapterTestsBase.cs) (remember to annotate your test class with `[TestClass]`!). Implement the `CreateServiceScope` and `CreateAdapter` methods. You can also optionally override the `CreateCallContext` method if you need to customise the `IAdapterCallContext` that will be passed to the adapter during tests.

`AdapterTestsBase<TAdapter>` defines tests for all standard adapter features. If your adapter does not support a given feature, tests for that feature will be marked as inconclusive.

Different tests also require the implementation of different supporting methods. If you run a test without overridding the associated support method(s), the test will fail, and the failure message will highlight which method you need to override. The vast majority of the support methods are named `CreateXXX`, where `XXX` refers to the type of test input that the method creates.

Most of the built-in test methods are declared as `virtual`, so you can override these methods in your test class if required (e.g. to perform some additional test bootstrapping).
