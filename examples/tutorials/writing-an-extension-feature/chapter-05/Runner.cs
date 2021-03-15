using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    internal class Runner : BackgroundService {

        private const string AdapterId = "example";

        private const string AdapterDisplayName = "Example Adapter";

        private const string AdapterDescription = "Example adapter with an extension feature, built using the tutorial on GitHub";


        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Run(new DefaultAdapterCallContext(), stoppingToken);
        }


        private static async Task Run(IAdapterCallContext context, CancellationToken cancellationToken) {
            using (IAdapter adapter = new Adapter(AdapterId, AdapterDisplayName, AdapterDescription)) {
                await adapter.StartAsync(cancellationToken);

                var adapterDescriptor = adapter.CreateExtendedAdapterDescriptor();

                Console.WriteLine();
                Console.WriteLine($"[{adapter.Descriptor.Id}]");
                Console.WriteLine($"  Name: {adapter.Descriptor.Name}");
                Console.WriteLine($"  Description: {adapter.Descriptor.Description}");
                Console.WriteLine("  Properties:");
                foreach (var prop in adapter.Properties) {
                    Console.WriteLine($"    - {prop.Name} = {prop.Value}");
                }
                Console.WriteLine("  Features:");
                foreach (var feature in adapterDescriptor.Features) {
                    Console.WriteLine($"    - {feature}");
                }
                Console.WriteLine("  Extensions:");
                foreach (var feature in adapterDescriptor.Extensions) {
                    var extension = adapter.GetFeature<IAdapterExtensionFeature>(feature);
                    var extensionDescriptor = await extension.GetDescriptor(context, feature, cancellationToken);
                    var extensionOps = await extension.GetOperations(context, feature, cancellationToken);
                    Console.WriteLine($"    - {feature}");
                    Console.WriteLine($"      - Name: {extensionDescriptor.DisplayName}");
                    Console.WriteLine($"      - Description: {extensionDescriptor.Description}");
                    Console.WriteLine($"      - Operations:");
                    foreach (var op in extensionOps) {
                        Console.WriteLine($"        - {op.Name} ({op.OperationId})");
                        Console.WriteLine($"          - Description: {op.Description}");
                    }
                }

                var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/temperature-converter/");
                Console.WriteLine();

                var degC = 40d;
                var degF = await extensionFeature.Invoke<double, double>(
                    context,
                    new Uri("asc:extensions/tutorial/temperature-converter/invoke/CtoF/"),
                    degC,
                    cancellationToken
                );

                Console.WriteLine($"{degC:0.#} Celsius is {degF:0.#} Fahrenheit");

                degF = 60d;
                degC = await extensionFeature.Invoke<double, double>(
                    context,
                    new Uri("asc:extensions/tutorial/temperature-converter/invoke/FtoC/"),
                    degF,
                    cancellationToken
                );

                Console.WriteLine($"{degF:0.#} Fahrenheit is {degC:0.#} Celsius");
            }
        }

    }
}
