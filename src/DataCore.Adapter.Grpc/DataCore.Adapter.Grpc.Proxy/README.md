# DataCore.Adapter.Grpc.Proxy

Proxy adapter that connects to a remote adapter via gRPC.


# Creating a Proxy Instance

```csharp
var options = new GrpcAdapterProxyOptions() {
	AdapterId = "{SOME_ADAPTER_ID}"
};

// OPTION 1: Use Grpc.Core channel
var channel = new Grpc.Core.Channel("localhost:5000", Grpc.Core.ChannelCredentials.Insecure);
var proxy = await GrpcAdapterProxy.Create(channel, options, cancellationToken);

// OPTION 2: Use HttpClient (.NET Core 3.0+)
var httpClient = new System.Net.HttpClient() {
    BaseAddress = new Uri("http://localhost:5000")
};
var proxy = await GrpcAdapterProxy.Create(httpClient, options, cancellationToken);
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
