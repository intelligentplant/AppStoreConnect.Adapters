# DataCore.Adapter.Grpc.Client

Client for querying remote adapters via [gRPC](https://grpc.io). Client and request objects are automatically generated from the adapter [service definition proto files](../Protos) using the [Grpc.Tools](https://www.nuget.org/packages/Grpc.Tools) NuGet package.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.Grpc.Client](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Grpc.Client).


# Creating Client Objects

The gRPC services are all defined in the `DataCore.Adapter.Grpc` namespace. For example, to request information about the adapter host:

```csharp
var channel = Grpc.Net.Client.GrpcChannel.ForAddress("https://localhost:58189");
var client = new DataCore.Adapter.Grpc.HostInfoService.HostInfoServiceClient(channel);

var hostInfo = await (client.GetHostInfoAsync(new GetHostInfoRequest())).HostInfo.ToAdapterHostInfo();
```


# Implementing Per-Call Authentication

gRPC supports both per-channel and per-call authentication. If the remote host supports (or requires) per-call authentication, you can easily construct and add the required metadata entries to the client call, using instances of the [IClientCallCredentials](./Authentication/IClientCallCredentials.cs) interface:

```csharp
public async Task<GetHostInfoResponse> CallWithCredentials(HostInfoService.HostInfoServiceClient client, CancellationToken cancellationToken) {
    var credentials = Grpc.Core.CallCredentials.FromInterceptor(new Grpc.Core.AsyncAuthInterceptor(async (authContext, metadata) => {
        ClientCertificateCallCredentials cert = await GetClientCertificateCredentials();
        cert.AddMetadataEntry(metadata);
    });

    var callOptions = Grpc.Core.CallOptions(cancellationToken: cancellationToken, credentials: credentials)

    return await client.GetHostInfoAsync(
        new GetHostInfoRequest(),
        callOptions
    );
}
```

Note that per-call implementation requires that SSL/TLS authentication is already in place at the channel level.