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
