# DataCore.Adapter.AspNetCore.SignalR.Client

[Client](./AdapterSignalRClient.cs) for querying remote adapters via ASP.NET Core SignalR.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Client](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Client).


# Registering System.Text.Json Converters

When using the protocol based on [System.Text.Json](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Protocols.Json), adapter-specific `JsonConverter<T>` instances must be registered with the with serializer settings:

```csharp
var builder = new HubConnectionBuilder()
    .WithUrl("https://some-server")
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.Converters.AddAdapterConverters();
    }); 
```


# Adding a Retry Policy

SignalR supports the use of retry policies to determine if and when a client should attempt to reconnect to a server if it becomes disconnected. The [RepeatingRetryPolicy](./RepeatingRetryPolicy.cs) class allows you to define a set of intervals to wait before attempting reconnection, and repeats the last delay until reconnection is established. This can be useful when recovering from longer-term connection outages. You can add the retry policy to your `HubConnectionBuilder`:

```csharp
var builder = new HubConnectionBuilder()
    .WithUrl("https://some-server")
    // Attempt reconnection after 200ms, 500ms, 1s, 5s, and 30s delays. The final 30s delay will 
    // be repeated until reconnection occurs.
    .WithAutomaticReconnect(new RepeatingRetryPolicy(200, 500, 1000, 5000, 30000))
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.Converters.AddAdapterConverters();
    }); 
```


# Using MessagePack with SignalR instead of JSON

See [here](https://docs.microsoft.com/en-us/aspnet/core/signalr/messagepackhubprotocol#configure-messagepack-on-the-client) for instructions for configuring the SignalR client to use MessagePack encoding instead of JSON.
