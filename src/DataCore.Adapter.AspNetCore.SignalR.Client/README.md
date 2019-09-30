# DataCore.Adapter.AspNetCore.SignalR.Client

[Client](./AdapterSignalRClient.cs) for querying remote adapters via ASP.NET Core SignalR.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Client](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.SignalR.Client).


# Known Issues

The `System.Text.Json` implementation of the SignalR protocol (the default in ASP.NET Core 3.0) is not currently (as of version 3.0.0) compatible with the Adapter DTO classes, due to the lack of a parameterless constructor in some types. The [JSON.NET](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Protocols.NewtonsoftJson/) or [MessagePack](https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Protocols.MessagePack/) protocol implementations can be used without issue.

See [here](https://docs.microsoft.com/en-us/aspnet/core/signalr/messagepackhubprotocol#configure-messagepack-on-the-client) for instructions to configure one of the two protocol implementations referenced above.
