using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataCore.Adapter.Grpc;
using Grpc.Net.Client;

namespace DataCore.Adapter.GrpcExampleClient {
    class Program {
        static async Task Main(string[] args) {
            // Include port of the gRPC server as an application argument
            var port = args.Length > 0 ? args[0] : "58189";

            var channel = GrpcChannel.ForAddress("https://localhost:" + port);

            var hostInfoClient = new HostInfoService.HostInfoServiceClient(channel);
            var hostInfoResponse = await hostInfoClient.GetHostInfoAsync(new GetHostInfoRequest());
            Console.WriteLine();
            Console.WriteLine("== Host Info ==");
            Console.WriteLine();
            Console.WriteLine($"{hostInfoResponse.HostInfo.Name} v{hostInfoResponse.HostInfo.Version}");
            Console.WriteLine($"{hostInfoResponse.HostInfo.VendorInfo.Name} ({hostInfoResponse.HostInfo.VendorInfo.Url})");

            var adaptersClient = new AdaptersService.AdaptersServiceClient(channel);
            var adaptersResponse = adaptersClient.FindAdapters(new FindAdaptersRequest());
            var adapters = new List<AdapterDescriptor>();

            Console.WriteLine();
            Console.WriteLine("== Adapters ==");
            while (await adaptersResponse.ResponseStream.MoveNext(default).ConfigureAwait(false)) {
                var adapter = adaptersResponse.ResponseStream.Current.Adapter;
                adapters.Add(adapter);

                Console.WriteLine();
                Console.WriteLine($"{adapter.Name} (ID: {adapter.Id})");
                Console.WriteLine($"    Description: {adapter.Description}");
            }
            

            foreach (var adapter in adapters) {

                var adapterId = adapter.Id;

                var tagSearchClient = new TagSearchService.TagSearchServiceClient(channel);
                var tagSearchRequest = new FindTagsRequest() {
                    AdapterId = adapterId,
                    PageSize = 10
                };

                var tags = new List<TagDefinition>();

                using (var tagSearchChannel = tagSearchClient.FindTags(tagSearchRequest)) {
                    while (await tagSearchChannel.ResponseStream.MoveNext(default)) {
                        tags.Add(tagSearchChannel.ResponseStream.Current);
                    }
                }

                //Console.WriteLine();
                //Console.WriteLine("== Real-Time Data ==");
                //Console.WriteLine();

                //var tagValuesClient = new TagValuesService.TagValuesServiceClient(channel);
                //var createChannelRequest = new CreateSnapshotPushChannelRequest() {
                //    AdapterId = adapterId
                //};
                //createChannelRequest.Tags.AddRange(new[] {
                //    tags.First().Id,
                //    "Tag2",
                //});

                //using (var snapshotChannel = tagValuesClient.CreateSnapshotPushChannel(createChannelRequest)) {
                //    _ = Task.Run(async () => {
                //        await Task.Delay(5000);

                //        // Test out addinga new tag to the subscription.
                //        Console.WriteLine($"Adding Tag3 to the subscription...");
                //        var addTagsRequest = new AddTagsToSnapshotPushChannelRequest() {
                //            AdapterId = adapterId
                //        };
                //        addTagsRequest.Tags.Add("Tag3");
                //        var addTagsResponse = await tagValuesClient.AddTagsToSnapshotPushChannelAsync(addTagsRequest);
                //        Console.WriteLine($"Now subscribed to {addTagsResponse.Count} tags!");

                //        await Task.Delay(5000);

                //        // Now test out removing tags from the subscription
                //        Console.WriteLine("Removing Tag1 from the subscription...");
                //        var removeTagsRequest = new RemoveTagsFromSnapshotPushChannelRequest() {
                //            AdapterId = adapterId
                //        };
                //        removeTagsRequest.Tags.Add(tags.First().Id);
                //        var removeTagsResponse = await tagValuesClient.RemoveTagsFromSnapshotPushChannelAsync(removeTagsRequest);
                //        Console.WriteLine($"Now subscribed to {removeTagsResponse.Count} tags!");
                //    });

                //    while (await snapshotChannel.ResponseStream.MoveNext(default)) {
                //        var val = snapshotChannel.ResponseStream.Current;
                //        Console.WriteLine($"[{val.TagName}] {val.Value.NumericValue} @ {val.Value.UtcSampleTime.ToDateTime():HH:mm:ss}");
                //    }
                //}


                Console.WriteLine();
                Console.WriteLine($"== Historical Data ({adapterId}; {tags.Count} tags) ==");

                var tagValuesClient = new TagValuesService.TagValuesServiceClient(channel);
                var now = DateTime.UtcNow;

                var histQueryStart = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(now.AddDays(-1));
                var histQueryEnd = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(now);
                var histQuerySampleInterval = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.FromMinutes(5));
                var histQueryRawPointCount = 5000;
                var histQueryPlotIntervals = 500;

                Console.WriteLine();
                foreach (var tag in tags) {
                    var rawDataRequest = new ReadRawTagValuesRequest() {
                        AdapterId = adapterId,
                        SampleCount = histQueryRawPointCount,
                        BoundaryType = RawDataBoundaryType.Inside,
                        UtcStartTime = histQueryStart,
                        UtcEndTime = histQueryEnd
                    };
                    rawDataRequest.Tags.AddRange(new[] { tag.Id });

                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    using (var rawDataChannel = tagValuesClient.ReadRawTagValues(rawDataRequest)) {
                        long samplesRead = 0;
                        var earliestSample = DateTime.MaxValue;
                        var latestSample = DateTime.MinValue;

                        while (await rawDataChannel.ResponseStream.MoveNext(default)) {
                            ++samplesRead;
                            var sampleTime = rawDataChannel.ResponseStream.Current.Value.UtcSampleTime.ToDateTime();
                            if (sampleTime < earliestSample) {
                                earliestSample = sampleTime;
                            }
                            if (sampleTime > latestSample) {
                                latestSample = sampleTime;
                            }
                        }

                        Console.WriteLine($"[{tag.Name}] Read {samplesRead} raw samples ({earliestSample:dd-MMM-yy HH:mm:ss} - {latestSample:dd-MMM-yy HH:mm:ss}) in {timer.Elapsed}");
                    }
                }

                Console.WriteLine();
                foreach (var tag in tags) {
                    var plotDataRequest = new ReadPlotTagValuesRequest() {
                        AdapterId = adapterId,
                        Intervals = histQueryPlotIntervals,
                        UtcStartTime = histQueryStart,
                        UtcEndTime = histQueryEnd
                    };
                    plotDataRequest.Tags.AddRange(new[] { tag.Id });

                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    using (var plotDataChannel = tagValuesClient.ReadPlotTagValues(plotDataRequest)) {
                        long samplesRead = 0;
                        var earliestSample = DateTime.MaxValue;
                        var latestSample = DateTime.MinValue;

                        while (await plotDataChannel.ResponseStream.MoveNext(default)) {
                            ++samplesRead;
                            var sampleTime = plotDataChannel.ResponseStream.Current.Value.UtcSampleTime.ToDateTime();
                            if (sampleTime < earliestSample) {
                                earliestSample = sampleTime;
                            }
                            if (sampleTime > latestSample) {
                                latestSample = sampleTime;
                            }
                        }

                        Console.WriteLine($"[{tag.Name}] Read {samplesRead} plot samples ({earliestSample:dd-MMM-yy HH:mm:ss} - {latestSample:dd-MMM-yy HH:mm:ss}) in {timer.Elapsed}");
                    }
                }

                Console.WriteLine();
                foreach (var tag in tags) {
                    var aggDataRequest = new ReadProcessedTagValuesRequest() {
                        AdapterId = adapterId,
                        SampleInterval = histQuerySampleInterval,
                        UtcStartTime = histQueryStart,
                        UtcEndTime = histQueryEnd
                    };
                    aggDataRequest.DataFunctions.AddRange(new[] { "INTERP", "AVG", "MIN", "MAX" });
                    aggDataRequest.Tags.AddRange(new[] { tag.Id });

                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    using (var aggDataChannel = tagValuesClient.ReadProcessedTagValues(aggDataRequest)) {
                        long samplesRead = 0;
                        var earliestSample = DateTime.MaxValue;
                        var latestSample = DateTime.MinValue;

                        while (await aggDataChannel.ResponseStream.MoveNext(default)) {
                            ++samplesRead;
                            var sampleTime = aggDataChannel.ResponseStream.Current.Value.UtcSampleTime.ToDateTime();
                            if (sampleTime < earliestSample) {
                                earliestSample = sampleTime;
                            }
                            if (sampleTime > latestSample) {
                                latestSample = sampleTime;
                            }
                        }

                        Console.WriteLine($"[{tag.Name}] Read {samplesRead} aggregated samples ({earliestSample:dd-MMM-yy HH:mm:ss} - {latestSample:dd-MMM-yy HH:mm:ss}) in {timer.Elapsed}");
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
            }

            Console.WriteLine();

            channel.Dispose();
        }
    }
}
