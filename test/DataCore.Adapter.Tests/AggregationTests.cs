using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Utilities;
using DataCore.Adapter.Tags;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AggregationTests : TestsBase {

        public static double CalculateExpectedAvgValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Where(x => x.Status == TagValueStatus.Good)
                .Average(x => x.GetValueOrDefault<double>());
        }


        public static double CalculateExpectedMinValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Where(x => x.Status == TagValueStatus.Good)
                .Min(x => x.GetValueOrDefault<double>());
        }


        public static DateTime? CalculateExpectedMinTimestamp(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Where(x => x.Status == TagValueStatus.Good)
                .OrderBy(x => x.GetValueOrDefault<double>())
                .FirstOrDefault()?.UtcSampleTime;
        }


        public static double CalculateExpectedMaxValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Where(x => x.Status == TagValueStatus.Good)
                .Max(x => x.GetValueOrDefault<double>());
        }


        public static DateTime? CalculateExpectedMaxTimestamp(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Where(x => x.Status == TagValueStatus.Good)
                .OrderByDescending(x => x.GetValueOrDefault<double>())
                .FirstOrDefault()?.UtcSampleTime;
        }


        public static double CalculateExpectedCountValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Count();
        }


        public static double CalculateExpectedRangeValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return Math.Abs(CalculateExpectedMaxValue(values, bucketStart, bucketEnd) - CalculateExpectedMinValue(values, bucketStart, bucketEnd));
        }


        public static double CalculateExpectedDeltaValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            return
                values
                    .Where(x => x.UtcSampleTime >= bucketStart)
                    .Where(x => x.UtcSampleTime < bucketEnd)
                    .Where(x => x.Status == TagValueStatus.Good)
                    .First()
                    .GetValueOrDefault<double>() 
                    
                - 
                
                values
                    .Where(x => x.UtcSampleTime >= bucketStart)
                    .Where(x => x.UtcSampleTime < bucketEnd)
                    .Where(x => x.Status == TagValueStatus.Good)
                    .Last()
                    .GetValueOrDefault<double>();
        }


        public static double CalculateExpectedPercentGoodValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            var bucketValues = values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd);
            return ((double) bucketValues.Count(x => x.Status == TagValueStatus.Good)) / bucketValues.Count() * 100;
        }


        public static double CalculateExpectedPercentBadValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            var bucketValues = values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd);
            return ((double) bucketValues.Count(x => x.Status == TagValueStatus.Bad)) / bucketValues.Count() * 100;
        }


        public static double CalculateExpectedVarianceValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            var bucketValues = values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Where(x => x.Status == TagValueStatus.Good);

            if (bucketValues.Count() < 2) {
                return 0;
            }

            var avg = CalculateExpectedAvgValue(bucketValues, bucketStart, bucketEnd);
            return bucketValues.Sum(x => Math.Pow(x.GetValueOrDefault<double>() - avg, 2)) / (bucketValues.Count() - 1);
        }


        public static double CalculateExpectedStandardDeviationValue(IEnumerable<TagValueExtended> values, DateTime bucketStart, DateTime bucketEnd) {
            var bucketValues = values
                .Where(x => x.UtcSampleTime >= bucketStart)
                .Where(x => x.UtcSampleTime < bucketEnd)
                .Where(x => x.Status == TagValueStatus.Good);

            if (bucketValues.Count() < 2) {
                return 0;
            }
            return Math.Sqrt(
                CalculateExpectedVarianceValue(bucketValues, bucketStart, bucketEnd)
            );
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage, nameof(CalculateExpectedAvgValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum, nameof(CalculateExpectedMinValue), nameof(CalculateExpectedMinTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum, nameof(CalculateExpectedMaxValue), nameof(CalculateExpectedMaxTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdCount, nameof(CalculateExpectedCountValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange, nameof(CalculateExpectedRangeValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta, nameof(CalculateExpectedDeltaValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentGood, nameof(CalculateExpectedPercentGoodValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentBad, nameof(CalculateExpectedPercentBadValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdVariance, nameof(CalculateExpectedVarianceValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdStandardDeviation, nameof(CalculateExpectedStandardDeviationValue), null)]
        public async Task DefaultDataFunctionShouldCalculateValue(
            string functionId, 
            string expectedValueCalculator,
            string expectedTimestampCalculator
        ) {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag, 
                new[] { functionId }, 
                start, 
                end, 
                interval, 
                rawData
            ).ToEnumerable();

            Assert.AreEqual(1, values.Count());

            var val = values.First();
            Assert.AreEqual(functionId, val.DataFunction);
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedValue = (double) GetType().GetMethod(expectedValueCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end });

            var expectedSampleTime = string.IsNullOrWhiteSpace(expectedTimestampCalculator)
                ? start
                : ((DateTime?) GetType().GetMethod(expectedTimestampCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end })) ?? start;

            Assert.AreEqual(expectedValue, val.Value.GetValueOrDefault<double>());
            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Good, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage, nameof(CalculateExpectedAvgValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum, nameof(CalculateExpectedMinValue), nameof(CalculateExpectedMinTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum, nameof(CalculateExpectedMaxValue), nameof(CalculateExpectedMaxTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdCount, nameof(CalculateExpectedCountValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange, nameof(CalculateExpectedRangeValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta, nameof(CalculateExpectedDeltaValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentGood, nameof(CalculateExpectedPercentGoodValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentBad, nameof(CalculateExpectedPercentBadValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdVariance, nameof(CalculateExpectedVarianceValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdStandardDeviation, nameof(CalculateExpectedStandardDeviationValue), null)]
        public async Task DefaultDataFunctionShouldCalculateValueWhenRequestedUsingName(
            string functionId,
            string expectedValueCalculator,
            string expectedTimestampCalculator
        ) {
            var aggregationHelper = new AggregationHelper();
            var func = aggregationHelper.GetSupportedDataFunctions().FirstOrDefault(x => x.IsMatch(functionId));
            Assert.IsNotNull(func);

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { func.Name },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            Assert.AreEqual(1, values.Count());

            var val = values.First();
            Assert.AreEqual(func.Name, val.DataFunction);
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedValue = (double) GetType().GetMethod(expectedValueCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end });

            var expectedSampleTime = string.IsNullOrWhiteSpace(expectedTimestampCalculator)
                ? start
                : ((DateTime?) GetType().GetMethod(expectedTimestampCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end })) ?? start;

            Assert.AreEqual(expectedValue, val.Value.GetValueOrDefault<double>());
            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Good, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage, nameof(CalculateExpectedAvgValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum, nameof(CalculateExpectedMinValue), nameof(CalculateExpectedMinTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum, nameof(CalculateExpectedMaxValue), nameof(CalculateExpectedMaxTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange, nameof(CalculateExpectedRangeValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta, nameof(CalculateExpectedDeltaValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdVariance, nameof(CalculateExpectedVarianceValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdStandardDeviation, nameof(CalculateExpectedStandardDeviationValue), null)]
        public async Task DefaultDataFunctionShouldFilterNonGoodInputValuesAndReturnUncertainStatus(
            string functionId,
            string expectedValueCalculator,
            string expectedTimestampCalculator
        ) {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { functionId },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            Assert.AreEqual(1, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedValue = (double) GetType().GetMethod(expectedValueCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end });

            var expectedSampleTime = string.IsNullOrWhiteSpace(expectedTimestampCalculator)
                ? start
                : ((DateTime?) GetType().GetMethod(expectedTimestampCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end })) ?? start;

            Assert.AreEqual(expectedValue, val.Value.GetValueOrDefault<double>());
            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage, null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum, nameof(CalculateExpectedMinTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum, nameof(CalculateExpectedMaxTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange, null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta, null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdVariance, null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdStandardDeviation, null)]
        public async Task DefaultDataFunctionShouldReturnErrorValueWhenNoGoodInputValuesAreProvided(
            string functionId,
            string expectedTimestampCalculator
        ) {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { functionId },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            Assert.AreEqual(1, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = string.IsNullOrWhiteSpace(expectedTimestampCalculator)
                ? start
                : ((DateTime?) GetType().GetMethod(expectedTimestampCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end })) ?? start;

            Assert.IsFalse(string.IsNullOrWhiteSpace(val.Value.Error));
            Assert.AreEqual(double.NaN, val.Value.GetValueOrDefault(double.NaN));
            Assert.AreEqual(TagValueStatus.Bad, val.Value.Status);
            Assert.AreEqual(start, val.Value.UtcSampleTime);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentGood, nameof(CalculateExpectedPercentGoodValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentBad, nameof(CalculateExpectedPercentBadValue), null)]
        public async Task DefaultDataFunctionShouldStillReturnGoodStatusWhenSomeNonGoodInputValuesAreProvided(
            string functionId,
            string expectedValueCalculator,
            string expectedTimestampCalculator
        ) {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { functionId },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            Assert.AreEqual(1, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedValue = (double) GetType().GetMethod(expectedValueCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end });

            var expectedSampleTime = string.IsNullOrWhiteSpace(expectedTimestampCalculator)
                ? start
                : ((DateTime?) GetType().GetMethod(expectedTimestampCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end })) ?? start;

            Assert.AreEqual(expectedValue, val.Value.GetValueOrDefault<double>());
            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Good, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentGood, nameof(CalculateExpectedPercentGoodValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentBad, nameof(CalculateExpectedPercentBadValue), null)]
        public async Task DefaultDataFunctionShouldStillCalculateWhenNoGoodInputValuesAreProvided(
            string functionId,
            string expectedValueCalculator,
            string expectedTimestampCalculator
        ) {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { functionId },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            Assert.AreEqual(1, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedValue = (double) GetType().GetMethod(expectedValueCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end });

            var expectedSampleTime = string.IsNullOrWhiteSpace(expectedTimestampCalculator)
                ? start
                : ((DateTime?) GetType().GetMethod(expectedTimestampCalculator, BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { rawValues, start, end })) ?? start;

            Assert.AreEqual(expectedValue, val.Value.GetValueOrDefault<double>());
            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Good, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public void InterpolatedValueShouldBeCalculatedCorrectly() {
            var now = DateTime.UtcNow;

            (DateTime, double) point0 = (now.AddDays(-1), 0);
            (DateTime, double) point1 = (now.AddDays(1), 100);

            var expectedValue = 50;
            var actualValue = InterpolationHelper.InterpolateValue(
                now.Ticks, 
                point0.Item1.Ticks, 
                point1.Item1.Ticks, 
                point0.Item2, 
                point1.Item2
            );

            Assert.AreEqual(expectedValue, actualValue);
        }


        [TestMethod]
        public void ForwardsExtrapolatedValueShouldBeCalculatedCorrectly() {
            var now = DateTime.UtcNow;

            (DateTime, double) point0 = (now.AddDays(-1), 0);
            (DateTime, double) point1 = (now.AddDays(1), 100);

            var expectedValue = 150;
            var actualValue = InterpolationHelper.InterpolateValue(
                now.AddDays(2).Ticks,
                point0.Item1.Ticks,
                point1.Item1.Ticks,
                point0.Item2,
                point1.Item2
            );

            Assert.AreEqual(expectedValue, actualValue);
        }


        [TestMethod]
        public void BackwardsExtrapolatedValueShouldBeCalculatedCorrectly() {
            var now = DateTime.UtcNow;

            (DateTime, double) point0 = (now.AddDays(-1), 0);
            (DateTime, double) point1 = (now.AddDays(1), 100);

            var expectedValue = -50;
            var actualValue = InterpolationHelper.InterpolateValue(
                now.AddDays(-2).Ticks,
                point0.Item1.Ticks,
                point1.Item1.Ticks,
                point0.Item2,
                point1.Item2
            );

            Assert.AreEqual(expectedValue, actualValue);
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateGoodQualityFirstValue() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = start;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Good, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateUncertainQualityFinalValue() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.Last();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = end;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateNonGoodQualityValueWhenLowerBoundaryRegionHasUncertainStatus() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            // The boundary region comprises the region between the last good-quality value before 
            // the boundary time. It has uncertain status if a non-good value lies after the last 
            // good value.

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-69)).WithValue(70).WithStatus(TagValueStatus.Bad).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = start;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateNonGoodQualityValueWhenLowerBoundaryValueHasUncertainStatus() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-69)).WithValue(70).WithStatus(TagValueStatus.Bad).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = start;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateNonGoodQualityValueWhenUpperBoundaryRegionHasUncertainStatus() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            // The boundary region comprises the region between the first good-quality value after 
            // the boundary time. It has uncertain status if a non-good value lies before the first 
            // good value.

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-57)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = start;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateNonGoodQualityValueWhenUpperBoundaryValueHasUncertainStatus() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-69)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = start;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateNonGoodQualityValueWhenExtrapolatingBackwards() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-57)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-51)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = start;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            // Value should have uncertain status because it has been extrapolated.
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public async Task InterpolateShouldCalculateNonGoodQualityValueWhenExtrapolatingForwards() {
            var aggregationHelper = new AggregationHelper();

            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(60);

            var rawValues = new[] {
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-57)).WithValue(70).Build(),
                new TagValueBuilder().WithUtcSampleTime(end.AddSeconds(-50)).WithValue(100).Build()
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var values = await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { DefaultDataFunctions.Interpolate.Id },
                start,
                end,
                interval,
                rawData
            ).ToEnumerable();

            // Values expected at start time and end time.
            Assert.AreEqual(2, values.Count());

            var val = values.First();
            Assert.AreEqual(tag.Id, val.TagId);
            Assert.AreEqual(tag.Name, val.TagName);

            var expectedSampleTime = start;

            Assert.AreEqual(expectedSampleTime, val.Value.UtcSampleTime);
            // Value should have uncertain status because it has been extrapolated.
            Assert.AreEqual(TagValueStatus.Uncertain, val.Value.Status);

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(CommonTagValuePropertyNames.XPoweredBy)));
        }


        [TestMethod]
        public void CustomDataFunctionShouldBeRegistered() {
            var aggregationHelper = new AggregationHelper();

            var descriptor = new DataFunctionDescriptor(
                TestContext.TestName, 
                "Test", 
                "A custom function", 
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );
            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsTrue(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));
        }


        [TestMethod]
        public void CustomDataFunctionRegistrationShouldFailOnSecondAttempt() {
            var aggregationHelper = new AggregationHelper();

            var descriptor = new DataFunctionDescriptor(
                TestContext.TestName,
                "Test",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );
            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsTrue(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id)));

            var descriptor2 = new DataFunctionDescriptor(
                TestContext.TestName,
                "Test2",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );
            var registered2 = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsFalse(registered2);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id) && x.Name.Equals(descriptor.Name)));
        }


        [TestMethod]
        public void CustomDataFunctionShouldBeUnregistered() {
            var aggregationHelper = new AggregationHelper();
            var descriptor = new DataFunctionDescriptor(
                TestContext.TestName,
                "Test",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );

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
            var descriptor = new DataFunctionDescriptor(
                TestContext.TestName,
                "Test",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );

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

            var descriptor = new DataFunctionDescriptor(
                DefaultDataFunctions.Average.Id,
                "Test",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );
            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                return Array.Empty<TagValueExtended>();
            });

            Assert.IsFalse(registered);
            Assert.IsTrue(aggregationHelper.GetSupportedDataFunctions().Any(x => x.Id.Equals(descriptor.Id) && x.Name.Equals(DefaultDataFunctions.Average.Name)));
        }


        [TestMethod]
        public async Task CustomDataFunctionShouldCalculateValues() {
            var aggregationHelper = new AggregationHelper();
            var descriptor = new DataFunctionDescriptor(
                TestContext.TestName,
                "Test",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );

            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                var val = !bucket.RawSamples.Any()
                    ? 0
                    : bucket.RawSamples.Sum(x => x.GetValueOrDefault(0f));

                return new[] { 
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
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
                // Bucket 1
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-57)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-50)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-46)).WithValue(1).Build(),

                // Bucket 2: no values

                // Bucket 3
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-30)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-20)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-16)).WithValue(1).Build()

                // Bucket 4: no values
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
                (now.AddSeconds(-60), 3),
                (now.AddSeconds(-45), 0),
                (now.AddSeconds(-30), 3),
                (now.AddSeconds(-15), 0),
            };

            Assert.AreEqual(expectedResults.Length, aggValues.Length, "Unexpected sample count.");

            for (var i = 0; i < expectedResults.Length; i++) {
                var sample = aggValues[i];
                Assert.AreEqual(descriptor.Id, sample.DataFunction);

                var expectedResult = expectedResults[i];

                Assert.AreEqual(expectedResult.Item1, sample.Value.UtcSampleTime, $"Iteration: {i}");
                Assert.AreEqual(expectedResult.Item2, sample.Value.GetValueOrDefault<double>(), $"Iteration: {i}");
            }
        }


        [TestMethod]
        public async Task CustomDataFunctionShouldCalculateValuesWhenRequestedUsingName() {
            var aggregationHelper = new AggregationHelper();
            var descriptor = new DataFunctionDescriptor(
                TestContext.TestName,
                "Test",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null
            );

            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                var val = !bucket.RawSamples.Any()
                    ? 0
                    : bucket.RawSamples.Sum(x => x.GetValueOrDefault(0f));

                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
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
                // Bucket 1
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-57)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-50)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-46)).WithValue(1).Build(),

                // Bucket 2: no values

                // Bucket 3
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-30)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-20)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-16)).WithValue(1).Build()

                // Bucket 4: no values
            };
            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var aggValues = (await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { descriptor.Name },
                now.AddSeconds(-60),
                now,
                TimeSpan.FromSeconds(15),
                rawData
            ).ToEnumerable().TimeoutAfter(TimeSpan.FromSeconds(30))).ToArray();

            var expectedResults = new ValueTuple<DateTime, double>[] {
                (now.AddSeconds(-60), 3),
                (now.AddSeconds(-45), 0),
                (now.AddSeconds(-30), 3),
                (now.AddSeconds(-15), 0),
            };

            Assert.AreEqual(expectedResults.Length, aggValues.Length, "Unexpected sample count.");

            for (var i = 0; i < expectedResults.Length; i++) {
                var sample = aggValues[i];
                Assert.AreEqual(descriptor.Name, sample.DataFunction);

                var expectedResult = expectedResults[i];

                Assert.AreEqual(expectedResult.Item1, sample.Value.UtcSampleTime, $"Iteration: {i}");
                Assert.AreEqual(expectedResult.Item2, sample.Value.GetValueOrDefault<double>(), $"Iteration: {i}");
            }
        }


        [TestMethod]
        public async Task CustomDataFunctionShouldCalculateValuesWhenRequestedUsingAlias() {
            const string alias = "TestAlias";

            var aggregationHelper = new AggregationHelper();
            var descriptor = new DataFunctionDescriptor(
                TestContext.TestName,
                "Test",
                "A custom function",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                null,
                new [] {
                    alias
                }
            );

            var registered = aggregationHelper.RegisterDataFunction(descriptor, (tag, bucket) => {
                var val = !bucket.RawSamples.Any()
                    ? 0
                    : bucket.RawSamples.Sum(x => x.GetValueOrDefault(0f));

                return new[] {
                    new TagValueBuilder()
                        .WithUtcSampleTime(bucket.UtcBucketStart)
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
                // Bucket 1
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-57)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-50)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-46)).WithValue(1).Build(),

                // Bucket 2: no values

                // Bucket 3
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-30)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-20)).WithValue(1).Build(),
                new TagValueBuilder().WithUtcSampleTime(now.AddSeconds(-16)).WithValue(1).Build()

                // Bucket 4: no values
            };
            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).ToArray();

            var aggValues = (await aggregationHelper.GetAggregatedValues(
                tag,
                new[] { alias },
                now.AddSeconds(-60),
                now,
                TimeSpan.FromSeconds(15),
                rawData
            ).ToEnumerable().TimeoutAfter(TimeSpan.FromSeconds(30))).ToArray();

            var expectedResults = new ValueTuple<DateTime, double>[] {
                (now.AddSeconds(-60), 3),
                (now.AddSeconds(-45), 0),
                (now.AddSeconds(-30), 3),
                (now.AddSeconds(-15), 0),
            };

            Assert.AreEqual(expectedResults.Length, aggValues.Length, "Unexpected sample count.");

            for (var i = 0; i < expectedResults.Length; i++) {
                var sample = aggValues[i];
                Assert.AreEqual(alias, sample.DataFunction);

                var expectedResult = expectedResults[i];

                Assert.AreEqual(expectedResult.Item1, sample.Value.UtcSampleTime, $"Iteration: {i}");
                Assert.AreEqual(expectedResult.Item2, sample.Value.GetValueOrDefault<double>(), $"Iteration: {i}");
            }
        }


        [TestMethod]
        public void PlotHelperShouldCalculateBucketSize() {
            var end = DateTime.UtcNow;
            var start = end.AddDays(-1);

            var bucketSize = PlotHelper.CalculateBucketSize(start, end, 3);
            Assert.AreEqual(TimeSpan.FromHours(8), bucketSize);
        }


        [TestMethod]
        public async Task PlotHelperShouldSelectCorrectValues() {
            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.Parse("2022-11-02T17:20:00Z", null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(20);

            var rawValues = new[] {
                // Bucket 1: 0-20s
                new TagValueBuilder().WithUtcSampleTime(start).WithValue(70).Build(), // earliest
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(7)).WithValue(100).Build(), // max + midpoint
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(14)).WithValue(0).Build(), // min
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(15)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(19)).WithValue(100).Build(), // latest
                // Bucket 2: 20-40s
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(21)).WithValue(1.883).Build(), // earliest + min
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(27)).WithValue(77.765).Build(), // midpoint
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(39)).WithValue(77.766).Build(), // latest + max
                // Bucket 3: 40-60s
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(41)).WithValue(88).Build(), // earliest
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(47)).WithValue(13).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(49)).WithValue(35).Build(), // midpoint
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(53)).WithValue(116).Build(), // max
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(55)).WithValue(0.8867).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(56)).WithValue(23).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(58)).WithValue(44.444).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(59)).WithValue(0.556).Build(), // latest + min
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).PublishToChannel();

            var plotChannel = PlotHelper.GetPlotValues(tag, start, end, interval, rawData.ReadAllAsync(CancellationToken));
            var plotValues = new List<TagValueQueryResult>();
            await foreach(var val in plotChannel.WithCancellation(CancellationToken).ConfigureAwait(false)) {
                plotValues.Add(val);
            }

            Assert.AreEqual(11, plotValues.Count);
        }


        [TestMethod]
        public async Task PlotHelperShouldHandleEmptyTimeBuckets() {
            var tag = new TagSummary(
                TestContext.TestName,
                TestContext.TestName,
                null,
                null,
                VariantType.Double
            );

            var end = DateTime.UtcNow;
            var start = end.AddSeconds(-60);
            var interval = TimeSpan.FromSeconds(20);

            var rawValues = new[] {
                // Bucket 1: 0-20s
                new TagValueBuilder().WithUtcSampleTime(start).WithValue(70).Build(), // earliest
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(7)).WithValue(100).Build(), // max + midpoint
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(14)).WithValue(0).Build(), // min
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(15)).WithValue(100).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(19)).WithValue(100).Build(), // latest
                // Bucket 2: 20-40s
                // Bucket 3: 40-60s
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(41)).WithValue(88).Build(), // earliest
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(47)).WithValue(13).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(49)).WithValue(35).Build(), // midpoint
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(53)).WithValue(116).Build(), // max
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(55)).WithValue(0.8867).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(56)).WithValue(23).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(58)).WithValue(44.444).Build(),
                new TagValueBuilder().WithUtcSampleTime(start.AddSeconds(59)).WithValue(0.556).Build(), // latest + min
            };

            var rawData = rawValues.Select(x => TagValueQueryResult.Create(tag.Id, tag.Name, x)).PublishToChannel();

            var plotChannel = PlotHelper.GetPlotValues(tag, start, end, interval, rawData.ReadAllAsync(CancellationToken));
            var plotValues = new List<TagValueQueryResult>();
            await foreach (var val in plotChannel.WithCancellation(CancellationToken).ConfigureAwait(false)) {
                plotValues.Add(val);
            }
            
            Assert.AreEqual(8, plotValues.Count);
        }

    }

}
