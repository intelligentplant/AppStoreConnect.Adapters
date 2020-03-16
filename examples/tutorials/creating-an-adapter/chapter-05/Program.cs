using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;

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

                var tagSearchFeature = adapter.GetFeature<ITagSearch>();
                var readRawFeature = adapter.GetFeature<IReadRawTagValues>();
                var readProcessedFeature = adapter.GetFeature<IReadProcessedTagValues>();

                Console.WriteLine();
                Console.WriteLine("  Supported Aggregations:");
                var funcs = new List<DataFunctionDescriptor>();
                await foreach (var func in readProcessedFeature.GetSupportedDataFunctions(context, cancellationToken).ReadAllAsync()) {
                    funcs.Add(func);
                    Console.WriteLine($"    - {func.Id}");
                    Console.WriteLine($"      - Name: {func.Name}");
                    Console.WriteLine($"      - Description: {func.Description}");
                    Console.WriteLine("      - Properties:");
                    foreach (var prop in func.Properties) {
                        Console.WriteLine($"        - {prop.Name} = {prop.Value}");
                    }
                }

                var tags = tagSearchFeature.FindTags(
                    context,
                    new FindTagsRequest() {
                        Name = "Sin*",
                        PageSize = 1
                    },
                    cancellationToken
                );

                await tags.WaitToReadAsync(cancellationToken);
                tags.TryRead(out var tag);

                Console.WriteLine();
                Console.WriteLine("[Tag Details]");
                Console.WriteLine($"  Name: {tag.Name}");
                Console.WriteLine($"  ID: {tag.Id}");
                Console.WriteLine($"  Description: {tag.Description}");
                Console.WriteLine("  Properties:");
                foreach (var prop in tag.Properties) {
                    Console.WriteLine($"    - {prop.Name} = {prop.Value}");
                }

                var now = DateTime.UtcNow;
                var start = now.AddSeconds(-15);
                var end = now;
                var sampleInterval = TimeSpan.FromSeconds(5);

                Console.WriteLine();
                Console.WriteLine($"  Raw Values ({start:HH:mm:ss.fff} - {end:HH:mm:ss.fff} UTC):");
                var rawValues = readRawFeature.ReadRawTagValues(
                    context,
                    new ReadRawTagValuesRequest() {
                        Tags = new[] { tag.Id },
                        UtcStartTime = start,
                        UtcEndTime = end,
                        BoundaryType = RawDataBoundaryType.Outside
                    },
                    cancellationToken
                );
                await foreach (var value in rawValues.ReadAllAsync(cancellationToken)) {
                    Console.WriteLine($"    - {value.Value}");
                }

                foreach (var func in funcs) {
                    Console.WriteLine();
                    Console.WriteLine($"  {func.Name} Values ({sampleInterval} sample interval):");

                    var processedValues = readProcessedFeature.ReadProcessedTagValues(
                        context,
                        new ReadProcessedTagValuesRequest() {
                            Tags = new[] { tag.Id },
                            DataFunctions = new[] { func.Id },
                            UtcStartTime = start,
                            UtcEndTime = end,
                            SampleInterval = sampleInterval
                        },
                        cancellationToken
                    );

                    await foreach (var value in processedValues.ReadAllAsync(cancellationToken)) {
                        Console.WriteLine($"    - {value.Value}");
                    }
                }
            }
        }

    }
}
