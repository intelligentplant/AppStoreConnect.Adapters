using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    internal class Runner : BackgroundService {

        private const string AdapterId = "example";

        private const string AdapterDisplayName = "Example Adapter";

        private const string AdapterDescription = "Example adapter, built using the tutorial on GitHub";


        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Run(new DefaultAdapterCallContext(), stoppingToken);
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

                var tagSearchFeature = adapter.GetFeature<ITagSearch>();
                var readSnapshotFeature = adapter.GetFeature<IReadSnapshotTagValues>();

                var tags = await tagSearchFeature.FindTags(
                    context,
                    new FindTagsRequest() {
                        Name = "*"
                    },
                    cancellationToken
                );

                await foreach (var tag in tags.ReadAllAsync(cancellationToken)) {
                    Console.WriteLine();
                    Console.WriteLine("[Tag Details]");
                    Console.WriteLine($"  Name: {tag.Name}");
                    Console.WriteLine($"  ID: {tag.Id}");
                    Console.WriteLine($"  Description: {tag.Description}");
                    Console.WriteLine("  Properties:");
                    foreach (var prop in tag.Properties) {
                        Console.WriteLine($"    - {prop.Name} = {prop.Value}");
                    }

                    var snapshotValues = await readSnapshotFeature.ReadSnapshotTagValues(
                        context,
                        new ReadSnapshotTagValuesRequest() {
                            Tags = new[] { tag.Id }
                        },
                        cancellationToken
                    );

                    Console.WriteLine("  Snapshot Value:");
                    await foreach (var value in snapshotValues.ReadAllAsync(cancellationToken)) {
                        Console.WriteLine($"    - {value.Value}");
                    }
                }
            }
        }

    }
}
