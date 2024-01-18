# DataCore.Adapter.AspNetCore.MinimalApi

This project contains Minimal API routes for querying adapters in an ASP.NET Core application.

The Minimal API routes are a drop-in replacement for the existing [MVC controllers](../DataCore.Adapter.AspNetCore.Mvc); attempting to use both in the same application will cause routing conflicts!


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.MinimalApi](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.MinimalApi).


# Usage

> See the [Minimal API example](../../examples/MinimalApiExample) for a working example of how to use the library.

When building your application, register the required supporting services:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Other code removed for brevity

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddDataCoreAdapterApiServices();
```

Once the application has been built, register the API routes:

```csharp
var app = builder.Build();

// Other code removed for brevity

app.MapDataCoreAdapterApiRoutes();
```


# Customising Endpoint Configuration

To customise the API route registrations such as applying an authorization policy, use the `IEndpointConventionBuilder` returned by the `MapDataCoreAdapterApiRoutes()` extension method. For example, to require an authenticated user to be able to call any adapter endpoint:

```csharp
app.MapDataCoreAdapterApiRoutes()
    .RequireAuthorization();
```


# Migrating from MVC Controllers

To migrate from hosting the HTTP API via [MVC controllers](../DataCore.Adapter.AspNetCore.Mvc) to Minimal API routes, remove the adapter MVC controllers from the MVC registration:

```csharp
// Before:
services.AddMvc()
    .AddDataCoreAdapterMvc();

// After:
services.AddMvc();
```

> If you do not require MVC elsewhere in your application, you can remove all MVC-related services and routes.

Once you have removed the MVC registration, follow the [instructions above](#usage) to register the Minimal API services and routes.
