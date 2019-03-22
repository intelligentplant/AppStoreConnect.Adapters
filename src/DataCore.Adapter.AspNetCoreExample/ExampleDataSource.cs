using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.DataSource.Features;
using DataCore.Adapter.DataSource.Models;
using DataCore.Adapter.DataSource.Utilities;
using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter.AspNetCoreExample {
    public class ExampleDataSource: IHostedService, IAdapter, ITagSearch, IReadSnapshotTagValues, IReadInterpolatedTagValues, IReadPlotTagValues, IReadProcessedTagValues, IReadRawTagValues, IReadTagValuesAtTimes, IReadTagValueAnnotations {

        private readonly AdapterDescriptor _descriptor = new AdapterDescriptor(
            "8FE69877-9074-4B51-BF86-AFAE4F28CCBD",
            "Example",
            "An example data source adapter"
        );

        private readonly IAdapterFeaturesCollection _features;

        private const string Tag1Id = "3D3CB0C7-3578-46E8-B4C1-5BFBA563BF48";

        private const string Tag2Id = "7C16DA6A-1802-42E4-8473-B2FB504968EE";

        private const string Tag3Id = "67360992-B195-4DDD-8947-4D7C09737966";

        private readonly TagDefinition[] _tags = {
            new TagDefinition(Tag1Id, "Tag1", "This is an example tag", null, TagDataType.Numeric, null, null),
            new TagDefinition(Tag2Id, "Tag2", "This is an example tag with units specified", "deg C", TagDataType.Numeric, null, null),
            new TagDefinition(Tag3Id, "Tag3", "This is an example tag with units and bespoke properties", null, TagDataType.Numeric, null, new Dictionary<string, string>() {
                { "utcCreatedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
            }),
        };

        private readonly ConcurrentDictionary<string, SortedList<DateTime, TagValue>> _rawValues = new ConcurrentDictionary<string, SortedList<DateTime, TagValue>>();

        private readonly List<DateTime> _utcSampleTimes = new List<DateTime>();

        /// <summary>
        /// The earliest UTC sample time in the loaded file.
        /// </summary>
        private DateTime _earliestSampleTimeUtc;

        /// <summary>
        /// The latest UTC sample time in the loaded file.
        /// </summary>
        private DateTime _latestSampleTimeUtc;

        /// <summary>
        /// The total duration of the data in the loaded file.
        /// </summary>
        private TimeSpan _dataSetDuration;

        private readonly ReadHistoricalTagValuesHelper _historicalQueryHelper;

        AdapterDescriptor IAdapter.Descriptor {
            get { return _descriptor; }
        }


        IAdapterFeaturesCollection IAdapter.Features {
            get { return _features; }
        }


        public ExampleDataSource() {
            _features = new AdapterFeatures(this);
            _historicalQueryHelper = new ReadHistoricalTagValuesHelper(this, this);
            LoadTagValuesFromCsv();
        }


        Task IHostedService.StartAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        Task<IEnumerable<TagDefinition>> ITagSearch.FindTags(IDataCoreContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            var result = _tags.ApplyFilter(request).ToArray();
            return Task.FromResult<IEnumerable<TagDefinition>>(result);
        }


        Task<IEnumerable<TagDefinition>> ITagSearch.GetTags(IDataCoreContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            var result = request
                .Tags
                .Select(nameOrId => _tags.FirstOrDefault(t => t.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)))
                .Where(tag => tag != null)
                .ToArray();

            return Task.FromResult<IEnumerable<TagDefinition>>(result);
        }


        Task<IEnumerable<SnapshotTagValue>> IReadSnapshotTagValues.ReadSnapshotTagValues(IDataCoreContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var tags = request.Tags.Select(x => _tags.FirstOrDefault(t => t.Id.Equals(x, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(x, StringComparison.OrdinalIgnoreCase))).Where(x => x != null).ToArray();
            var values = GetSnapshotValues(tags.Select(t => t.Id).ToArray());
            var result = tags.Select(x => new SnapshotTagValue(x.Id, x.Name, values.TryGetValue(x.Id, out var val) ? val : null)).ToArray();
            return Task.FromResult<IEnumerable<SnapshotTagValue>>(result);
        }


        Task<IEnumerable<HistoricalTagValues>> IReadRawTagValues.ReadRawTagValues(IDataCoreContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var tags = request.Tags.Select(x => _tags.FirstOrDefault(t => t.Id.Equals(x, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(x, StringComparison.OrdinalIgnoreCase))).Where(x => x != null).ToArray();
            var values = GetRawValues(tags.Select(t => t.Id).ToArray(), request.UtcStartTime, request.UtcEndTime, request.BoundaryType, request.SampleCount);
            var result = tags.Select(x => new HistoricalTagValues(x.Id, x.Name, values.TryGetValue(x.Id, out var val) ? val : null)).ToArray();
            return Task.FromResult<IEnumerable<HistoricalTagValues>>(result);
        }


        Task<IEnumerable<HistoricalTagValues>> IReadPlotTagValues.ReadPlotTagValues(IDataCoreContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadPlotTagValues(context, request, cancellationToken);
        }


        Task<IEnumerable<HistoricalTagValues>> IReadInterpolatedTagValues.ReadInterpolatedTagValues(IDataCoreContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadInterpolatedTagValues(context, request, cancellationToken);
        }


        Task<IEnumerable<HistoricalTagValues>> IReadTagValuesAtTimes.ReadTagValuesAtTimes(IDataCoreContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadTagValuesAtTimes(context, request, cancellationToken);
        }


        Task<IEnumerable<string>> IReadProcessedTagValues.GetSupportedDataFunctions(IDataCoreContext context, CancellationToken cancellationToken) {
            return _historicalQueryHelper.GetSupportedDataFunctions(context, cancellationToken);
        }


        Task<IEnumerable<ProcessedHistoricalTagValues>> IReadProcessedTagValues.ReadProcessedTagValues(IDataCoreContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadProcessedTagValues(context, request, cancellationToken);
        }


        Task<IEnumerable<TagValueAnnotations>> IReadTagValueAnnotations.ReadTagValueAnnotations(IDataCoreContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<TagValueAnnotations>>(new TagValueAnnotations[0]);
        }


        private void LoadTagValuesFromCsv() {
            var tag1Values = new SortedList<DateTime, TagValue>();
            var tag2Values = new SortedList<DateTime, TagValue>();
            var tag3Values = new SortedList<DateTime, TagValue>();

            foreach (var row in CsvData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
                var cols = row.Split(',');
                var timestamp = DateTime.Parse(cols[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

                _utcSampleTimes.Add(timestamp);

                var tag1Value = double.Parse(cols[1], CultureInfo.InvariantCulture);
                tag1Values.Add(timestamp, TagValue.Create().WithUtcSampleTime(timestamp).WithNumericValue(tag1Value).WithStatus(TagValueStatus.Good));

                var tag2Value = double.Parse(cols[2], CultureInfo.InvariantCulture);
                tag2Values.Add(timestamp, TagValue.Create().WithUtcSampleTime(timestamp).WithNumericValue(tag2Value).WithStatus(TagValueStatus.Good));

                var tag3Value = double.Parse(cols[3], CultureInfo.InvariantCulture);
                tag3Values.Add(timestamp, TagValue.Create().WithUtcSampleTime(timestamp).WithNumericValue(tag3Value).WithStatus(TagValueStatus.Good));
            }

            _rawValues[Tag1Id] = tag1Values;
            _rawValues[Tag2Id] = tag2Values;
            _rawValues[Tag3Id] = tag3Values;

            _earliestSampleTimeUtc = _utcSampleTimes.First();
            _latestSampleTimeUtc = _utcSampleTimes.Last();
            _dataSetDuration = _latestSampleTimeUtc - _earliestSampleTimeUtc;
        }


        private IDictionary<string, TagValue> GetSnapshotValues(string[] tags) {
            // If we don't have any valid tags in the request, or if we don't have any CSV data to work with, 
            // return a null value for each valid tag.
            if (tags.Length == 0 || _dataSetDuration <= TimeSpan.Zero) {
                return tags.ToDictionary(x => x, x => (TagValue) null);
            }

            var result = new Dictionary<string, TagValue>();

            var now = DateTime.UtcNow;
            if (now >= _earliestSampleTimeUtc && now <= _latestSampleTimeUtc) {
                foreach (var tag in tags) {
                    SortedList<DateTime, TagValue> valuesForTag;
                    if (!_rawValues.TryGetValue(tag, out valuesForTag)) {
                        result[tag] = null;
                        continue;
                    }

                    // The snapshot value is the sample with a time stamp less than or equal to now.
                    var snapshot = valuesForTag.Values.LastOrDefault(x => x.UtcSampleTime <= now);
                    result[tag] = snapshot;
                }

                return result;
            }


            // Offset the CSV dates until "now" is inside the set, and then get the value at or 
            // immediately before now.

            var offset = TimeSpan.Zero;

            if (now < _earliestSampleTimeUtc) {
                // If utcStartTime is less than the earliest CSV sample time, we need to calculate a 
                // negative offset initially.
                var adjustedDataSetStartTime = _earliestSampleTimeUtc;
                while (now < adjustedDataSetStartTime) {
                    offset = offset.Subtract(_dataSetDuration);
                    adjustedDataSetStartTime = adjustedDataSetStartTime.Subtract(_dataSetDuration);
                }
            }
            else if (now > _latestSampleTimeUtc) {
                // If utcStartTime is greater than the latest CSV sample time, we need to calculate a 
                // positive offset initially.
                var adjustedDataSetEndTime = _latestSampleTimeUtc;
                while (now > adjustedDataSetEndTime) {
                    offset = offset.Add(_dataSetDuration);
                    adjustedDataSetEndTime = adjustedDataSetEndTime.Add(_dataSetDuration);
                }
            }

            foreach (var tag in tags) {
                SortedList<DateTime, TagValue> valuesForTag;
                if (!_rawValues.TryGetValue(tag, out valuesForTag)) {
                    result[tag] = null;
                    continue;
                }

                // Get the value at or immediately before now.
                var snapshot = valuesForTag.Values.LastOrDefault(x => x.UtcSampleTime.Add(offset) <= now);
                result[tag] = snapshot == null
                    ? null
                    : TagValue.CreateFromExisting(snapshot).WithUtcSampleTime(snapshot.UtcSampleTime.Add(offset));
            }

            return result;
        }


        private IDictionary<string, List<TagValue>> GetRawValues(string[] tags, DateTime utcStartTime, DateTime utcEndTime, RawDataBoundaryType boundaryType, int maxValues) {
            // If we don't have any valid tags in the request, or if we don't have any data to work with, 
            // return an empty set of values for each valid tag.
            if (tags.Length == 0) {
                return tags.ToDictionary(x => x, x => new List<TagValue>());
            }

            // If the requested time range is inside the loaded data time range, it's easy - we just 
            // get the raw values inside the requested time range for each valid tag.
            if ((utcStartTime >= _earliestSampleTimeUtc && utcEndTime <= _latestSampleTimeUtc)) {
                // If the request is outside of the CSV range, return an empty set of values for every valid tag in the request.
                if ((utcStartTime < _earliestSampleTimeUtc && utcEndTime < _earliestSampleTimeUtc) || (utcStartTime > _latestSampleTimeUtc && utcEndTime > _latestSampleTimeUtc)) {
                    return tags.ToDictionary(x => x, x => new List<TagValue>());
                }

                // For every valid tag in the request, return the raw values inside the requested time range.
                var res = new Dictionary<string, List<TagValue>>();
                foreach (var tag in tags) {
                    SortedList<DateTime, TagValue> valuesForTag;
                    if (!_rawValues.TryGetValue(tag, out valuesForTag)) {
                        res[tag] = new List<TagValue>();
                        continue;
                    }

                    var query = valuesForTag.Values.Where(x => x.UtcSampleTime >= utcStartTime && x.UtcSampleTime <= utcEndTime);

                    res[tag] = maxValues > 0
                        ? query.Take(maxValues).ToList()
                        : query.ToList();
                }

                return res;
            }

            // The time stamp offset that we have to apply to the original CSV samples in order to create 
            // raw samples between utcStartTime and utcEndTime.  We'll calculate an initial offset based 
            // on utcStartTime's position compared to the earliest or latest sample in the CSV data set, 
            // and we'll then adjust it every time we iterate over the original CSV data.
            var offset = TimeSpan.Zero;

            if (utcStartTime < _earliestSampleTimeUtc) {
                // If utcStartTime is less than the earliest CSV sample time, we need to calculate a 
                // negative offset initially.
                var adjustedDataSetStartTime = _earliestSampleTimeUtc;
                while (utcStartTime < adjustedDataSetStartTime) {
                    offset = offset.Subtract(_dataSetDuration);
                    adjustedDataSetStartTime = adjustedDataSetStartTime.Subtract(_dataSetDuration);
                }
            }
            else if (utcStartTime > _latestSampleTimeUtc) {
                // If utcStartTime is greater than the latest CSV sample time, we need to calculate a 
                // positive offset initially.
                var adjustedDataSetEndTime = _latestSampleTimeUtc;
                while (utcStartTime > adjustedDataSetEndTime) {
                    offset = offset.Add(_dataSetDuration);
                    adjustedDataSetEndTime = adjustedDataSetEndTime.Add(_dataSetDuration);
                }
            }

            // Now that we've calculated our initial offset, we need to find the index of the sample time 
            // in the CSV data that, when shifted by our offset, is greater than or equal to utcStartTime.
            // This will be ur starting point as we iterate over the CSV data.
            var startingIndex = 0;
            for (; startingIndex < _utcSampleTimes.Count; startingIndex++) {
                var tmp = _utcSampleTimes[startingIndex].Add(offset);
                if (tmp >= utcStartTime) {
                    // If we are using an outside boundary, and our starting index would be greater than 
                    // the start time for the query, move back by one sample so that the first value will 
                    // be before the request start time..
                    if (boundaryType == RawDataBoundaryType.Outside && tmp > utcStartTime && startingIndex > 0) {
                        startingIndex--;
                    }
                    break;
                }
            }

            var result = new Dictionary<string, List<TagValue>>();

            // We'll set this to false when we don't need to iterate over the CSV data any more.
            var @continue = true;
            var iterations = 0;
            var continueOnce = boundaryType == RawDataBoundaryType.Outside;
            do {
                // Starting at startingIndex, we'll iterate over the sample times in the CSV data.
                for (var i = startingIndex; i < _utcSampleTimes.Count; i++) {
                    if (maxValues > 0) {
                        ++iterations;
                        if (iterations > maxValues) {
                            continueOnce = false;
                            @continue = false;
                            break;
                        }
                    }

                    // Get the unmodified CSV sample time.
                    var unmodifiedSampleTime = _utcSampleTimes[i];
                    // Apply our current offset to the sample time.
                    var sampleTimeThisIteration = offset.Equals(TimeSpan.Zero)
                        ? unmodifiedSampleTime
                        : unmodifiedSampleTime.Add(offset);

                    if (sampleTimeThisIteration == utcEndTime) {
                        // We've hit our end time exactly; no need to include a value after the end boundary.
                        continueOnce = false;
                    }

                    // If we have gone past utcEndTime, we can break out of this loop, and out of the 
                    // do..while loop.
                    if (sampleTimeThisIteration > utcEndTime) {
                        // If we will only return values inside the query time range, or we exactly hit 
                        // our end time in the previous iteration, we will break from the loop now. 

                        if (continueOnce) {
                            continueOnce = false;
                        }
                        else {
                            @continue = false;
                            break;
                        }
                    }

                    // For each valid tag in the request, we'll check to see if that tag has a sample at 
                    // the unmodified CSV sample time for the current iteration.  If it does, we'll 
                    // create a new DataCoreTagValue (or re-use the CSV value, if the sample time for the 
                    // current iteration is inside the original CSV date range) and add it to the raw 
                    // data for the tag.
                    foreach (var tag in tags) {
                        SortedList<DateTime, TagValue> csvValuesForTag;
                        // If there are no raw values for the current tag, or if we have already exceeded 
                        // the maximum number of raw samples we are allowed to use in a query, move to the 
                        // next tag.
                        if (!_rawValues.TryGetValue(tag, out csvValuesForTag)) {
                            continue;
                        }

                        TagValue unmodifiedSample;
                        if (!csvValuesForTag.TryGetValue(unmodifiedSampleTime, out unmodifiedSample)) {
                            continue;
                        }

                        List<TagValue> resultValuesForTag;
                        if (!result.TryGetValue(tag, out resultValuesForTag)) {
                            resultValuesForTag = new List<TagValue>();
                            result[tag] = resultValuesForTag;
                        }

                        // If the time stamp offset is currently zero, we'll just use the original CSV 
                        // sample, to prevent us from creating unnecessary instances of DataCoreTagValue.
                        var sample = offset.Equals(TimeSpan.Zero)
                            ? unmodifiedSample
                            : TagValue.CreateFromExisting(unmodifiedSample).WithUtcSampleTime(sampleTimeThisIteration);
                        resultValuesForTag.Add(sample);
                    }
                }

                if (@continue) {
                    // We've now iterated over the CSV data, but we still need more raw data before we 
                    // can stop.  We'll shift the offset forward by one iteration, and move back to the 
                    // first sample in the CSV data.
                    offset = offset.Add(_dataSetDuration);
                    startingIndex = 0;
                }
            }
            while (@continue);

            return result.ToDictionary(x => x.Key, x => x.Value);
        }


        private class AdapterFeatures : IAdapterFeaturesCollection {

            private readonly IDictionary<Type, object> _features;


            public IEnumerable<Type> Keys {
                get { return _features.Keys; }
            }


            private object this[Type key] {
                get {
                    return key == null || !_features.TryGetValue(key, out var value)
                        ? null
                        : value;
                }
            }

            public AdapterFeatures(ExampleDataSource dataSource) {
                _features = new Dictionary<Type, object>() {
                    { typeof(IReadInterpolatedTagValues), dataSource },
                    { typeof(IReadPlotTagValues), dataSource },
                    { typeof(IReadProcessedTagValues), dataSource },
                    { typeof(IReadRawTagValues), dataSource },
                    { typeof(IReadSnapshotTagValues), dataSource },
                    { typeof(IReadTagValueAnnotations), dataSource },
                    { typeof(IReadTagValuesAtTimes), dataSource },
                    { typeof(ITagSearch), dataSource },
                };
            }


            public TFeature Get<TFeature>() where TFeature: IAdapterFeature {
                var type = typeof(TFeature);
                return (TFeature) this[type];
            }
        }


        private const string CsvData = @"
2019-03-21T12:49:10Z,55.65,54.81,45.01
2019-03-21T12:49:40Z,53.46,55.40,46.89
2019-03-21T12:50:10Z,54.79,56.44,46.03
2019-03-21T12:50:40Z,56.43,55.72,46.99
2019-03-21T12:51:10Z,55.73,56.36,46.57
2019-03-21T12:51:40Z,57.31,54.69,48.20
2019-03-21T12:52:10Z,57.87,54.86,46.98
2019-03-21T12:52:40Z,55.32,56.22,45.59
2019-03-21T12:53:10Z,53.02,55.20,46.48
2019-03-21T12:53:40Z,55.04,55.75,47.11
2019-03-21T12:54:10Z,55.60,54.57,48.87
2019-03-21T12:54:40Z,55.77,54.75,47.72
2019-03-21T12:55:10Z,55.40,55.18,47.03
2019-03-21T12:55:40Z,54.68,57.05,47.56
2019-03-21T12:56:10Z,54.83,55.45,46.16
2019-03-21T12:56:40Z,55.16,57.02,46.48
2019-03-21T12:57:10Z,55.80,57.14,47.56
2019-03-21T12:57:40Z,58.26,57.44,48.65
2019-03-21T12:58:10Z,60.79,56.55,48.52
2019-03-21T12:58:40Z,59.66,55.82,46.78
2019-03-21T12:59:10Z,62.11,54.58,47.90
2019-03-21T12:59:40Z,61.04,56.42,46.20
2019-03-21T13:00:10Z,62.42,57.09,44.88
2019-03-21T13:00:40Z,61.16,56.50,45.14
2019-03-21T13:01:10Z,62.83,56.32,46.83
2019-03-21T13:01:40Z,62.15,54.63,47.44
2019-03-21T13:02:10Z,59.62,52.77,45.97
2019-03-21T13:02:40Z,59.46,52.96,47.06
2019-03-21T13:03:10Z,61.65,54.49,45.58
2019-03-21T13:03:40Z,64.19,53.30,46.61
2019-03-21T13:04:10Z,62.77,51.66,47.59
2019-03-21T13:04:40Z,64.10,52.71,46.07
2019-03-21T13:05:10Z,64.96,51.46,47.04
2019-03-21T13:05:40Z,64.70,49.83,2067.19
2019-03-21T13:06:10Z,65.87,49.60,49.77
2019-03-21T13:06:40Z,65.83,48.01,51.58
2019-03-21T13:07:10Z,68.00,46.41,52.15
2019-03-21T13:07:40Z,66.93,48.17,50.86
2019-03-21T13:08:10Z,64.89,49.16,51.38
2019-03-21T13:08:40Z,64.41,50.27,49.94
2019-03-21T13:09:10Z,64.15,51.36,50.40
2019-03-21T13:09:40Z,65.84,53.24,50.28
2019-03-21T13:10:10Z,65.93,52.26,50.70
2019-03-21T13:10:40Z,63.96,52.72,50.36
2019-03-21T13:11:10Z,66.07,52.63,49.68
2019-03-21T13:11:40Z,64.05,52.38,50.39
2019-03-21T13:12:10Z,63.83,50.90,51.74
2019-03-21T13:12:40Z,65.97,51.68,53.16
2019-03-21T13:13:10Z,67.45,50.64,54.00
2019-03-21T13:13:40Z,67.59,48.74,52.74
2019-03-21T13:14:10Z,69.34,50.07,54.52
2019-03-21T13:14:40Z,68.05,49.08,52.94
2019-03-21T13:15:10Z,69.08,48.94,54.42
2019-03-21T13:15:40Z,69.68,49.80,55.53
2019-03-21T13:16:10Z,69.57,50.25,57.01
2019-03-21T13:16:40Z,68.18,50.21,55.64
2019-03-21T13:17:10Z,69.63,52.05,57.52
2019-03-21T13:17:40Z,71.22,53.29,57.32
2019-03-21T13:18:10Z,71.41,54.77,55.66
2019-03-21T13:18:40Z,69.97,55.69,2534.21
2019-03-21T13:19:10Z,68.78,57.53,57.18
2019-03-21T13:19:40Z,69.81,59.30,56.86
2019-03-21T13:20:10Z,68.20,58.70,57.94
2019-03-21T13:20:40Z,68.18,58.40,59.59
2019-03-21T13:21:10Z,70.59,58.44,58.59
2019-03-21T13:21:40Z,69.28,57.88,57.85
2019-03-21T13:22:10Z,68.96,58.38,58.66
2019-03-21T13:22:40Z,67.59,57.07,57.41
2019-03-21T13:23:10Z,68.01,57.42,58.08
2019-03-21T13:23:40Z,68.45,57.92,59.36
2019-03-21T13:24:10Z,66.58,58.12,59.59
2019-03-21T13:24:40Z,64.18,58.20,58.59
2019-03-21T13:25:10Z,64.88,57.53,58.55
2019-03-21T13:25:40Z,64.41,2595.47,57.08
2019-03-21T13:26:10Z,66.56,56.54,57.85
2019-03-21T13:26:40Z,65.97,55.14,57.93
2019-03-21T13:27:10Z,66.30,55.82,56.74
2019-03-21T13:27:40Z,68.22,56.29,57.79
2019-03-21T13:28:10Z,65.92,57.36,58.03
2019-03-21T13:28:40Z,65.50,59.09,58.99
2019-03-21T13:29:10Z,67.46,60.36,58.82
2019-03-21T13:29:40Z,68.52,61.05,58.07
2019-03-21T13:30:10Z,66.17,60.54,57.64
2019-03-21T13:30:40Z,65.75,60.18,57.97
2019-03-21T13:31:10Z,65.29,61.63,56.83
2019-03-21T13:31:40Z,65.95,60.69,56.07
2019-03-21T13:32:10Z,65.68,59.39,54.89
2019-03-21T13:32:40Z,67.66,58.93,53.27
2019-03-21T13:33:10Z,68.92,58.68,54.18
2019-03-21T13:33:40Z,69.51,57.02,53.18
2019-03-21T13:34:10Z,67.12,58.37,54.12
2019-03-21T13:34:40Z,2183.79,60.02,53.48
2019-03-21T13:35:10Z,65.67,61.42,52.83
2019-03-21T13:35:40Z,64.44,61.25,52.61
2019-03-21T13:36:10Z,64.73,60.23,52.68
2019-03-21T13:36:40Z,64.85,60.20,54.06
2019-03-21T13:37:10Z,63.73,59.60,2439.61
2019-03-21T13:37:40Z,63.86,58.66,55.73
2019-03-21T13:38:10Z,62.81,58.66,54.73
2019-03-21T13:38:40Z,62.21,57.63,54.69
2019-03-21T13:39:10Z,63.26,57.13,56.42
2019-03-21T13:39:40Z,61.61,57.69,54.96
2019-03-21T13:40:10Z,59.66,57.67,53.60
2019-03-21T13:40:40Z,61.10,57.44,53.57
2019-03-21T13:41:10Z,59.28,56.10,55.21
2019-03-21T13:41:40Z,60.35,56.01,55.00
2019-03-21T13:42:10Z,59.81,55.40,54.86
2019-03-21T13:42:40Z,59.04,54.40,53.18
2019-03-21T13:43:10Z,56.86,52.92,53.84
2019-03-21T13:43:40Z,55.55,2496.67,52.22
2019-03-21T13:44:10Z,56.61,53.09,52.68
2019-03-21T13:44:40Z,54.96,54.58,54.36
2019-03-21T13:45:10Z,52.73,54.40,54.20
2019-03-21T13:45:40Z,54.62,56.03,53.30
2019-03-21T13:46:10Z,56.04,54.89,54.36
2019-03-21T13:46:40Z,54.90,54.88,54.42
2019-03-21T13:47:10Z,53.57,54.21,52.65
2019-03-21T13:47:40Z,51.44,53.87,52.81
2019-03-21T13:48:10Z,49.50,55.14,51.19
2019-03-21T13:48:40Z,50.65,53.69,51.41
2019-03-21T13:49:10Z,50.09,54.35,50.94";

    }

}
