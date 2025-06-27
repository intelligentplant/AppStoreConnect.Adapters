# Industrial App Store Data Adapter Unit Tests: Example.Adapter

This Industrial App Store data adapter project uses a [starter template](https://github.com/intelligentplant/AppStoreConnect.Adapters/src/DataCore.Adapter.Templates) from the [Industrial App Store](https://appstore.intelligentplant.com). 

This project is an MSTest project that tests your [adapter implementation](../Example.Adapter).


# Getting Started

The `RngAdapterTests` class defines the test cases for your adapter. Standard tests are inherited from a base class provided by the data adapter toolkit, and you can add your own tests to this class.

The base class defines various `virtual` methods that you can override to define the input data used for various tests, such as the tags to search for, the tag values to return, and the expected results of the tests.
