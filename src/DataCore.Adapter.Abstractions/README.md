# DataCore.Adapter.Abstractions

This project contains core types and abstractions for implementing an App Store Connect Adapter. An adapter is a component that exposes real-time process data and/or alarm & event data to App Store Connect. This data can then be used by apps on the [Industrial App Store](https://appstore.intelligentplant.com) such as [Gestalt Trend](https://appstore.intelligentplant.com/Home/AppProfile?appId=3fbd54df59964243aa9cf4b3f04823f6) and [Alarm Analysis](https://appstore.intelligentplant.com/Home/AppProfile?appId=d2322b59ff334c97b49760e40000d28e).


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Abstractions](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Abstractions).


# Implementing an Adapter

All adapters implement the [IAdapter](./IAdapter.cs) interface. Each adapter implements a set of *features*, which are exposed via an [IAdapterFeaturesCollection](./IAdapterFeaturesCollection.cs). Individual features are defined as interfaces, and inherit from [IAdapterFeature](./IAdapterFeature.cs). The [AdapterBase&lt;T&gt;](/src/DataCore.Adapter/AdapterBaseT.cs) and [AdapterBase](/src/DataCore.Adapter/AdapterBase.cs) classes in the [DataCore.Adapter](/src/DataCore.Adapter) project provide abstract base classes that your adapter can inherit from with methods for registering and unregistering adapter features dynamically.

Adapter implementers can pick and choose which features they want to provide. For example, the `DataCore.Adapter.RealTimeData` namespace defines interfaces for features related to real-time process data (searching for available tags, requesting snapshot tag values, performing various types of historical data queries, and so on). An individual adapter can implement features related to process data, alarm and event sources, and alarm and event sinks, as required.

Every feature defines a URI that uniquely identifies the feature. URIs for well-known features are defined [here](./WellKnownFeatures.cs).


## Standard Features

Adapters can implement any number of the following standard feature interfaces:

- Asset Model:
    - [IAssetModelBrowse](./AssetModel/IAssetModelBrowse.cs)
    - [IAssetModelSearch](./AssetModel/IAssetModelSearch.cs)
- Diagnostics:
    - [IHealthCheck](./Diagostics/IHealthCheck.cs)
- Tags:
    - [ITagInfo](./Tags/ITagInfo.cs)
    - [ITagSearch](./Tags/ITagSearch.cs)
- Events:
    - [IEventMessagePush](./Events/IEventMessagePush.cs)
    - [IEventMessagePushWithTopics](./Events/IEventMessagePushWithTopics.cs)
    - [IReadEventMessagesForTimeRange](./Events/IReadEventMessagesForTimeRange.cs)
    - [IReadEventMessagesUsingCursor](./Events/IReadEventMessagesUsingCursor.cs)
    - [IWriteEventMessages](./Events/IWriteEventMessages.cs)
- Real-Time Data:
    - [IReadPlotTagValues](./RealTimeData/IReadPlotTagValues.cs)
    - [IReadProcessedTagValues](./RealTimeData/IReadProcessedTagValues.cs)
    - [IReadRawTagValues](./RealTimeData/IReadRawTagValues.cs)
    - [IReadSnapshotTagValues](./RealTimeData/IReadSnapshotTagValues.cs)
    - [IReadTagValueAnnotations](./RealTimeData/IReadTagValueAnnotations.cs)
    - [IReadTagValuesAtTimes](./RealTimeData/IReadTagValuesAtTimes.cs)
    - [ISnapshotTagValuePush](./RealTimeData/ISnapshotTagValuePush.cs)
    - [IWriteHistoricalTagValues](./RealTimeData/IWriteHistoricalTagValues.cs)
    - [IWriteSnapshotTagValues](./RealTimeData/IWriteSnapshotTagValues.cs)
    - [IWriteTagValueAnnotations](./RealTimeData/IWriteTagValueAnnotations.cs)

## Extension Features

In addition to standard features that inherit from [IAdapterFeature](./IAdapterFeature.cs), adapter implementers can also define extension features on their adapters. Extension features must inherit from [IAdapterExtensionFeature](./Extensions/IAdapterExtensionFeature.cs) and must be annotated using the [ExtensionFeatureAttribute](./Extensions/ExtensionFeatureAttribute.cs) attribute, with a relative feature URI e.g.

```csharp
[ExtensionFeature(
    "com.myco/my-example", 
    Name = "My Example", 
    Description = "An example extension feature"
)]
public interface IMyExampleExtensionFeature : IAdapterExtensionFeature {
    
    [ExtensionFeatureOperation(typeof(MyExampleExtensionFeatureProvider), nameof(GetGreetDescriptor))]
    string Greet(IAdapterCallContext context, string name);

}


internal static class MyExampleExtensionFeatureProvider {
    
    internal static ExtensionFeatureOperationDescriptorPartial GetGreetDescriptor() {
        return new ExtensionFeatureOperationDescriptorPartial() {
            Name = "Greet",
            Description = "Greets a person",
            Inputs = new[] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    Ordinal = 0,
                    VariantType = VariantType.String,
                    Description = "The name of the person to greet"
                }
            },
            Outputs = new[] {
                new ExtensionFeatureOperationParameterDescriptor() {
                    Ordinal = 0,
                    VariantType = VariantType.String,
                    Description = "The greeting"
                }
            }
        };
    }

}
```

> *NOTE:* The absolute URI for an extension feature will be relative to `WellKnownFeatures.Extensions.ExtensionFeatureBasePath` e.g. the relative URI `com.myco/my-example` will be transformed into `asc:extensions/com.myco/my-example/`. It is also allowed to specified an absolute URI if it is a child path of `WellKnownFeatures.Extensions.ExtensionFeatureBasePath`

The [ExtensionFeatureOperationAttribute](./Extensions/ExtensionFeatureOperationAttribute.cs) attribute can be used to annotate extension feature operations. This information can then be used when building the list of operation descriptors for a feature.

In order to simplify implementation of non-standard adapter features, the [AdapterExtensionFeature](/src/DataCore.Adapter/Extensions/AdapterExtensionFeature.cs) base class in the [DataCore.Adapter](/src/DataCore.Adapter) project can be used as the basis of an extension feature implementation. This class offers a number of `BindInvoke`, `BindStream`, and `BindDuplexStream` method overloads to automatically register methods on the implementation to be handled by the `Invoke`, `Stream` or `DuplexStream` method defined by `IAdapterExtensionFeature` as appropriate.


## Working with IAsyncEnumerable<T>

Adapter features make extensive use of the `IAsyncEnumerable<T>` type, to allow query results to be streamed back to the caller asynchronously. For .NET Framework and .NET Standard 2.0 targets, the [Microsoft.Bcl.AsyncInterfaces](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/) NuGet package is used to define the type.

> NOTE: In order to produce `IAsyncEnumerable<T>` instances from iterator methods, or to consume `IAsyncEnumerator<T>` instances using `await foreach` loops, your project must use C# 8.0 or higher.

In most cases, it is advisable to declare a feature method using the `async` keyword, and to use `yield return` statements to emit values as they occur. For example:

```csharp
public class MyAdapter : AdapterBase, IReadSnapshotTagValues {

    // -- other code removed for brevity --

    async IAsyncEnumerable<ChannelReader<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
        IAdapterCallContext context, 
        ReadSnapshotTagValuesRequest request, 
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        ValidateInvocation(context, request);

        // CreateCancellationTokenSource is defined on AdapterBase<TOptions>, and is used to 
        // create CancellationTokenSource instances that will cancel when the adapter is stopped, 
        // in addition to when any cancellation tokens passed to the method are cancelled.
        using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
            await foreach (var item in GetSnapshotValues(request.Tags, ctSource.Token).ConfigureAwait(false)) {
                yield return item;
            }
        }
    }

    private IAsyncEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
        ...
    }

}
```

If your implementation runs synchronously (e.g. if the return values are held in an in-memory collection), you can use `Task.Yield` to make the implementation asynchronous:

```csharp
public class MyAdapter : AdapterBase, IReadSnapshotTagValues {

    // -- other code removed for brevity --

    async IAsyncEnumerable<ChannelReader<TagValueQueryResult> IReadSnapshotTagValues.ReadSnapshotTagValues(
        IAdapterCallContext context, 
        ReadSnapshotTagValuesRequest request, 
        [EnumeratorCancellation]
        CancellationToken cancellationToken
    ) {
        ValidateInvocation(context, request);

        await Task.Yield();

        using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
            foreach (var item in GetSnapshotValues(request.Tags, ctSource.Token)) {
                yield return item;
            }
        }
    }

    private IEnumerable<TagValueQueryResult> GetSnapshotValues(IEnumerable<string> tags, CancelationToken cancellationToken) {
        ...
    }

}
```


## Helper Classes

The [DataCore.Adapter](/src/DataCore.Adapter) project contains a number of helper classes to simplify adapter implementation. For example, if an adapter only natively supports the retrieval of [raw, unprocessed tag values](./RealTimeData/IReadRawTagValues.cs), the [ReadHistoricalTagValues](/src/DataCore.Adapter/RealTimeData/ReadHistoricalTagValues.cs) class can be used to provide support for [values-at-times](./RealTimeData/IReadTagValuesAtTimes.cs), [plot](./RealTimeData/IReadPlotTagValues.cs), and [aggregated](./RealTimeData/IReadProcessedTagValues.cs) data queries.


# Implementing Authorization

All calls to adapter features include information about the caller (via the [IAdapterCallContext](./IAdapterCallContext.cs) interface).

Alternatively, you can implement the [IAdapterAuthorizationService](./IAdapterAuthorizationService.cs) and use it in your host application to pre-authorize access to individual adapters and features.

When hosting adapters in an ASP.NET Core application, you can use the ASP.NET Core authorization model to control access to adapters and features. This approach is described [here](/src/DataCore.Adapter.AspNetCore.Common).
