﻿using System;
using System.Collections.Generic;
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


        private static System.Text.Json.JsonSerializerOptions GetJsonOptions() {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions() {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };
            jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            return jsonOptions;
        }


        private static async Task Run(IAdapterCallContext context, CancellationToken cancellationToken) {
            using (IAdapter adapter = new Adapter(AdapterId, AdapterDisplayName, AdapterDescription)) {
                await adapter.StartAsync(cancellationToken);

                var adapterInfo = await AdapterInfo.Create(context, adapter, cancellationToken);
                Console.WriteLine("Adapter Summary:");
                Console.WriteLine();
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(adapterInfo, GetJsonOptions()));

                var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
                var correlationId = Guid.NewGuid().ToString();
                var now = DateTime.UtcNow;
                var pingMessage = new PingMessage() { CorrelationId = correlationId, UtcTime = now };
                var pingMessageStream = Channel.CreateUnbounded<PingMessage>();

                Console.WriteLine();

                _ = Task.Run(async () => { 
                    try {
                        var rnd = new Random();
                        while (!cancellationToken.IsCancellationRequested) {
                            // Delay for up to 2 seconds.
                            var delay = TimeSpan.FromMilliseconds(2000 * rnd.NextDouble());
                            if (delay > TimeSpan.Zero) {
                                await Task.Delay(delay, cancellationToken);
                            }
                            var pingMessage = new PingMessage() { CorrelationId = Guid.NewGuid().ToString() };

                            Console.WriteLine($"[DUPLEX STREAM] Ping: {pingMessage.CorrelationId} @ {pingMessage.UtcTime:HH:mm:ss} UTC");
                            await pingMessageStream.Writer.WriteAsync(pingMessage, cancellationToken);
                        }
                    }
                    catch { }
                    finally {
                        pingMessageStream.Writer.TryComplete();
                    }
                }, cancellationToken);

                await foreach (var pongMessage in extensionFeature.DuplexStream<PingMessage, PongMessage>(
                    context,
                    new Uri("asc:extensions/tutorial/ping-pong/duplexstream/Ping/"),
                    pingMessageStream.Reader.ReadAllAsync(cancellationToken),
                    null,
                    cancellationToken
                )) {
                    Console.WriteLine($"[DUPLEX STREAM] Pong: {pongMessage.CorrelationId} @ {pongMessage.UtcTime:HH:mm:ss} UTC");
                }
            }
        }

    }

    public class AdapterInfo {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public IEnumerable<string> Features { get; set; }
        public IDictionary<string, ExtensionFeatureInfo> Extensions { get; set; }

        public static async Task<AdapterInfo> Create(IAdapterCallContext context, IAdapter adapter, CancellationToken cancellationToken) {
            var adapterDescriptor = adapter.CreateExtendedAdapterDescriptor();

            var result = new AdapterInfo() {
                Id = adapterDescriptor.Id,
                Name = adapterDescriptor.Name,
                Description = adapterDescriptor.Description,
                Properties = adapterDescriptor.Properties.ToDictionary(x => x.Name, x => x.Value.ToString()),
                Features = adapterDescriptor.Features.ToArray(),
                Extensions = new Dictionary<string, ExtensionFeatureInfo>()
            };

            foreach (var feature in adapterDescriptor.Extensions) {
                var extension = adapter.GetFeature<IAdapterExtensionFeature>(feature);
                var extensionDescriptor = await extension.GetDescriptor(context, feature, cancellationToken);
                var extensionOps = await extension.GetOperations(context, feature, cancellationToken);

                var extInfo = new ExtensionFeatureInfo() {
                    Name = extensionDescriptor.DisplayName,
                    Description = extensionDescriptor.Description,
                    Operations = extensionOps.ToDictionary(x => x.OperationId.ToString(), x => new ExtensionOperationInfo() {
                        OperationType = x.OperationType,
                        Name = x.Name,
                        Description = x.Description,
                        RequestSchema = x.RequestSchema,
                        ResponseSchema = x.ResponseSchema
                    })
                };

                result.Extensions[extensionDescriptor.Uri.ToString()] = extInfo;
            }

            return result;
        }
    }

    public class ExtensionFeatureInfo {
        public string Name { get; set; }
        public string Description { get; set; }
        public IDictionary<string, ExtensionOperationInfo> Operations { get; set; }
    }

    public class ExtensionOperationInfo {
        public ExtensionFeatureOperationType OperationType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public System.Text.Json.JsonElement? RequestSchema { get; set; }
        public System.Text.Json.JsonElement? ResponseSchema { get; set; }
    }

}
