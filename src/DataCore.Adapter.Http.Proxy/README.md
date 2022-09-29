# DataCore.Adapter.AspNetCoreHttp.Proxy

Proxy adapter that connects to a remote adapter via HTTP.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Http.Proxy](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Http.Proxy).


# Creating a Proxy Instance

```csharp
var options = new HttpAdapterProxyOptions() {
    Id = "some-id",
    Name = "some-name",
    RemoteId = "{SOME_ADAPTER_ID}"
};

var httpClient = new HttpClient() {
    BaseAddress = "https://my-site.com/"
};
var adapterHttpClient = new AdapterHttpClient(httpClient);

var proxy = new HttpAdapterProxy(adapterHttpClient, Options.Create(options), NullLoggerFactory.Instance);
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

Per-call authentication can be applied by configuring the proxy's underlying `HttpClient` to use a `DelegatingHandler` that will set appropriate headers on outgoing HTTP requests. The static `CreateRequestTransformHandler` method on the [AdapterHttpClient](/src/DataCore.Adapter.Http.Client/AdapterHttpClient.cs) class can be used to create an appropriate handler:

```csharp
public void ConfigureServices(IServiceCollection services) {

    services.AddHttpClient("AdapterProxy", options => {
        options.BaseAddress = new Uri("https://my-site.com/");
    })
    .AddHttpMessageHandler(AdapterHttpClient.CreateRequestTransformHandler(async (request, principal, cancellationToken) => {
        if (principal == null) {
            return;
        }

        string accessToken;

        // Add your logic to get the access token for the principal...

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }));

}
```

You can then use this client when creating the `AdapterHttpClient` that is passed to the proxy:

```csharp
async Task<IAdapter> CreateProxy(IHttpClientFactory factory) {
    var options = new HttpAdapterProxyOptions() {
        Id = "some-id",
        Name = "some-name",
        RemoteId = "{SOME_ADAPTER_ID}"
    };

    var httpClient = factory.CreateClient("AdapterProxy");
    var adapterHttpClient = new AdapterHttpClient(httpClient);

    var proxy = new HttpAdapterProxy(httpClient, Options.Create(options), NullLoggerFactory.Instance);
    await proxy.StartAsync(cancellationToken);

    return proxy;
}
```

The `ClaimsPrincipal` that is passed to the callback delegate is passed through from the `IAdapterCallContext` that is specified when an adapter feature method is called.

