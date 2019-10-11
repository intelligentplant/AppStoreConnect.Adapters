# DataCore.Adapter.Csv

An example App Store Connect [adapter](/src/DataCore.Adapter.Abstractions/IAdapter.cs) implementation that uses loops over a CSV file to provide tag data.

The adapter options are defined via the [CsvAdapterOptions](./CsvAdapterOptions.cs) class. The `CsvFile` or `GetCsvStream` properties on the class are used by the adapter to obtain a CSV stream to parse.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Csv](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Csv).


# CSV Format

The CSV file must define a header that contains the time stamp field and the names of the tags in the file. The time stamp field is assumed to be the first field in the header, but can be customised via the `TimeStampFieldIndex` property on the [CsvAdapterOptions](./CsvAdapterOptions.cs) class. It does not matter what name is given to the field.

## Tag Definition Fields

The remaining fields in the header are used to configure the tag definitions. The field can be defined in one of the following formats:

1. `<tag name>`
2. `[<property 1>=<value 1>|...|<property N>=<value N>]`

In the first case, the tag name and ID will both be set to the field name; other tag properties will be set to their default values (i.e. a numeric tag with no units, description, or discrete states defined).

In the second case, the tag's configuration is defined using a set of `property=value` pairs that are separated using the pipe (`|`) character. The entire configuration must be enclosed in square brackets. The following properties can be specified:

- `name`
- `id`
- `description`
- `units`
- `dataType` (value can be one of `numeric` or `text`, and is ignored if discrete state properties are defined; see below for details)

At least one of the `name` or `id` properties must be defined. If only one of the properties is defined, it will be re-used for the other identification property.

It is also possible to specify discrete states for the tag, by prefixing additional properties with `state_`. The value of the state properties *must* be parsable to an integer. For example, to define NORMAL and ALARM states with values of 0 and 1 respectively:

    [name=My_Tag|id=1|description=Just a test|state_NORMAL=0|state_ALARM=1]

## Localization

The `CultureInfo` used for parsing time stamps and numeric values can be configured via the `CultureInfo` property on the [CsvAdapterOptions](./CsvAdapterOptions.cs) class. If undefined, `CultureInfo.CurrentCulture` will be used.

## Time Stamp Format

The time stamp format can be specified via the `TimeStampFormat` property on the [CsvAdapterOptions](./CsvAdapterOptions.cs) class. If undefined, the time stamp will be parsed using the configured `CultureInfo` in the options.

## Time Zone

All time stamps are converted to UTC internally. If the UTC offset can be inferred from the time stamp (e.g. `2019-07-07T06:35:30+0300`) it will be used automatically. If no offset is supplied, the time stamp is assumed to be in the machine's local time unless the `TimeZone` property on the [CsvAdapterOptions](./CsvAdapterOptions.cs) class is set. The value of the setting can be any time zone identifier that can be resolved via `TimeZoneInfo.FindSystemTimeZoneById`.
