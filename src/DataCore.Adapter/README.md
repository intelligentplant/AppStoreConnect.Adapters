# DataCore.Adapter

This project contains core types for implementing an App Store Connect Adapter.


# Implementing an Adapter

All adapters implement the [IAdapter](./IAdapter.cs) interface. Each adapter implements a set of *features*, which are exposed via an [IAdapterFeaturesCollection](./IAdapterFeaturesCollection.cs). Individual features are defined as interfaces, and must inherit from [IAdapterFeature](./IAdapterFeature.cs). The [AdapterFeaturesCollection](./AdapterFeaturesCollection.cs) class provides a default implementation of `IAdapterFeaturesCollection` that can register and unregister features dynamically at runtime.

Adapter implementers can pick and choose which features they want to provide. For example, the `DataCore.Adapter.DataSource.Features` namespace defines interfaces for features related to real-time process data (searching for available tags, requesting snapshot tag values, performing various types of historical data queries, and so on). An individual adapter can implement features related to process data, alarm and event sources, and alarm and event sinks, as required. 


## Helper Classes

The project contains a number of helper classes to simplify adapter implementation. For example, if an adapter only natively supports the retrieval of [raw, unprocessed tag values](./DataSource/Features/IReadRawTagValues.cs), the [ReadHistoricalTagValuesHelper](./DataSource/Utilities/ReadHistoricalTagValuesHelper.cs) class can be used to provide support for [interpolated](./DataSource/Features/IReadInterpolatedTagValues.cs), [values-at-times](./DataSource/Features/IReadTagValuesAtTimes.cs), [plot](./DataSource/Features/IReadPlotTagValues.cs), and [aggregated](./DataSource/Features/IReadProcessedTagValues.cs) data queries.
