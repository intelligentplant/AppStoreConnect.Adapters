# DataCore.Adapter.Grpc.Proxy

Proxy adapter that connects to a remote adapter via gRPC.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Grpc.Proxy](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Grpc.Proxy).


# Creating a Proxy Instance

```csharp
var options = new GrpcAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    RemoteId = "{SOME_ADAPTER_ID}"
};

var channel = Grpc.Net.Client.GrpcChannel.ForAddress("https://localhost:5001");
var proxy = new GrpcAdapterProxy(channel, Options.Create(options), NullLoggerFactory.Instance);
await proxy.StartAsync(cancellationToken);
```

When running the proxy on .NET Framework, you must also set the HTTP handler for the channel, as per the instructions [here](https://learn.microsoft.com/en-us/aspnet/core/grpc/netstandard#net-framework):

```csharp
var channel = Grpc.Net.Client.GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions {
    HttpHandler = new WinHttpHandler()
});
```

> Grpc.Net.Client on .NET Framework has limited suuport for bidirectional and client streaming calls on some versions of Windows. Adapter operations that normally use bidirectional streaming such as tag value subscriptions and writes are translated into unary or server streaming invocations by the proxy if the underlying version of Windows does not provide full gRPC client support.


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

gRPC supports both per-channel and per-call authentication. If the remote host supports (or requires) per-call authentication, you can configure this by setting the `GetCallCredentials` property in the `GrpcAdapterProxyOptions` object you pass to the proxy constructor. The property is a delegate that takes an `IAdapterCallContext` representing the calling user, and returns a collection of [IClientCallCredentials](../DataCore.Adapter.Grpc.Client/Authentication/IClientCallCredentials.cs) objects that will be added to the headers of the outgoing gRPC request:

```csharp
var options = new GrpcAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    RemoteId = "{SOME_ADAPTER_ID}",
    GetCallCredentials = async (IAdapterCallContext context) => {
        var accessToken = await GetAccessToken(context);
        return new IClientCallCredentials[] {
            new BearerTokenCallCredentials(accessToken)
        };
    }
};
```

Note that per-call authentication requires that SSL/TLS authentication is already in place at the channel level.
