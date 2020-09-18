using System;
using System.Linq;
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
                Console.WriteLine($"    - <{health.Status.ToString()}> {health.Description}");
                foreach (var item in health.InnerResults) {
                    Console.WriteLine($"      - <{item.Status.ToString()}> {item.Description}");
                }
                Console.WriteLine();

            }
        }

    }
}
