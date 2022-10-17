# DataCore.Adapter.AspNetCore.SignalR.Client

[Client](./AdapterSignalRClient.cs) for querying remote adapters via ASP.NET Core SignalR.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Client](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Client).


# Configuring a Connection

Configure the connection as follows:

```csharp
var builder = new HubConnectionBuilder()
    .WithDataCoreAdapterConnection("https://some-server" + AdapterSignalRClient.DefaultHubRoute);
```


# Configuring the HTTP Connection Settings

The SignalR HTTP connection settings can be configured by passing a delegate to the `WithDataCoreAdapterConnection` extension method. The delegate has the same signature as the built-in `WithUrl` extension method:

```csharp
var builder = new HubConnectionBuilder()
    .WithDataCoreAdapterConnection("https://some-server" + AdapterSignalRClient.DefaultHubRoute, options => {
        options.Transports = Http.Connections.HttpTransportType.WebSockets;
    });
```


# Adding a Retry Policy

SignalR supports the use of retry policies to determine if and when a client should attempt to reconnect to a server if it becomes disconnected. The [RepeatingRetryPolicy](./RepeatingRetryPolicy.cs) class allows you to define a set of intervals to wait before attempting reconnection, and repeats the last delay until reconnection is established. This can be useful when recovering from longer-term connection outages. You can add the retry policy to your `HubConnectionBuilder`:

```csharp
var builder = new HubConnectionBuilder()
    .WithDataCoreAdapterConnection("https://some-server" + AdapterSignalRClient.DefaultHubRoute);
    // Attempt reconnection after 200ms, 500ms, 1s, 5s, and 30s delays. The final 30s delay will 
    // be repeated until reconnection occurs.
    .WithAutomaticReconnect(new RepeatingRetryPolicy(200, 500, 1000, 5000, 30000)); 
```
