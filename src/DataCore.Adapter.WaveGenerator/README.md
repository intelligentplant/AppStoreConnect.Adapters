# DataCore.Adapter.WaveGenerator

An example App Store Connect [adapter](/src/DataCore.Adapter.Abstractions/IAdapter.cs) implementation that generates tag values using wave generator functions.

The adapter options are defined via the [WaveGeneratorAdapterOptions](./WaveGeneratorAdapterOptions.cs) class.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.WaveGenerator](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.WaveGenerator).


# Getting Started

By default, the adapter defines a single tag for each of the supported wave types:

- `Sawtooth`
- `Sinusoid`
- `Square`
- `Triangle`

Using the `WaveGeneratorAdapterOptions` class, it is possible to define additional "built-in" tags via the `Tags` property. The `Tags` property is a dictionary that maps from a tag name to a string literal defining the properties of the wave generator.

## Wave Generator Options

Wave generator options are defined using a string that consists of semicolon-delimited `key=value` pairs, where each `key` is the case-insensitive name of a property on the [WaveGeneratorOptions](./WaveGeneratorOptions.cs) class (e.g. `Type`, `Amplitude`).

Examples:

- `Type=Sinusoid;Period=3600`
- `Type=Triangle;Amplitude=50;Phase=60;Offset=27.664`
- `type=square;amplitude=100`


# Tag Value Queries

Snapshot polling, snapshot subscriptions, raw history, and values-at-times queries are natively supported by the adapter. Plot and aggregated data queries are implemented using the [ReadHistoricalTagValues](/src/DataCore.Adapter/RealTimeData/ReadHistoricalTagValues.cs) helper class.

In addition to the built-in tags and the tags defined in the adapter options, callers can request values for ad hoc wave generators (defined using the syntax described above), if this is enabled via the `EnableAdHocGenerators` property on the `WaveGeneratorAdapterOptions` class.
