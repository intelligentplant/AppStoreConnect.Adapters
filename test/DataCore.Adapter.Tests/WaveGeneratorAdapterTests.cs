using System;
using System.Linq;

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
            return ActivatorUtilities.CreateInstance<WaveGeneratorAdapter>(serviceProvider, context.TestName, new WaveGeneratorAdapterOptions() { 
                EnableAdHocGenerators = true
            });
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

    }
}
