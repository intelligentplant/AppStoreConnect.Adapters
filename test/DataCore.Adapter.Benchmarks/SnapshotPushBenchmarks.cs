using System;
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Benchmarks {

    [MemoryDiagnoser]
    public class SnapshotPushBenchmarks {

        [Params(1, 10, 100)]
        public int TagCount { get; set; }

        [Params(100, 1000, 10000)]
        public int SampleCountPerTag { get; set; }


        [Benchmark]
        public async Task ReadSnapshotValuesFromSubscription() {
            var expectedTotalSampleCount = SampleCountPerTag * TagCount;

            using (var push = new SnapshotPush(SampleCountPerTag))
            using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(30))) {
                var cancellationToken = ctSource.Token;
                var subscription = await push.Subscribe(null);

                var tcs = new TaskCompletionSource<bool>();

                _ = Task.Run(async () => {
                    try {
                        var actualSampleCount = 0;
                        while (!cancellationToken.IsCancellationRequested && actualSampleCount < expectedTotalSampleCount) {
                            await subscription.Reader.WaitToReadAsync(cancellationToken);
                            if (subscription.Reader.TryRead(out var _)) {
                                ++actualSampleCount;
                            }
                        }
                    }
                    catch (OperationCanceledException) {
                        tcs.TrySetCanceled();
                    }
                    catch (Exception e) {
                        tcs.TrySetException(e);
                    }
                    finally {
                        tcs.TrySetResult(true);
                    }
                });

                for (var i = 0; i < TagCount; i++) {
                    await subscription.AddTagToSubscription("Tag_" + (i + 1));
                }

                await tcs.Task;
            }
        }

    }
}
