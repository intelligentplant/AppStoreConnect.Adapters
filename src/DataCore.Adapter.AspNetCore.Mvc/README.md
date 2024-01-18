# DataCore.Adapter.AspNetCore.Mvc

This project contains API controllers for querying adapters in an ASP.NET Core application.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Mvc](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Mvc).


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


# Registering Adapter API Controllers

API controllers are registered with ASP.NET Core MVC in the `Startup.cs` file's `ConfigureServices` method:

```csharp
// Add the adapter API controllers to the MVC registration.
builder.Services.AddMvc()
    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
    .AddDataCoreAdapterMvc();
```


# Registering Endpoints

Once the adapter controllers have been added to the MVC registration, endpoints will added when the standard `MapControllers` endpoint routing extension method is called in the `Startup.cs` file's `Configure` method:

```csharp
app.UseRouting();

app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
});
```