# DataCore.Adapter.Grpc.Proxy

Proxy adapter that connects to a remote adapter via gRPC.


# Creating a Proxy Instance

```csharp
var descriptor = new AdapterDescriptor("some-id", "some-name");

var options = new GrpcAdapterProxyOptions() {
	AdapterId = "{SOME_ADAPTER_ID}"
};

// OPTION 1: Use Grpc.Core channel
var channel = new Grpc.Core.Channel("localhost:5000", Grpc.Core.ChannelCredentials.Insecure);
var proxy = new GrpcAdapterProxy(descriptor, channel, options);
await proxy.StartAsync(cancellationToken);

// OPTION 2: Use HttpClient (requires .NET Core 3.0 due to lack of HTTP/2 support in earlier versions)
var httpClient = new System.Net.HttpClient() {
    BaseAddress = new Uri("http://localhost:5000")
};
var proxy = new GrpcAdapterProxy(descriptor, httpClient, options);
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


# Implementing Per-Call Authentication

gRPC supports both per-channel and per-call authentication. If the remote host supports (or requires) per-call authentication, you can configure this by setting the `GetCallCredentials` property in the `GrpcAdapterProxyOptions` object you pass to the proxy constructor. The property is a delegate that takes an `IAdapterCallContext` representing the calling user, and returns a `GrpcAdapterProxyCallCredentials` object that will be added to the headers of the outgoing gRPC request:

```csharp
var options = new GrpcAdapterProxyOptions() {
    AdapterId = "{SOME_ADAPTER_ID}",
    GetCallCredentials = async (IAdapterCallContext context) => {
        var accessToken = await GetAccessToken(context);
        return GrpcAdapterProxyCallCredentials.FromAccessToken(accessToken);
    }
};
```

Note that per-call implementation requires that SSL/TLS authentication is already in place at the channel level.


# Adding Extension Feature Support

You can add support for adapter extension features by providing an `ExtensionFeatureFactory` delegate in the proxy options. This delegate will be invoked for every extension feature that the remote proxy reports that it supports:

```csharp
var options = new GrpcAdapterProxyOptions() {
    AdapterId = "{SOME_ADAPTER_ID}",
    ExtensionFeatureFactory = (featureName, proxy) => {
        return GetFeatureImplementation(featureName, proxy);
    }
};
```

The `CreateClient<TClient>` method on the proxy can be used to create a gRPC client that uses the channel or HTTP client configured when the proxy was created.