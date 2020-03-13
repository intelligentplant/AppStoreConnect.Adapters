# Tutorial - Creating an Adapter

_This is part 1 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Getting Started

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-01)._

In this tutorial, we will create a bare-bones adapter, using the `AdapterBase` base class. In later chapters, we will extend our adapter to implement different features.

To get started, create a new .NET Core 3.1 console app project called `MyAdapter` using Visual Studio or `dotnet new`:

```
mkdir MyAdapter
cd MyAdapter
dotnet new console -f netcoreapp3.1
```

Next, we will add a package reference to the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) NuGet package.

Now, create a new class called `Adapter`, and extend `AdapterBase`:

```csharp
using DataCore.Adapter;

namespace MyAdapter {
    public class Adapter : AdapterBase {

    }
}
```

The first thing we have to do is write a constructor that will call the protected constructor in `AdapterBase`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase {

        public Adapter(
            // Unique identifier for the adapter instance
            string id, 
            // Adapter display name
            string name, 
            // Adapter description
            string description = null,
            // Used to allow the adapter to run background operations.
            IBackgroundTaskService scheduler = null,
            // Logging
            ILogger<Adapter> logger = null
        ) : base(id, name, description, scheduler, logger) { }

    }
}
```

The `IBackgroundTaskService` type is defined in the [IntelligentPlant.BackgroundTasks](https://www.nuget.org/packages/IntelligentPlant.BackgroundTasks/) package, which is transitively referenced by the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) package.

Next, we must provide implementations for the abstract `StartAsync` and `StopAsync` methods:

```csharp
protected override Task StartAsync(CancellationToken cancellationToken) {
    AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    return Task.CompletedTask;
}

protected override Task StopAsync(CancellationToken cancellationToken) {
    return Task.CompletedTask;
}
```

Note the following line in the `StartupAsync` method:

```csharp
AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
```

All adapters can define bespoke properties. When writing an adapter that connects to an external system, this can be used to provide contextual information such as the vendor, software version, user name, and so on.

At this stage, we have a fully-working adapter implementation, albeit one that we have not yet added any features to. However, `AdapterBase` does provide an implementation for one adapter feature automatically: `IHealthCheck`. The `IHealthCheck` feature is used to report on the current health of an adapter. This can be used to verify the health of e.g. connections to remote systems that the adapter speaks to.

We can customise the health checks that our adapter performs by overriding the `CheckHealthAsync` method:

```csharp
protected override Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(
    IAdapterCallContext context, 
    CancellationToken cancellationToken
) {
    return Task.FromResult<IEnumerable<HealthCheckResult>>(new[] {
        HealthCheckResult.Healthy("All systems normal!")
    });
}
```

Note that the `CheckHealthAsync` method accepts an `IAdapterCallContext` object as one of its parameters. This object is used by adapter features to provide information about the calling user, so that the adapter can decide whether or not to authorize the call. When hosting adapters in ASP.NET Core using the [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Common](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Common) package, an `IAdapterCallContext` that is constructed from the `HttpContext` for the caller is passed to the adapter.


## Testing

To test our adapter, add the following code to the `Program.cs` file that was generated when you created the console app project:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;

namespace MyAdapter {
    class Program {

        private const string AdapterId = "example";

        private const string AdapterDisplayName = "Example Adapter";

        private const string AdapterDescription = "Example adapter, built using the tutorial on GitHub";


        public static async Task Main(params string[] args) {
            using (var userCancelled = new CancellationTokenSource()) {
                Console.CancelKeyPress += (sender, e) => userCancelled.Cancel();
                try {
                    Console.WriteLine("Press CTRL+C to quit");
                    await Run(new DefaultAdapterCallContext(), userCancelled.Token);
                }
                catch (OperationCanceledException) { }
            }
        }


        private static async Task Run(IAdapterCallContext context, CancellationToken cancellationToken) {
            await using (IAdapter adapter = new Adapter(AdapterId, AdapterDisplayName, AdapterDescription)) {

                await adapter.StartAsync(cancellationToken);

                Console.WriteLine();
                Console.WriteLine($"[{adapter.Descriptor.Id}]");
                Console.WriteLine($"  Name: {adapter.Descriptor.Name}");
                Console.WriteLine($"  Description: {adapter.Descriptor.Description}");
                Console.WriteLine("  Properties:");
                foreach (var prop in adapter.Properties) {
                    Console.WriteLine($"    - {prop.Name} = {prop.Value}");
                }
                Console.WriteLine("  Features:");
                foreach (var feature in adapter.Features.Keys) {
                    Console.WriteLine($"    - {feature.Name}");
                }

                var healthFeature = adapter.GetFeature<IHealthCheck>();
                var health = await healthFeature.CheckHealthAsync(context, cancellationToken);
                Console.WriteLine("  Health:");
                Console.WriteLine($"    - <{health.Status.ToString()}> {health.Description}");
                foreach (var item in health.InnerResults) {
                    Console.WriteLine($"      - <{item.Status.ToString()}> {item.Description}");
                }
                Console.WriteLine();

            }
        }

    }
}
```

The `Run` method will display information about the adapter, including the implemented features. It will then call the `IHealthCheck.CheckHealthAsync` method and display the health check results. When you run the program, you will see output like the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-03-13T08:27:12Z
  Features:
    - IHealthCheck
  Health:
    - <Healthy> The adapter is running with healthy status.
      - <Healthy> All systems normal!
```


## Next Steps

In the [next chapter](02-Reading_Current_Values.md), we will start adding more standard feature implementations to our new adapter.
