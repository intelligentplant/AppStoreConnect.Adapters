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


# Using MessagePack with SignalR instead of JSON

See [here](https://docs.microsoft.com/en-us/aspnet/core/signalr/messagepackhubprotocol#configure-messagepack-on-the-client) for instructions for configuring the SignalR client to use MessagePack encoding instead of JSON.
