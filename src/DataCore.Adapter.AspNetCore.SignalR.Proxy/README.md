# DataCore.Adapter.AspNetCore.SignalR.Proxy

Proxy adapter that connects to a remote adapter via ASP.NET Core SignalR.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Proxy](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Proxy).


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
