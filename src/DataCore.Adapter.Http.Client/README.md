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