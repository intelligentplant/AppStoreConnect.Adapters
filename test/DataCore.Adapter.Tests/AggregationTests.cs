using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AggregationTests : TestsBase {

        [TestMethod]
        public void CustomDataFunctionShouldBeRegistered() {
            var aggregationHelper = new AggregationHelper();

            var descriptor = new DataFunctionDescriptor(TestContext.TestName, "Test", "A custom function", null);
            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsTrue(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));
        }


        [TestMethod]
        public void CustomDataFunctionRegistrationShouldFailOnSecondAttempt() {
            var aggregationHelper = new AggregationHelper();

            var descriptor = new DataFunctionDescriptor(TestContext.TestName, "Test", "A custom function", null);
            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsTrue(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));

            var descriptor2 = new DataFunctionDescriptor(TestContext.TestName, "Test2", "A custom function", null);
            var registered2 = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsFalse(registered2);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id) && x.Name.Equals(descriptor.Name)));
        }


        [TestMethod]
        public void CustomDataFunctionShouldBeUnregistered() {
            var aggregationHelper = new AggregationHelper();
            var descriptor = new DataFunctionDescriptor(TestContext.TestName, "Test", "A custom function", null);

            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsTrue(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));

            var unregistered = aggregationHelper.UnregisterDataFunction(descriptor.Id);
            Assert.IsTrue(unregistered);
            Assert.IsFalse(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));
        }


        [TestMethod]
        public void CustomDataFunctionDeregistrationShouldFailOnSecondAttempt() {
            var aggregationHelper = new AggregationHelper();
            var descriptor = new DataFunctionDescriptor(TestContext.TestName, "Test", "A custom function", null);

            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsTrue(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));

            var unregistered = aggregationHelper.UnregisterDataFunction(descriptor.Id);
            Assert.IsTrue(unregistered);
            Assert.IsFalse(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));

            unregistered = aggregationHelper.UnregisterDataFunction(descriptor.Id);
            Assert.IsFalse(unregistered);
        }


        [TestMethod]
        public void CustomDataFunctionRegistrationShouldFailWhenDefaultFunctionIdIsUsed() {
            var aggregationHelper = new AggregationHelper();

            var descriptor = new DataFunctionDescriptor(DefaultDataFunctions.Average.Id, "Test", "A custom function", null);
            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsFalse(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id) && x.Name.Equals(DefaultDataFunctions.Average.Name)));
        }


        [TestMethod]
        public async Task CustomDataFunctionShouldCalculateValues() {
            var aggregationHelper = new AggregationHelper();
            var descriptor = new DataFunctionDescriptor(TestContext.TestName, "Test", "A custom function", null);

            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                var val = bucket.RawSamples.Count == 0
                    ? 0
                    : bucket.RawSamples.Sum(x => x.Value.GetValueOrDefault(0f));

                return new[] { 
                    TagValueBuilder.Create()
                        .WithUtcSampleTime(bucket.UtcBucketEnd)
                        .WithValue(val)
                        .Build()
                };
            });

            Assert.IsTrue(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var now = DateTime.UtcNow;

            var rawValues = new[] {
                // Bucket 1: no values

                // Bucket 2
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-57)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-50)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-46)).WithValue(1).Build(),

                // Bucket 3: no values

                // Bucket 4
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-30)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-20)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-16)).WithValue(1).Build()

                // Bucket 5: no values
            };
            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var aggValues = (await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { descriptor.Id },
                now.AddSeconds(-60),
                now,
                TimeSpan.FromSeconds(15),
                rawData
            ).ToEnumerable().TimeoutAfter(TimeSpan.FromSeconds(30))).ToArray();

            var expectedResults = new ValueTuple<DateTime, double>[] {
                (now.AddSeconds(-60), 0),
                (now.AddSeconds(-45), 3),
                (now.AddSeconds(-30), 0),
                (now.AddSeconds(-15), 3),
                (now, 0)
            };

            Assert.AreEqual(expectedResults.Length, aggValues.Length, "Unexpected sample count.");

            for (var i = 0; i < expectedResults.Length; i++) {
                var sample = aggValues[i];
                Assert.AreEqual(descriptor.Id, sample.DataFunction);

                var expectedResult = expectedResults[i];

                Assert.AreEqual(expectedResult.Item1, sample.Value.UtcSampleTime);
                Assert.AreEqual(expectedResult.Item2, sample.Value.Value.GetValueOrDefault<double>(), $"Sample Time: {sample.Value.UtcSampleTime:dd-MMM-yy HH:mm:ss}");
            }
        }

    }

}
