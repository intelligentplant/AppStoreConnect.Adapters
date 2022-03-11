# Tutorial - Creating an Adapter

_This is part 1 of a tutorial series about creating an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Getting Started

_The full code for this chapter can be found [here](/examples/tutorials/creating-an-adapter/chapter-01)._

In this tutorial, we will create a bare-bones adapter, using the [AdapterBase](/src/DataCore.Adapter/AdapterBase.cs) base class. In later chapters, we will extend our adapter to implement different features, and explore other base classes available to us.

To get started, create a new console app project called `MyAdapter` using Visual Studio or `dotnet new`:

```
mkdir MyAdapter
cd MyAdapter
dotnet new console
```

The project can target .NET Framework 4.8, .NET Core 3.1, or .NET 5.0 or later.

Next, we will add a package references to the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) and [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting/) NuGet packages.

We will use the [.NET Generic Host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host) to run the example console applications in this tutorial. After you have added the above package references, replace the code in your `Program.cs` file with the following:

```csharp
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyAdapter {
    class Program {

        public static async Task Main(params string[] args) {
            await CreateHostBuilder(args).RunConsoleAsync().ConfigureAwait(false);
        }


        private static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args).ConfigureLogging(options => {
                options.SetMinimumLevel(LogLevel.Warning);
            }).ConfigureServices(services => {
                services.AddHostedService<Runner>();
            });
        }

    }

}
```

> Note: programs that use the .NET Generic Host will keep running until they are cancelled e.g. by pressing `CTRL+C`.

Next, add a new file to the project called `Runner.cs` and replace the code with the following:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    internal class Runner : BackgroundService {

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Task.CompletedTask;
        }

    }
}
```

The `Runner` class is a background service run by the .NET Generic Host that we will use to run our application logic.


### About Adapter Features

Adapters can define a set of features that expose different kinds of information and behaviours, depending on the capabilities of the system that you are connecting to. For example, you can add a feature that allows a caller to browse the available measurements on an adapter, or a feature that allows a caller to request the current value of a set of measurements.

Each feature is identifier via a URI. For example, the feature to request current measurement values has a URI of `asc:features/real-time-data/values/read/snapshot/`. Each feature is defined in C# using an interface; the interface definitions can be found [here](/src/DataCore.Adapter.Abstractions).

To view a summary of each standard adapter feature, update the `Runner.cs` file in your project as follows:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    internal class Runner : BackgroundService {

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            PrintFeatureDescriptions();
            return Task.CompletedTask;
        }


        private static void PrintFeatureDescriptions() {
            foreach (var type in TypeExtensions.GetStandardAdapterFeatureTypes().OrderBy(x => x.FullName)) {
                var descriptor = type.CreateFeatureDescriptor();
                Console.WriteLine($"[{descriptor.DisplayName}]");
                Console.WriteLine($"  Type: {type.FullName}");
                Console.WriteLine($"  URI: {descriptor.Uri}");
                Console.WriteLine($"  Description: {descriptor.Description}");
                Console.WriteLine();
            }
        }

    }
}
```

If you compile and run the program, you will see output similar to the following describing the standard features:

```
[Asset Model Browsing]
  Type: DataCore.Adapter.AssetModel.IAssetModelBrowse
  URI: asc:features/asset-model/browse/
  Description: Allows browsing of an adapter's asset model hierarchy.

[Asset Model Search]
  Type: DataCore.Adapter.AssetModel.IAssetModelSearch
  URI: asc:features/asset-model/search/
  Description: Allows an adapter's asset model hierarchy to be searched.

[Health Check]
  Type: DataCore.Adapter.Diagnostics.IHealthCheck
  URI: asc:features/diagnostics/health-check/
  Description: Allows an adapter's health status to be polled or subscribed to.

[Event Message Push]
  Type: DataCore.Adapter.Events.IEventMessagePush
  URI: asc:features/events/push/
  Description: Allows subscribers to receive event messages from an adapter in real-time.

[Topic-Based Event Message Push]
  Type: DataCore.Adapter.Events.IEventMessagePushWithTopics
  URI: asc:features/events/push/topics/
  Description: Allows subscribers to receive event messages from an adapter in real-time for specific topics.

[Time-Based Event Message History]
  Type: DataCore.Adapter.Events.IReadEventMessagesForTimeRange
  URI: asc:features/events/read/time/
  Description: Allows the event message history on an adapter to be polled using a time range.

[Cursor-Based Event Message History]
  Type: DataCore.Adapter.Events.IReadEventMessagesUsingCursor
  URI: asc:features/events/read/cursor/
  Description: Allows the event message history on an adapter to be polled using a cursor to define the starting point.

[Write Event Messages]
  Type: DataCore.Adapter.Events.IWriteEventMessages
  URI: asc:features/events/write/
  Description: Allows an adapter to be used as an event sink for messages generated by external sources.

[Read Plot Tag Values]
  Type: DataCore.Adapter.RealTimeData.IReadPlotTagValues
  URI: asc:features/real-time-data/values/read/plot/
  Description: Allows polling of historical tag values using a visualisation-friendly aggregation.

[Read Processed Tag Values]
  Type: DataCore.Adapter.RealTimeData.IReadProcessedTagValues
  URI: asc:features/real-time-data/values/read/processed/
  Description: Allows polling of historical tag values using a supported aggregate function to compute the results.

[Read Raw Tag Values]
  Type: DataCore.Adapter.RealTimeData.IReadRawTagValues
  URI: asc:features/real-time-data/values/read/raw/
  Description: Allows polling of raw, unprocessed historical tag values.

[Read Snapshot Tag Values]
  Type: DataCore.Adapter.RealTimeData.IReadSnapshotTagValues
  URI: asc:features/real-time-data/values/read/snapshot/
  Description: Allows polling of snapshot (instantaneous) tag values.

[Read Annotations]
  Type: DataCore.Adapter.RealTimeData.IReadTagValueAnnotations
  URI: asc:features/real-time-data/annotations/read/
  Description: Allows polling of annotations on historical tag values.

[Read Tag Values At Times]
  Type: DataCore.Adapter.RealTimeData.IReadTagValuesAtTimes
  URI: asc:features/real-time-data/values/read/at-times/
  Description: Allows polling of tag values at specific timestamps in history.

[Snapshot Tag Value Push]
  Type: DataCore.Adapter.RealTimeData.ISnapshotTagValuePush
  URI: asc:features/real-time-data/values/push/
  Description: Allows subscribers to receive snapshot tag value updates from an adapter in real-time.

[Write Historical Tag Values]
  Type: DataCore.Adapter.RealTimeData.IWriteHistoricalTagValues
  URI: asc:features/real-time-data/values/write/history/
  Description: Allows tag values from an external source to be written directly into an adapter's history archive.

[Write Snapshot Tag Values]
  Type: DataCore.Adapter.RealTimeData.IWriteSnapshotTagValues
  URI: asc:features/real-time-data/values/write/snapshot/
  Description: Allows the snapshot value of an adapter's tags to be updated from an external source.

[Write Annotations]
  Type: DataCore.Adapter.RealTimeData.IWriteTagValueAnnotations
  URI: asc:features/real-time-data/annotations/write/
  Description: Allows tag value annotations on an adapter to be created, updated, and deleted.

[Tag Information]
  Type: DataCore.Adapter.RealTimeData.ITagInfo
  URI: asc:features/tags/info/
  Description: Allows retrieval of an adapter's tag definitions using the tag's ID or name.

[Tag Search]
  Type: DataCore.Adapter.RealTimeData.ITagSearch
  URI: asc:features/tags/search/
  Description: Allows an adapter's tag definitions to be searched.
```

> Note: adapters are not required to implement every feature! You can pick and choose which of the features are appropriate for your adapter.


### Creating an Adapter

Next, create a new class called `Adapter`, and extend `AdapterBase`:

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase {

    }
}
```

The first thing we have to do is write a constructor that will call the protected constructor in `AdapterBase`:

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

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
            IBackgroundTaskService backgroundTaskService = null,
            // Logging
            ILogger<Adapter> logger = null
        ) : base(id, name, description, backgroundTaskService, logger) { }

    }
}
```

The `IBackgroundTaskService` type is defined in the [IntelligentPlant.BackgroundTasks](https://www.nuget.org/packages/IntelligentPlant.BackgroundTasks/) package, which is transitively referenced by the [IntelligentPlant.AppStoreConnect.Adapter](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter/) package.

Next, we must provide implementations for the abstract `StartAsync` and `StopAsync` methods, that are called when the adapter is started and stopped respectively:

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

At this stage, we have a fully-working adapter implementation, albeit one that we have not yet added any features to. However, `AdapterBase` does provide an implementation for one adapter feature automatically: [IHealthCheck](/src/DataCore.Adapter.Abstractions/Diagnostics/IHealthCheck.cs). The `IHealthCheck` feature is used to report on the current health of an adapter. This can be used to verify the health of e.g. connections to remote systems that the adapter speaks to.

We can customise the health checks that our adapter performs by overriding the `CheckHealthAsync` method:

```csharp
protected override Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(
    IAdapterCallContext context, 
    CancellationToken cancellationToken
) {
    return Task.FromResult<IEnumerable<HealthCheckResult>>(new[] {
        HealthCheckResult.Healthy("Example", "All systems normal!")
    });
}
```

Note that the `CheckHealthAsync` method accepts an [IAdapterCallContext](/src/DataCore.Adapter.Abstractions/IAdapterCallContext.cs) object as one of its parameters. This object is used by adapter features to provide information about the calling user, so that the adapter can decide whether or not to authorize the call. When hosting adapters in ASP.NET Core using the [IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Common](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.AspNetCore.Common) package, an `IAdapterCallContext` that is constructed from the `HttpContext` for the caller is passed to the adapter.


## Testing

To test our adapter, replace the contents of the `Runner.cs` file with the following:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    internal class Runner : BackgroundService {

        private const string AdapterId = "example";

        private const string AdapterDisplayName = "Example Adapter";

        private const string AdapterDescription = "Example adapter, built using the tutorial on GitHub";


        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Run(new DefaultAdapterCallContext(), stoppingToken);
        }


        private static void PrintFeatureDescriptions() {
            foreach (var type in TypeExtensions.GetStandardAdapterFeatureTypes().OrderBy(x => x.FullName)) {
                var descriptor = type.CreateFeatureDescriptor();
                Console.WriteLine($"[{descriptor.DisplayName}]");
                Console.WriteLine($"  Type: {type.FullName}");
                Console.WriteLine($"  URI: {descriptor.Uri}");
                Console.WriteLine($"  Description: {descriptor.Description}");
                Console.WriteLine();
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
                    Console.WriteLine($"    - {feature}");
                }

                var healthFeature = adapter.GetFeature<IHealthCheck>();
                var health = await healthFeature.CheckHealthAsync(context, cancellationToken);
                Console.WriteLine("  Health:");
                Console.WriteLine($"    - <{health.Status}> {health.DisplayName}: {health.Description ?? "no description provided"}");
                foreach (var item in health.InnerResults) {
                    Console.WriteLine($"      - <{item.Status}> {item.DisplayName}: {item.Description ?? "no description provided"}");
                }
                Console.WriteLine();

            }
        }

    }
}
```

The `Run` method will display information about the adapter, including the implemented features. It will then call the `IHealthCheck.CheckHealthAsync` method and display the health check results.

When you run the program, you will see output like the following:

```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-09-18T09:58:24Z
  Features:
    - asc:features/diagnostics/health-check/
  Health:
    - <Healthy> Adapter Health: The adapter is running with healthy status.
      - <Healthy> Example: All systems normal!
```

Note that the URI for the health check feature (`asc:features/diagnostics/health-check/`) is listed in the available adapter features.


## Next Steps

In the [next chapter](02-Reading_Current_Values.md), we will start adding more standard feature implementations to our new adapter.
