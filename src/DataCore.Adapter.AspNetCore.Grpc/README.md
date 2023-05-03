# DataCore.Adapter.AspNetCore.Grpc

This project provides helper methods to add gRPC adapter services to an ASP.NET Core application.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Grpc](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Grpc).


# Registering Adapters and Adapter Services

Adapter services must be added to the application's service collection at startup. For example:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo("My Host", "A brief description of the hosting application")
    .AddAdapter<MyAdapter>()
    .AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();
```

In most cases, adapters can be registered using the `AddAdapter` extension method on the `IAdapterConfigurationBuilder` class, which registers them as singleton services. If your application can dynamically add or remove adapters at runtime, you must handle the adapter lifetimes yourself.


# Registering gRPC Services

```csharp
builder.Services.AddGrpc();
```


# Registering Endpoints

gRPC endpoints must be added to the application's endpoints in the `Startup.cs` file's `Configure` method:

```csharp
app.UseRouting();

app.UseEndpoints(endpoints => {
    endpoints.MapDataCoreGrpcServices();
});
```