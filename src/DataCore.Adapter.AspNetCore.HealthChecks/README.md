# DataCore.Adapter.AspNetCore.HealthChecks

ASP.NET Core [health checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) for hosted App Store Connect adapters.


# Installation

Add a NuGet package reference to [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.HealthChecks](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.HealthChecks).


# Registering Adapter Health Checks

To register the adapter health checks, use the `AddAdapterHeathChecks` extension when adding health checks to your registered services at startup:

```csharp
services.AddHealthChecks().AddAdapterHeathChecks();
```


# Details

When the registered health check is invoked, it will provide an aggregate health status for registered adapters, accessed via the registered [IAdapterAccessor](../DataCore.Adapter.Abstractions/IAdapterAccessor.cs) ([AspNetCoreAdapterAccessor](../DataCore.Adapter.AspNetCore.Common/AspNetCoreAdapterAccessor.cs) by default for ASP.NET Core applications).

The health status for an individual adapter is calculated as follows:

- If the adapter implements the [IHealthCheck](../DataCore.Adapter.Abstractions/Diagnostics/IHealthCheck.cs) feature, the feature is invoked to retrieve the health status for the adapter.
- If the adapter does not implement the `IHealthCheck` feature, a healthy or unhealthy status is returned depending on whether the adapter has been started or not.

Note that all adapters inheriting from [AdapterBase](../DataCore.Adapter/AdapterBase.cs) have built-in support for the `IHealthCheck` feature.

Once the health status of all adapters has been calculated, an aggregate health status is computed, which is used to determine the ASP.NET Core health status. The result data on the ASP.NET Core health check result maps from adapter ID to the adapter health check result for that adapter.
