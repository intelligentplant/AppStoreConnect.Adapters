# DataCore.Adapter.Http.Client

[Client](./AdapterHttpClient.cs) for querying remote adapters via the HTTP REST API.

This client supports polling requests only; as such, it cannot be used to subscribe to receive snapshot tag value changes, or event messages published by remote adapters. For push-related functionality, use the gRPC or SignalR client.


# Notes

All API routes are relative to the base URL for the HTTP client supplied to the `AdapterHttpClient`. Therefore, you _must_ set a `BaseAddress` property on the `HttpClient` instance i.e.

```csharp
var httpClient = new HttpClient() {
    BaseAddress = "https://my-site.com/"
};
```

All query methods optionally accept a `ClaimsPrincipal` as a parameter, allowing you to associate a principal with an outgoing request. This is useful if e.g. you are using a back-channel HTTP connection to query remote adapters on behalf of one of your app's users, and you need to add an `Authorize` header to an outgoing request to represent the calling user.

You can create an `HttpMessageHandler` capable of receiving the `ClaimsPrincipal` and modifying the associated HTTP request by calling the `AdapterHttpClient.CreateRequestTransformHandler` method. This handler can be added to the request pipeline for the `HttpClient` passed into the `AdapterHttpClient` constructor. If you are using ASP.NET Core, you can configure the `AdapterHttpClient` by registering it as a service with the ASP.NET Core dependency injection system:

```csharp
public void ConfigureServices(IServiceCollection services) {

    services.AddHttpClient<AdapterHttpClient>(options => {
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

You can then inject the `AdapterHttpClient` directly into e.g. API controllers:

```csharp
[ApiController]
public class MyController: ControllerBase {

    private readonly AdapterHttpClient _client;

    public MyController(AdapterHttpClient client) {
        _client = client;
    }

    public async Task<HostInfo> GetAdapterHostInfo(CancellationToken cancellationToken) {
        return await _client.HostInfo.GetHostInfoAsync(User, cancellationToken);
    }

}
```