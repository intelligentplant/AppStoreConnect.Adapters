using System;
using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.Grpc;
using Grpc.Core;

namespace DataCore.Adapter.GrpcExampleClient {
    class Program {
        static async Task Main(string[] args) {
            // Include port of the gRPC server as an application argument
            var port = args.Length > 0 ? args[0] : "52026";

            var channel = new Channel("localhost:" + port, ChannelCredentials.Insecure);

            var hostInfoClient = new HostInfoService.HostInfoServiceClient(channel);
            var hostInfoResponse = await hostInfoClient.GetHostInfoAsync(new GetHostInfoRequest());
            Console.WriteLine();
            Console.WriteLine("== Host Info ==");
            Console.WriteLine();
            Console.WriteLine($"{hostInfoResponse.HostInfo.Name} v{hostInfoResponse.HostInfo.Version}");
            Console.WriteLine($"{hostInfoResponse.HostInfo.VendorInfo.Name} ({hostInfoResponse.HostInfo.VendorInfo.Url})");

            var adaptersClient = new AdaptersService.AdaptersServiceClient(channel);
            var adaptersResponse = await adaptersClient.GetAdaptersAsync(new GetAdapterRequest());
            Console.WriteLine();
            Console.WriteLine("== Adapters ==");
            foreach (var adapter in adaptersResponse.Adapters) {
                Console.WriteLine();
                Console.WriteLine($"{adapter.AdapterDescriptor.Name} (ID: {adapter.AdapterDescriptor.Id})");
                Console.WriteLine($"    Description: {adapter.AdapterDescriptor.Description}");
                Console.WriteLine("    Features:");
                foreach (var feature in adapter.Features) {
                    Console.WriteLine($"          {feature}");
                }
            }

            var adapterId = adaptersResponse.Adapters.First().AdapterDescriptor.Id;

            Console.WriteLine();
            Console.WriteLine("== Real-Time Data ==");
            Console.WriteLine();

            var tagValuesClient = new TagValuesService.TagValuesServiceClient(channel);
            var createChannelRequest = new CreateSnapshotPushChannelRequest() {
                AdapterId = adapterId
            };
            createChannelRequest.Tags.AddRange(new[] {
                "Tag1",
                "Tag2",
            });

            using (var snapshotChannel = tagValuesClient.CreateSnapshotPushChannel(createChannelRequest)) {
                _ = Task.Run(async () => {
                    await Task.Delay(5000);

                    // Test out addinga new tag to the subscription.
                    Console.WriteLine($"Adding Tag3 to the subscription...");
                    var addTagsRequest = new AddTagsToSnapshotPushChannelRequest() {
                        AdapterId = adapterId
                    };
                    addTagsRequest.Tags.Add("Tag3");
                    var addTagsResponse = await tagValuesClient.AddTagsToSnapshotPushChannelAsync(addTagsRequest);
                    Console.WriteLine($"Now subscribed to {addTagsResponse.Count} tags!");

                    await Task.Delay(5000);

                    // Now test out removing tags from the subscription
                    Console.WriteLine("Removing Tag1 from the subscription...");
                    var removeTagsRequest = new RemoveTagsFromSnapshotPushChannelRequest() {
                        AdapterId = adapterId
                    };
                    removeTagsRequest.Tags.Add("Tag1");
                    var removeTagsResponse = await tagValuesClient.RemoveTagsFromSnapshotPushChannelAsync(removeTagsRequest);
                    Console.WriteLine($"Now subscribed to {removeTagsResponse.Count} tags!");
                });

                while (await snapshotChannel.ResponseStream.MoveNext(default)) {
                    var val = snapshotChannel.ResponseStream.Current;
                    Console.WriteLine($"[{val.TagName}] {val.Value.NumericValue} @ {val.Value.UtcSampleTime.ToDateTime():HH:mm:ss}");
                }
            }

            //Console.WriteLine();
            //Console.WriteLine("== Event Messages ==");
            //Console.WriteLine();

            //var eventsClient = new EventsService.EventsServiceClient(channel);

            //using (var eventsChannel = eventsClient.CreateEventPushChannel(new CreateEventPushChannelRequest() {
            //    AdapterId = adapterId,
            //    Active = true
            //})) {
            //    while (await eventsChannel.ResponseStream.MoveNext(default)) {
            //        var msg = eventsChannel.ResponseStream.Current;
            //        Console.WriteLine($"[{msg.EventMessage.UtcEventTime.ToDateTime():HH:mm:ss}] [{msg.EventMessage.Priority}] {msg.EventMessage.Category} >> {msg.EventMessage.Message}");
            //    }
            //}

            Console.WriteLine();

            await channel.ShutdownAsync();
        }
    }
}
