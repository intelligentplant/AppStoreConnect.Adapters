using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Utilities;
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
            return Math.Abs(
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
                    .GetValueOrDefault<double>()
            );
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


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage, nameof(CalculateExpectedAvgValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum, nameof(CalculateExpectedMinValue), nameof(CalculateExpectedMinTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum, nameof(CalculateExpectedMaxValue), nameof(CalculateExpectedMaxTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdCount, nameof(CalculateExpectedCountValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange, nameof(CalculateExpectedRangeValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta, nameof(CalculateExpectedDeltaValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentGood, nameof(CalculateExpectedPercentGoodValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdPercentBad, nameof(CalculateExpectedPercentBadValue), null)]
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage, nameof(CalculateExpectedAvgValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum, nameof(CalculateExpectedMinValue), nameof(CalculateExpectedMinTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum, nameof(CalculateExpectedMaxValue), nameof(CalculateExpectedMaxTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange, nameof(CalculateExpectedRangeValue), null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta, nameof(CalculateExpectedDeltaValue), null)]
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
        }


        [DataTestMethod]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdAverage, null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMinimum, nameof(CalculateExpectedMinTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdMaximum, nameof(CalculateExpectedMaxTimestamp))]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdRange, null)]
        [DataRow(DefaultDataFunctions.Constants.FunctionIdDelta, null)]
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-69)).WithValue(70).WithStatus(TagValueStatus.Bad).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-69)).WithValue(70).WithStatus(TagValueStatus.Bad).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-75)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-57)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-69)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-59)).WithValue(100).WithStatus(TagValueStatus.Bad).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).WithStatus(TagValueStatus.Bad).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-57)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-51)).WithValue(100).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-2)).WithValue(0).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-57)).WithValue(70).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(end.AddSeconds(-50)).WithValue(100).Build()
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

            Assert.IsTrue(val.Value.Properties.Any(p => p.Name.Equals(AggregationHelper.XPoweredByPropertyName)));
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
                    : bucket.RawSamples.Sum(x => x.Value.GetValueOrDefault(0f));

                return new[] { 
                    TagValueBuilder.Create()
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
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-57)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-50)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-46)).WithValue(1).Build(),

                // Bucket 2: no values

                // Bucket 3
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-30)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-20)).WithValue(1).Build(),
                TagValueBuilder.Create().WithUtcSampleTime(now.AddSeconds(-16)).WithValue(1).Build()

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
                Assert.AreEqual(expectedResult.Item2, sample.Value.Value.GetValueOrDefault<double>(), $"Iteration: {i}");
            }
        }

    }

}
