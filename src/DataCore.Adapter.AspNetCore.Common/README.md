﻿# DataCore.Adapter.AspNetCore.Common

Common types and services used when hosting adapters using ASP.NET Core.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Common](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Common).


# Writing an Adapter Accessor

An [IAdapterAccessor](../DataCore.Adapter.Abstractions/IAdapterAccessor.cs) service is required so that your adapter(s) can be resolved at runtime. If the adapters that your application hosts are registered with the service collection at startup time, you can use the [AspNetCoreAdapterAccessor](./AspNetCoreAdapterAccessor.cs) class. This implementation is used by default if no custom adapter accessor is supplied.

You can supply your own implementation by inheriting from the [AdapterAccessor](../DataCore.Adapter/AdapterAccessor.cs) class. Inheriting from this class will ensure that an adapter is only visible to a calling user if they are authorized to access the adapter. See the [authorization](#writing-an-authorization-handler) section for information about authorizing access to adapters and adapter features.

To register your adapter accessor, call the `AddAdapterAccessor<TAdapterAccessor>()` extension method when [registering adapter services](#registering-adapter-services). Note that the adapter accessor is always registered as a *singleton* service.


# Writing an Authorization Handler

By default, all calls to the adapter API will be authorized, as long as they meet the authentication requirements of the hosting application. However, you may want to apply custom authorization policies to control access to individual adapters, or to features on an adapter (for example, you may want to prevent unauthorized callers from writing values to tags). 

Custom authorization is performed by the ASP.NET Core authorization model, by inheriting from the [FeatureAuthorizationHandler](./Authorization/FeatureAuthorizationHandler.cs) and implementing the `HandleRequirementAsync` method. In your implementation, you will be passed the adapter, and a [FeatureAuthorizationRequirement](./Authorization/FeatureAuthorizationRequirement.cs) that describes the feature that the caller is requesting access to. For example:

```csharp
public class MyFeatureAuthorizationHandler : FeatureAuthorizationHandler {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FeatureAuthorizationRequirement requirement, IAdapter resource) {
        var isAuthorized = true;

        if (requirement.Feature == null) {
            // Feature will be null if the call is to check if the adapter is visible to the 
            // calling user.
            isAuthorized = context.User.IsInRole("AdapterUsers");
        }
        else if (requirement.Feature == typeof(DataCore.Adapter.RealTimeData.Features.IReadTagValueAnnotations)) {
            isAuthorized = context.User.IsInRole("CanReadAnnotations");
        }

        if (isAuthorized) {
            context.Succeed(requirement);
        }
        else {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
```

To register your authorization handler, call `AddAdapterFeatureAuthorization<THandler>()` extension method when [registering adapter services](#registering-adapter-services). Note that the handler is always registered as a *singleton* service.


# Registering Adapter Services

Adapter services must be added to the application's service collection at startup. For example:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo("My Host", "A brief description of the hosting application")
    .AddAdapter<MyAdapter>()
    .AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();
```

If your adapter constructor accepts an `IOptions<T>` or `IOptionsMonitor<T>` parameter containing the adapter options, you can configure the adapter from the application's configuration settings (via your application's `appsettings.json` file for example):

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http1AndHttp2"
    }
  },
  "MyAdapter": {
    "adapter-01": {
      "Name": "Sample Data",
      "Description": "Provides a sample data set",
      "Enabled": true
    }
  }
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataCoreAdapterAspNetCoreServices()
    .AddHostInfo("My Host", "A brief description of the hosting application")
    .AddAdapterOptions<MyAdapterOptions>("adapter-01", options => options.Bind(builder.Configuration.GetSection("MyAdapter:adapter-01")))
    .AddAdapter<MyAdapter>("adapter-01")
    .AddAdapterFeatureAuthorization<MyAdapterFeatureAuthHandler>();
```

Note that, when using `IOptionsMonitor<T>` with an adapter derived from `AdapterBase<TOptions>`, the adapter will always try and retrieve named options that match the ID of the adapter. That is, if you register an adapter with an ID of `adapter-01`, you must also register named adapter options with the configuration system with a name of `adapter-01` as shown above.
