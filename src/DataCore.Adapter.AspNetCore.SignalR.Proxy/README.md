# DataCore.Adapter.AspNetCore.SignalR.Proxy

Proxy adapter that connects to a remote adapter via ASP.NET Core SignalR.


# Creating a Proxy Instance

```csharp
var options = new SignalRAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    RemoteId = "{SOME_ADAPTER_ID}",
    ConnectionFactory = key => {
        var hubRoute = key == null
            ? SignalRAdapterProxy.HubRoute
            : GetExtensionHubRoute(key);
        
        return new HubConnectionBuilder()
            .WithUrl($"http://localhost:5000" + hubRoute)
            .Build();
    }
};

var proxy = new SignalRAdapterProxy(Options.Create(options), NullLoggerFactory.Instance);
await proxy.StartAsync(cancellationToken);
```


# Using the Proxy

You can use the proxy as you would any other `IAdapter` instance:

```csharp
var readRaw = proxy.Features.Get<IReadRawTagValues>();

var now = DateTime.UtcNow;

var rawChannel = readRaw.ReadRawTagValues(null, new ReadRawTagValuesRequest() {
    Tags = new[] { "Sensor_001", "Sensor_002" },
    UtcStartTime = now.Subtract(TimeSpan.FromDays(7)),
    UtcEndTime = now,
    SampleCount = 0, // i.e. all raw values inside the time range
    BoundaryType = RawDataBoundaryType.Inside
}, cancellationToken);

while (await rawChannel.WaitToReadAsync()) {
    if (rawChannel.TryRead(out var val)) {
        DoSomethingWithValue(val);
    }
}
```

Note however, that the proxy's feature implementations ignore `IAdapterCallContext` objects that are passed into the feature's methods; it is safe to pass `null` for these parameters. The connection factory method should be used to directly set any required authentication properties on the hub connections.


# Adding Extension Feature Support

You can add support for adapter extension features by providing an `ExtensionFeatureFactory` delegate in the proxy options. This delegate will be invoked for every extension feature that the remote proxy reports that it supports:

```csharp
var options = new SignalRAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    AdapterId = "{SOME_ADAPTER_ID}",
    ConnectionFactory = key => {
        var hubRoute = key == null
            ? SignalRAdapterProxy.HubRoute
            : GetExtensionHubRoute(key);
            
        return new HubConnectionBuilder()
            .WithUrl($"http://localhost:5000" + hubRoute)
            .Build();
    },
    ExtensionFeatureFactory = (featureName, proxy) => {
        return GetFeatureImplementation(featureName, proxy);
    }
};
```


# Enabling MessagePack

[MessagePack](https://docs.microsoft.com/en-us/aspnet/core/signalr/messagepackhubprotocol) increases performance when e.g. querying for large data sets. After adding the [Microsoft.AspNetCore.SignalR.Protocols.MessagePack](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Protocols.MessagePack) NuGet package to your project, it can be added to hub connections inside the proxy's connection factory method:

```csharp
var options = new SignalRAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    AdapterId = "{SOME_ADAPTER_ID}",
    ConnectionFactory = key => {
        var hubRoute = key == null
            ? SignalRAdapterProxy.HubRoute
            : GetExtensionHubRoute(key);
        
        return new HubConnectionBuilder()
            .WithUrl($"http://localhost:5000" + hubRoute)
            .AddMessagePackProtocol()
            .Build();
    }
};
```
