using System;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.WaveGenerator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class WaveGeneratorAdapterTests : AdapterTestsBase<WaveGeneratorAdapter> {

        private static readonly string[] s_defaultTagNames = {
            nameof(WaveType.Sawtooth),
            nameof(WaveType.Sinusoid),
            nameof(WaveType.Square),
            nameof(WaveType.Triangle)
        };


        protected override IServiceScope CreateServiceScope(TestContext context) {
            return AssemblyInitializer.ApplicationServices.CreateScope();
        }


        protected override WaveGeneratorAdapter CreateAdapter(TestContext context, IServiceProvider serviceProvider) {
            var options = new WaveGeneratorAdapterOptions() {
                EnableAdHocGenerators = !string.Equals(context.TestName, nameof(WaveGeneratorShouldNotAllowAdHocTags))
            };

            return ActivatorUtilities.CreateInstance<WaveGeneratorAdapter>(serviceProvider, context.TestName, options);
        }


        protected override FindTagsRequest CreateFindTagsRequest(TestContext context) {
            return new FindTagsRequest();
        }


        protected override GetTagsRequest CreateGetTagsRequest(TestContext context) {
            return new GetTagsRequest() { 
                Tags = s_defaultTagNames
            };
        }


        protected override ReadSnapshotTagValuesRequest CreateReadSnapshotTagValuesRequest(TestContext context) {
            return new ReadSnapshotTagValuesRequest() {
                Tags = s_defaultTagNames
            };
        }


        protected override CreateSnapshotTagValueSubscriptionRequest CreateSnapshotTagValueSubscriptionRequest(TestContext context) {
            return new CreateSnapshotTagValueSubscriptionRequest() { 
                Tags = s_defaultTagNames
            };
        }


        protected override ReadRawTagValuesRequest CreateReadRawTagValuesRequest(TestContext context) {
            var now = DateTime.UtcNow;

            return new ReadRawTagValuesRequest() { 
                Tags = s_defaultTagNames,
                UtcStartTime = now.AddDays(-1),
                UtcEndTime = now
            };
        }


        protected override ReadPlotTagValuesRequest CreateReadPlotTagValuesRequest(TestContext context) {
            var now = DateTime.UtcNow;

            return new ReadPlotTagValuesRequest() {
                Tags = s_defaultTagNames,
                UtcStartTime = now.AddDays(-1),
                UtcEndTime = now,
                Intervals = 50
            };
        }


        protected override ReadProcessedTagValuesRequest CreateReadProcessedTagValuesRequest(TestContext context) {
            var now = DateTime.UtcNow;

            return new ReadProcessedTagValuesRequest() {
                Tags = s_defaultTagNames,
                UtcStartTime = now.AddDays(-1),
                UtcEndTime = now,
                SampleInterval = TimeSpan.FromHours(4),
                DataFunctions = RealTimeData.Utilities.AggregationHelper.GetDefaultDataFunctions().Select(x => x.Id).ToArray()
            };
        }


        protected override ReadTagValuesAtTimesRequest CreateReadTagValuesAtTimesRequest(TestContext context) {
            var now = DateTime.UtcNow;

            return new ReadTagValuesAtTimesRequest() { 
                Tags = s_defaultTagNames,
                UtcSampleTimes = new [] {
                    now.AddHours(-7.443),
                    now.AddHours(-4),
                    now.AddHours(-2.99999999),
                    now.AddHours(-0.1)
                }
            };
        }


        [TestMethod]
        public Task WaveGeneratorShouldAllowAdHocTags() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var generatorLiteral = "Type=Sawtooth;Period=180;Amplitude=500";

                var feature = adapter.GetFeature<ITagInfo>();
                var tagChannel = await feature.GetTags(context, new GetTagsRequest() {
                    Tags = new[] { generatorLiteral }
                }, ct).ConfigureAwait(false);

                var tags = await tagChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, tags.Count());
                Assert.AreEqual(generatorLiteral, tags.First().Id);
            });
        }


        [TestMethod]
        public Task WaveGeneratorShouldNotAllowAdHocTags() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var generatorLiteral = "Type=Sawtooth;Period=180;Amplitude=500";

                var feature = adapter.GetFeature<ITagInfo>();
                Assert.IsNotNull(feature);
                var tagChannel = await feature.GetTags(context, new GetTagsRequest() {
                    Tags = new[] { generatorLiteral }
                }, ct).ConfigureAwait(false);

                var tags = await tagChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(0, tags.Count());
            });
        }


        [DataTestMethod]
        [DataRow(WaveType.Sinusoid)]
        [DataRow(WaveType.Sawtooth)]
        [DataRow(WaveType.Square)]
        [DataRow(WaveType.Triangle)]
        public Task PhaseShouldBeAppliedToBaseFunction(WaveType waveType) {
            return RunAdapterTest(async (adapter, context, ct) => {
                double phase = 90; // seconds
                var baseFunc = waveType.ToString();
                var offsetFunc = $"Type={waveType};Phase={phase}";

                var now = DateTime.UtcNow;
                var feature = adapter.GetFeature<IReadTagValuesAtTimes>();

                var baseFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { baseFunc },
                    UtcSampleTimes = new[] { now.AddSeconds(phase) }
                }, ct).ConfigureAwait(false);

                var baseFuncValues = await baseFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, baseFuncValues.Count());

                var offsetFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { offsetFunc },
                    UtcSampleTimes = new[] { now }
                }, ct).ConfigureAwait(false);

                var offsetFuncValues = await offsetFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, offsetFuncValues.Count());

                Assert.AreEqual(baseFuncValues.First().Value.Value, offsetFuncValues.First().Value.Value);
            });
        }


        [DataTestMethod]
        [DataRow(WaveType.Sinusoid)]
        [DataRow(WaveType.Sawtooth)]
        [DataRow(WaveType.Square)]
        [DataRow(WaveType.Triangle)]
        public Task AmplitudeShouldBeAppliedToBaseFunction(WaveType waveType) {
            return RunAdapterTest(async (adapter, context, ct) => {
                double amplitude = 5;
                var baseFunc = waveType.ToString();
                var offsetFunc = $"Type={waveType};Amplitude={amplitude}";

                var now = DateTime.UtcNow;
                var feature = adapter.GetFeature<IReadTagValuesAtTimes>();

                var baseFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { baseFunc },
                    UtcSampleTimes = new[] { now }
                }, ct).ConfigureAwait(false);

                var baseFuncValues = await baseFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, baseFuncValues.Count());

                var offsetFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { offsetFunc },
                    UtcSampleTimes = new[] { now }
                }, ct).ConfigureAwait(false);

                var offsetFuncValues = await offsetFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, offsetFuncValues.Count());

                Assert.AreEqual(baseFuncValues.First().Value.GetValueOrDefault<double>() * amplitude, offsetFuncValues.First().Value.GetValueOrDefault<double>(), 1 * Math.Pow(10, -16));
            });
        }


        [DataTestMethod]
        [DataRow(WaveType.Sinusoid)]
        [DataRow(WaveType.Sawtooth)]
        [DataRow(WaveType.Square)]
        [DataRow(WaveType.Triangle)]
        public Task OffsetShouldBeAppliedToBaseFunction(WaveType waveType) {
            return RunAdapterTest(async (adapter, context, ct) => {
                double offset = 1.2345;
                var baseFunc = waveType.ToString();
                var offsetFunc = $"Type={waveType};Offset={offset}";

                var now = DateTime.UtcNow;
                var feature = adapter.GetFeature<IReadTagValuesAtTimes>();

                var baseFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { baseFunc },
                    UtcSampleTimes = new[] { now }
                }, ct).ConfigureAwait(false);

                var baseFuncValues = await baseFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, baseFuncValues.Count());

                var offsetFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { offsetFunc },
                    UtcSampleTimes = new[] { now }
                }, ct).ConfigureAwait(false);

                var offsetFuncValues = await offsetFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, offsetFuncValues.Count());

                Assert.AreEqual(baseFuncValues.First().Value.GetValueOrDefault<double>() + offset, offsetFuncValues.First().Value.GetValueOrDefault<double>());
            });
        }


        [DataTestMethod]
        [DataRow(WaveType.Sinusoid)]
        [DataRow(WaveType.Sawtooth)]
        [DataRow(WaveType.Square)]
        [DataRow(WaveType.Triangle)]
        public Task PeriodShouldBeAppliedToBaseFunction(WaveType waveType) {
            return RunAdapterTest(async (adapter, context, ct) => {
                // Default period on base function is 60 minutes.
                double defaultPeriod = TimeSpan.FromMinutes(60).TotalSeconds;
                double period = TimeSpan.FromMinutes(30).TotalSeconds;
                var baseFunc = waveType.ToString();
                var offsetFunc = $"Type={waveType};Period={period}";

                var feature = adapter.GetFeature<IReadTagValuesAtTimes>();

                var baseFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { baseFunc },
                    UtcSampleTimes = new[] { DateTime.UnixEpoch.AddSeconds(defaultPeriod) }
                }, ct).ConfigureAwait(false);

                var baseFuncValues = await baseFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, baseFuncValues.Count());

                var offsetFuncChannel = await feature.ReadTagValuesAtTimes(context, new ReadTagValuesAtTimesRequest() {
                    Tags = new[] { offsetFunc },
                    UtcSampleTimes = new[] { DateTime.UnixEpoch.AddSeconds(period) }
                }, ct).ConfigureAwait(false);

                var offsetFuncValues = await offsetFuncChannel.ToEnumerable(cancellationToken: ct).ConfigureAwait(false);
                Assert.AreEqual(1, offsetFuncValues.Count());

                Assert.AreEqual(baseFuncValues.First().Value.GetValueOrDefault<double>(), offsetFuncValues.First().Value.GetValueOrDefault<double>());
            });
        }

    }
}
