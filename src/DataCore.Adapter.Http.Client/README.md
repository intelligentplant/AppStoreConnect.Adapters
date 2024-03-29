﻿# DataCore.Adapter.Http.Client

[Client](./AdapterHttpClient.cs) for querying remote adapters via the HTTP REST API.

This client supports polling requests only; as such, it cannot be used to subscribe to receive snapshot tag value changes, or event messages published by remote adapters. For push-related functionality, use the gRPC or SignalR client.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Http.Client](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Http.Client).


# Notes

## Base Address

All API routes are relative to the base URL for the HTTP client supplied to the `AdapterHttpClient`. Therefore, you _must_ set a `BaseAddress` property on the `HttpClient` instance i.e.

```csharp
var httpClient = new HttpClient() {
    BaseAddress = "https://my-site.com/"
};
```

## Authentication and Request Metadata

All query methods optionally accept a `RequestMetadata` as a parameter, allowing you to associate additional metadata with an outgoing request. This is useful if e.g. you are using a back-channel HTTP connection to query remote adapters on behalf of one of your app's users, and you need to add an `Authorize` header to an outgoing request to represent the calling user.

You can create an `HttpMessageHandler` capable of receiving the `RequestMetadata` and modifying the associated HTTP request by calling the `AdapterHttpClient.CreateRequestTransformHandler` method. This handler can be added to the request pipeline for the `HttpClient` passed into the `AdapterHttpClient` constructor. If you are using ASP.NET Core, you can configure the `AdapterHttpClient` by registering it as a service with the ASP.NET Core dependency injection system:

```csharp
public void ConfigureServices(IServiceCollection services) {

    services.AddHttpClient<AdapterHttpClient>(options => {
        options.BaseAddress = new Uri("https://my-site.com/");
    })
    .AddHttpMessageHandler(AdapterHttpClient.CreateRequestTransformHandler(async (request, metadata, cancellationToken) => {
        if (metadata?.principal == null) {
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
public class MyController : ControllerBase {

    private readonly AdapterHttpClient _client;

    public MyController(AdapterHttpClient client) {
        _client = client;
    }

    public async Task<HostInfo> GetAdapterHostInfo(CancellationToken cancellationToken) {
        return await _client.HostInfo.GetHostInfoAsync(new RequestMetadata() { Principal = User }, cancellationToken);
    }

}
```

## Enabling HTTP/2 Support

Requests sent by an `HttpClient` default to using HTTP 1.1. To allow the use of HTTP/2 (or HTTP/3), you can add a `DelegatingHandler` to your request pipeline to set a higher default version:

```csharp
services
    .AddHttpClient<AdapterHttpClient>()
    .ConfigureHttpMessageHandlerBuilder(builder => {
        var httpVersionHandler = AdapterHttpClient.CreateHttpVersionHandler(new Version(2, 0));
        builder.AdditionalHandlers.Add(httpVersionHandler);
    };
```

Note that setting the default HTTP version on the `HttpClient` via `HttpClient.DefaultRequestVersion` will _not_ correctly set the HTTP version here, due to [this issue](https://github.com/dotnet/runtime/issues/31190) in the .NET runtime.  

The default behaviour of an HTTP request is to downgrade to a lower HTTP version if the requested version is not supported by the server, so specifying a higher version is safe by default.


### HTTP/2 Support on .NET Framework

HTTP/2 is supported on .NET Framework on Windows 11 and Windows Server 2022 or higher via the `System.Net.Http.WinHttpHandler` NuGet package. You must explicitly configure the primary HTTP handler for an HTTP client to be able to use it:

```csharp
services
    .AddHttpClient<AdapterHttpClient>()
    .ConfigureHttpMessageHandlerBuilder(builder => {
        builder.PrimaryHandler = new WinHttpHandler();

        builder.AdditionalHandlers.Add(AdapterHttpClient.CreateRequestTransformHandler((req, metadata, ct) => {
            req.Version = new Version(2, 0);
            return Task.CompletedTask;
        }));
    };
```
