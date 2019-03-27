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

    /// <summary>
    /// Example adapter that has data source capabilities (tag search, tag value queries, etc). The 
    /// adapter contains a set of sensor-like data for 3 tags that it will loop over.
    /// </summary>
    public class ExampleDataSource: BackgroundService, IAdapter, ITagSearch, IReadSnapshotTagValues, IReadInterpolatedTagValues, IReadPlotTagValues, IReadProcessedTagValues, IReadRawTagValues, IReadTagValuesAtTimes, IReadTagValueAnnotations {

        /// <summary>
        /// Background task that will be returned when <see cref="ExecuteAsync(CancellationToken)"/>.
        /// </summary>
        private Task _bgTask;

        /// <summary>
        /// The descriptor for the adapter.
        /// </summary>
        private readonly AdapterDescriptor _descriptor = new AdapterDescriptor(
            "example",
            "Example Adapter",
            "An example data source adapter"
        );

        /// <summary>
        /// Resolves adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection();

        /// <summary>
        /// ID for tag 1.
        /// </summary>
        private const string Tag1Id = "3D3CB0C7-3578-46E8-B4C1-5BFBA563BF48";

        /// <summary>
        /// ID for tag 2.
        /// </summary>
        private const string Tag2Id = "7C16DA6A-1802-42E4-8473-B2FB504968EE";

        /// <summary>
        /// ID for tag 3.
        /// </summary>
        private const string Tag3Id = "67360992-B195-4DDD-8947-4D7C09737966";

        /// <summary>
        /// Tag definitions.
        /// </summary>
        private readonly TagDefinition[] _tags = {
            new TagDefinition(Tag1Id, "Tag1", "This is an example tag", null, TagDataType.Numeric, null, null),
            new TagDefinition(Tag2Id, "Tag2", "This is an example tag with units specified", "deg C", TagDataType.Numeric, null, null),
            new TagDefinition(Tag3Id, "Tag3", "This is an example tag with units and bespoke properties", null, TagDataType.Numeric, null, new Dictionary<string, string>() {
                { "utcCreatedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
            }),
        };

        /// <summary>
        /// Raw data for each tag, indexed by tag ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, SortedList<DateTime, TagValue>> _rawValues = new ConcurrentDictionary<string, SortedList<DateTime, TagValue>>();

        /// <summary>
        /// The UTC sample times in the CSV data that we load in.
        /// </summary>
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

        /// <summary>
        /// Handles non-raw historical queries for us.
        /// </summary>
        private readonly ReadHistoricalTagValuesHelper _historicalQueryHelper;

        /// <summary>
        /// Maximum number of raw samples to return per tag per query.
        /// </summary>
        private const int MaxRawSamplesPerTag = 20000;


        /// <inheritdoc/>
        AdapterDescriptor IAdapter.Descriptor {
            get { return _descriptor; }
        }


        /// <inheritdoc/>
        IAdapterFeaturesCollection IAdapter.Features {
            get { return _features; }
        }


        /// <summary>
        /// Creates a new <see cref="ExampleDataSource"/> object.
        /// </summary>
        public ExampleDataSource() {
            // Register features!
            _features.Add<IReadInterpolatedTagValues>(this);
            _features.Add<IReadPlotTagValues>(this);
            _features.Add<IReadProcessedTagValues>(this);
            _features.Add<IReadRawTagValues>(this);
            _features.Add<IReadSnapshotTagValues>(this);
            _features.Add<IReadTagValueAnnotations>(this);
            _features.Add<IReadTagValuesAtTimes>(this);
            _features.Add<ITagSearch>(this);

            _historicalQueryHelper = new ReadHistoricalTagValuesHelper(this, this);
            LoadTagValuesFromCsv();
        }


        /// <summary>
        /// Starts the long-running <see cref="BackgroundService"/> task.
        /// </summary>
        /// <param name="stoppingToken">
        ///   A cancellation token that will fire when the background service is stopping.
        /// </param>
        /// <returns>
        ///   The long-running task for the adapter.
        /// </returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            lock (this) {
                if (_bgTask == null) {
                    _bgTask = Task.Delay(-1, stoppingToken);
                }
            }

            return _bgTask;
        }


        /// <inheritdoc/>
        Task<IEnumerable<TagDefinition>> ITagSearch.FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            var result = _tags.ApplyFilter(request).ToArray();
            return Task.FromResult<IEnumerable<TagDefinition>>(result);
        }


        /// <inheritdoc/>
        Task<IEnumerable<TagDefinition>> ITagSearch.GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            var result = request
                .Tags
                .Select(nameOrId => _tags.FirstOrDefault(t => t.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)))
                .Where(tag => tag != null)
                .ToArray();

            return Task.FromResult<IEnumerable<TagDefinition>>(result);
        }


        /// <inheritdoc/>
        Task<IEnumerable<SnapshotTagValue>> IReadSnapshotTagValues.ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var tags = request.Tags.Select(x => _tags.FirstOrDefault(t => t.Id.Equals(x, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(x, StringComparison.OrdinalIgnoreCase))).Where(x => x != null).ToArray();
            var values = GetSnapshotValues(tags.Select(t => t.Id).ToArray());
            var result = tags.Select(x => new SnapshotTagValue(x.Id, x.Name, values.TryGetValue(x.Id, out var val) ? val : null)).ToArray();
            return Task.FromResult<IEnumerable<SnapshotTagValue>>(result);
        }


        /// <inheritdoc/>
        Task<IEnumerable<HistoricalTagValues>> IReadRawTagValues.ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var tags = request.Tags.Select(x => _tags.FirstOrDefault(t => t.Id.Equals(x, StringComparison.OrdinalIgnoreCase) || t.Name.Equals(x, StringComparison.OrdinalIgnoreCase))).Where(x => x != null).ToArray();
            var values = GetRawValues(tags.Select(t => t.Id).ToArray(), request.UtcStartTime, request.UtcEndTime, request.BoundaryType, request.SampleCount);
            var result = tags.Select(x => new HistoricalTagValues(x.Id, x.Name, values.TryGetValue(x.Id, out var val) ? val : null)).ToArray();
            return Task.FromResult<IEnumerable<HistoricalTagValues>>(result);
        }


        /// <inheritdoc/>
        Task<IEnumerable<HistoricalTagValues>> IReadPlotTagValues.ReadPlotTagValues(IAdapterCallContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadPlotTagValues(context, request, cancellationToken);
        }


        /// <inheritdoc/>
        Task<IEnumerable<HistoricalTagValues>> IReadInterpolatedTagValues.ReadInterpolatedTagValues(IAdapterCallContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadInterpolatedTagValues(context, request, cancellationToken);
        }


        /// <inheritdoc/>
        Task<IEnumerable<HistoricalTagValues>> IReadTagValuesAtTimes.ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadTagValuesAtTimes(context, request, cancellationToken);
        }


        /// <inheritdoc/>
        Task<IEnumerable<DataFunctionDescriptor>> IReadProcessedTagValues.GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            return _historicalQueryHelper.GetSupportedDataFunctions(context, cancellationToken);
        }


        /// <inheritdoc/>
        Task<IEnumerable<ProcessedHistoricalTagValues>> IReadProcessedTagValues.ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            return _historicalQueryHelper.ReadProcessedTagValues(context, request, cancellationToken);
        }


        /// <inheritdoc/>
        Task<IEnumerable<TagValueAnnotations>> IReadTagValueAnnotations.ReadTagValueAnnotations(IAdapterCallContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            return Task.FromResult<IEnumerable<TagValueAnnotations>>(new TagValueAnnotations[0]);
        }


        /// <summary>
        /// Loads the looping data set in from CSV.
        /// </summary>
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

            // We store various properties about the CSV data to improve data query performance.
            _earliestSampleTimeUtc = _utcSampleTimes.First();
            _latestSampleTimeUtc = _utcSampleTimes.Last();
            _dataSetDuration = _latestSampleTimeUtc - _earliestSampleTimeUtc;
        }


        /// <summary>
        /// Gets the snapshot values for the specified tags.
        /// </summary>
        /// <param name="tags">
        ///   The IDs of the tags to query.
        /// </param>
        /// <returns>
        ///   The snapshot tag values, indexed by tag ID.
        /// </returns>
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


        /// <summary>
        /// Gets raw historical data for the specified tags.
        /// </summary>
        /// <param name="tags">
        ///   The IDs of the tags to query.
        /// </param>
        /// <param name="utcStartTime">
        ///   The query start time.
        /// </param>
        /// <param name="utcEndTime">
        ///   The query end time.
        /// </param>
        /// <param name="boundaryType">
        ///   The raw data boundary time.
        /// </param>
        /// <param name="maxValues">
        ///   The maximum number of values to retrieve.
        /// </param>
        /// <returns>
        ///   The raw historical values, indexed by tag ID.
        /// </returns>
        private IDictionary<string, List<TagValue>> GetRawValues(string[] tags, DateTime utcStartTime, DateTime utcEndTime, RawDataBoundaryType boundaryType, int maxValues) {
            // If we don't have any valid tags in the request, or if we don't have any data to work with, 
            // return an empty set of values for each valid tag.
            if (tags.Length == 0) {
                return tags.ToDictionary(x => x, x => new List<TagValue>());
            }

            if (maxValues < 1 || maxValues > MaxRawSamplesPerTag) {
                maxValues = MaxRawSamplesPerTag;
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

                    res[tag] = query.Take(maxValues).ToList();
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
                    ++iterations;
                    if (iterations > maxValues) {
                        continueOnce = false;
                        @continue = false;
                        break;
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


        /// <summary>
        /// CSV data that we parse at startup.
        /// </summary>
        private const string CsvData = @"
2019-03-27T09:50:12Z,55.65,54.81,45.01
2019-03-27T09:50:27Z,53.46,55.40,46.89
2019-03-27T09:50:42Z,54.79,56.44,46.03
2019-03-27T09:50:57Z,56.43,55.72,46.99
2019-03-27T09:51:12Z,55.73,56.36,46.57
2019-03-27T09:51:27Z,57.31,54.69,48.20
2019-03-27T09:51:42Z,57.87,54.86,46.98
2019-03-27T09:51:57Z,55.32,56.22,45.59
2019-03-27T09:52:12Z,53.02,55.20,46.48
2019-03-27T09:52:27Z,55.04,55.75,47.11
2019-03-27T09:52:42Z,55.60,54.57,48.87
2019-03-27T09:52:57Z,55.77,54.75,47.72
2019-03-27T09:53:12Z,55.40,55.18,47.03
2019-03-27T09:53:27Z,54.68,57.05,47.56
2019-03-27T09:53:42Z,54.83,55.45,46.16
2019-03-27T09:53:57Z,55.16,57.02,46.48
2019-03-27T09:54:12Z,55.80,57.14,47.56
2019-03-27T09:54:27Z,58.26,57.44,48.65
2019-03-27T09:54:42Z,60.79,56.55,48.52
2019-03-27T09:54:57Z,59.66,55.82,46.78
2019-03-27T09:55:12Z,62.11,54.58,47.90
2019-03-27T09:55:27Z,61.04,56.42,46.20
2019-03-27T09:55:42Z,62.42,57.09,44.88
2019-03-27T09:55:57Z,61.16,56.50,45.14
2019-03-27T09:56:12Z,62.83,56.32,46.83
2019-03-27T09:56:27Z,62.15,54.63,47.44
2019-03-27T09:56:42Z,59.62,52.77,45.97
2019-03-27T09:56:57Z,59.46,52.96,47.06
2019-03-27T09:57:12Z,61.65,54.49,45.58
2019-03-27T09:57:27Z,64.19,53.30,46.61
2019-03-27T09:57:42Z,62.77,51.66,47.59
2019-03-27T09:57:57Z,64.10,52.71,46.07
2019-03-27T09:58:12Z,64.96,51.46,47.04
2019-03-27T09:58:27Z,64.70,49.83,48.26
2019-03-27T09:58:42Z,65.87,49.60,49.77
2019-03-27T09:58:57Z,65.83,48.01,51.58
2019-03-27T09:59:12Z,68.00,46.41,52.15
2019-03-27T09:59:27Z,66.93,48.17,50.86
2019-03-27T09:59:42Z,64.89,49.16,51.38
2019-03-27T09:59:57Z,64.41,50.27,49.94
2019-03-27T10:00:12Z,64.15,51.36,50.40
2019-03-27T10:00:27Z,65.84,53.24,50.28
2019-03-27T10:00:42Z,65.93,52.26,50.70
2019-03-27T10:00:57Z,63.96,52.72,50.36
2019-03-27T10:01:12Z,66.07,52.63,49.68
2019-03-27T10:01:27Z,64.05,52.38,50.39
2019-03-27T10:01:42Z,63.83,50.90,51.74
2019-03-27T10:01:57Z,65.97,51.68,53.16
2019-03-27T10:02:12Z,67.45,50.64,54.00
2019-03-27T10:02:27Z,67.59,48.74,52.74
2019-03-27T10:02:42Z,69.34,50.07,54.52
2019-03-27T10:02:57Z,68.05,49.08,52.94
2019-03-27T10:03:12Z,69.08,48.94,54.42
2019-03-27T10:03:27Z,69.68,49.80,55.53
2019-03-27T10:03:42Z,69.57,50.25,57.01
2019-03-27T10:03:57Z,68.18,50.21,55.64
2019-03-27T10:04:12Z,69.63,52.05,57.52
2019-03-27T10:04:27Z,71.22,53.29,57.32
2019-03-27T10:04:42Z,71.41,54.77,55.66
2019-03-27T10:04:57Z,69.97,55.69,55.35
2019-03-27T10:05:12Z,68.78,57.53,57.18
2019-03-27T10:05:27Z,69.81,59.30,56.86
2019-03-27T10:05:42Z,68.20,58.70,57.94
2019-03-27T10:05:57Z,68.18,58.40,59.59
2019-03-27T10:06:12Z,70.59,58.44,58.59
2019-03-27T10:06:27Z,69.28,57.88,57.85
2019-03-27T10:06:42Z,68.96,58.38,58.66
2019-03-27T10:06:57Z,67.59,57.07,57.41
2019-03-27T10:07:12Z,68.01,57.42,58.08
2019-03-27T10:07:27Z,68.45,57.92,59.36
2019-03-27T10:07:42Z,66.58,58.12,59.59
2019-03-27T10:07:57Z,64.18,58.20,58.59
2019-03-27T10:08:12Z,64.88,57.53,58.55
2019-03-27T10:08:27Z,64.41,56.71,57.08
2019-03-27T10:08:42Z,66.56,56.54,57.85
2019-03-27T10:08:57Z,65.97,55.14,57.93
2019-03-27T10:09:12Z,66.30,55.82,56.74
2019-03-27T10:09:27Z,68.22,56.29,57.79
2019-03-27T10:09:42Z,65.92,57.36,58.03
2019-03-27T10:09:57Z,65.50,59.09,58.99
2019-03-27T10:10:12Z,67.46,60.36,58.82
2019-03-27T10:10:27Z,68.52,61.05,58.07
2019-03-27T10:10:42Z,66.17,60.54,57.64
2019-03-27T10:10:57Z,65.75,60.18,57.97
2019-03-27T10:11:12Z,65.29,61.63,56.83
2019-03-27T10:11:27Z,65.95,60.69,56.07
2019-03-27T10:11:42Z,65.68,59.39,54.89
2019-03-27T10:11:57Z,67.66,58.93,53.27
2019-03-27T10:12:12Z,68.92,58.68,54.18
2019-03-27T10:12:27Z,69.51,57.02,53.18
2019-03-27T10:12:42Z,67.12,58.37,54.12
2019-03-27T10:12:57Z,65.67,60.02,53.48
2019-03-27T10:13:12Z,65.67,61.42,52.83
2019-03-27T10:13:27Z,64.44,61.25,52.61
2019-03-27T10:13:42Z,64.73,60.23,52.68
2019-03-27T10:13:57Z,64.85,60.20,54.06
2019-03-27T10:14:12Z,63.73,59.60,54.61
2019-03-27T10:14:27Z,63.86,58.66,55.73
2019-03-27T10:14:42Z,62.81,58.66,54.73
2019-03-27T10:14:57Z,62.21,57.63,54.69
2019-03-27T10:15:12Z,63.26,57.13,56.42
2019-03-27T10:15:27Z,61.61,57.69,54.96
2019-03-27T10:15:42Z,59.66,57.67,53.60
2019-03-27T10:15:57Z,61.10,57.44,53.57
2019-03-27T10:16:12Z,59.28,56.10,55.21
2019-03-27T10:16:27Z,60.35,56.01,55.00
2019-03-27T10:16:42Z,59.81,55.40,54.86
2019-03-27T10:16:57Z,59.04,54.40,53.18
2019-03-27T10:17:12Z,56.86,52.92,53.84
2019-03-27T10:17:27Z,55.55,53.06,52.22
2019-03-27T10:17:42Z,56.61,53.09,52.68
2019-03-27T10:17:57Z,54.96,54.58,54.36
2019-03-27T10:18:12Z,52.73,54.40,54.20
2019-03-27T10:18:27Z,54.62,56.03,53.30
2019-03-27T10:18:42Z,56.04,54.89,54.36
2019-03-27T10:18:57Z,54.90,54.88,54.42
2019-03-27T10:19:12Z,53.57,54.21,52.65
2019-03-27T10:19:27Z,51.44,53.87,52.81
2019-03-27T10:19:42Z,49.50,55.14,51.19
2019-03-27T10:19:57Z,50.65,53.69,51.41
2019-03-27T10:20:12Z,50.09,54.35,50.94
2019-03-27T10:20:27Z,50.54,53.07,51.56
2019-03-27T10:20:42Z,51.75,53.35,51.08
2019-03-27T10:20:57Z,52.49,54.34,50.05
2019-03-27T10:21:12Z,51.17,55.95,50.75
2019-03-27T10:21:27Z,50.14,56.28,52.64
2019-03-27T10:21:42Z,49.20,57.42,52.10
2019-03-27T10:21:57Z,51.60,57.32,50.69
2019-03-27T10:22:12Z,51.86,55.51,52.47
2019-03-27T10:22:27Z,54.25,56.27,51.96
2019-03-27T10:22:42Z,55.30,55.50,51.63
2019-03-27T10:22:57Z,54.89,56.95,52.97
2019-03-27T10:23:12Z,53.19,55.28,52.02
2019-03-27T10:23:27Z,53.23,53.81,52.53
2019-03-27T10:23:42Z,54.58,51.98,51.01
2019-03-27T10:23:57Z,54.45,53.87,51.81
2019-03-27T10:24:12Z,55.72,53.73,50.64
2019-03-27T10:24:27Z,58.00,55.02,49.23
2019-03-27T10:24:42Z,55.62,56.79,50.66
2019-03-27T10:24:57Z,55.20,57.72,50.27
2019-03-27T10:25:12Z,56.81,59.36,51.17
2019-03-27T10:25:27Z,54.44,60.44,52.01
2019-03-27T10:25:42Z,54.77,59.76,51.94
2019-03-27T10:25:57Z,54.70,59.07,52.54
2019-03-27T10:26:12Z,55.69,59.05,51.58
2019-03-27T10:26:27Z,54.45,59.69,52.16
2019-03-27T10:26:42Z,53.60,59.01,53.08
2019-03-27T10:26:57Z,54.49,60.85,51.56
2019-03-27T10:27:12Z,53.14,60.30,52.26
2019-03-27T10:27:27Z,54.08,59.98,51.29
2019-03-27T10:27:42Z,55.56,59.11,50.03
2019-03-27T10:27:57Z,55.10,57.92,49.18
2019-03-27T10:28:12Z,55.35,56.72,49.11
2019-03-27T10:28:27Z,55.75,57.65,48.53
2019-03-27T10:28:42Z,55.32,57.84,49.00
2019-03-27T10:28:57Z,54.00,57.20,48.14
2019-03-27T10:29:12Z,55.06,55.37,47.65
2019-03-27T10:29:27Z,56.94,55.25,45.76
2019-03-27T10:29:42Z,57.83,53.89,46.24
2019-03-27T10:29:57Z,56.32,53.45,45.60
2019-03-27T10:30:12Z,53.88,53.80,44.22
2019-03-27T10:30:27Z,51.44,52.22,43.12
2019-03-27T10:30:42Z,50.14,52.70,43.21
2019-03-27T10:30:57Z,51.45,52.74,43.11
2019-03-27T10:31:12Z,51.31,52.14,43.69
2019-03-27T10:31:27Z,48.93,52.11,44.87
2019-03-27T10:31:42Z,47.13,50.72,44.06
2019-03-27T10:31:57Z,45.00,52.61,43.37
2019-03-27T10:32:12Z,45.67,53.91,41.75
2019-03-27T10:32:27Z,46.18,55.28,42.21
2019-03-27T10:32:42Z,44.69,53.44,43.86
2019-03-27T10:32:57Z,43.87,53.79,41.98
2019-03-27T10:33:12Z,45.11,53.94,41.45
2019-03-27T10:33:27Z,45.46,55.53,39.61
2019-03-27T10:33:42Z,43.15,53.80,39.50
2019-03-27T10:33:57Z,40.81,55.21,39.01
2019-03-27T10:34:12Z,39.34,55.20,39.15
2019-03-27T10:34:27Z,37.27,54.16,40.89
2019-03-27T10:34:42Z,36.44,53.64,39.27
2019-03-27T10:34:57Z,34.23,52.67,39.47
2019-03-27T10:35:12Z,32.29,53.90,39.23
2019-03-27T10:35:27Z,29.77,55.74,41.06
2019-03-27T10:35:42Z,29.47,55.56,40.20
2019-03-27T10:35:57Z,27.00,55.91,41.33
2019-03-27T10:36:12Z,25.81,56.15,41.90
2019-03-27T10:36:27Z,26.87,57.54,43.44
2019-03-27T10:36:42Z,24.86,58.06,44.84
2019-03-27T10:36:57Z,25.24,59.89,45.72
2019-03-27T10:37:12Z,24.08,58.54,45.06
2019-03-27T10:37:27Z,22.09,59.02,43.66
2019-03-27T10:37:42Z,20.98,58.84,45.25
2019-03-27T10:37:57Z,20.86,60.52,44.49
2019-03-27T10:38:12Z,23.24,61.38,43.87
2019-03-27T10:38:27Z,23.69,60.47,44.24
2019-03-27T10:38:42Z,21.74,60.79,44.67
2019-03-27T10:38:57Z,23.75,62.14,43.80
2019-03-27T10:39:12Z,25.31,61.53,43.67
2019-03-27T10:39:27Z,23.71,59.73,42.27
2019-03-27T10:39:42Z,24.71,59.31,43.35
2019-03-27T10:39:57Z,22.30,58.52,41.75
2019-03-27T10:40:12Z,22.20,58.84,41.00
2019-03-27T10:40:27Z,23.15,57.45,40.95
2019-03-27T10:40:42Z,21.80,58.66,40.24
2019-03-27T10:40:57Z,21.29,60.16,39.89
2019-03-27T10:41:12Z,20.29,62.04,41.58
2019-03-27T10:41:27Z,22.46,60.46,42.33
2019-03-27T10:41:42Z,21.70,61.16,42.34
2019-03-27T10:41:57Z,21.22,61.35,42.30
2019-03-27T10:42:12Z,22.14,61.58,43.31
2019-03-27T10:42:27Z,23.98,62.70,45.05
2019-03-27T10:42:42Z,22.69,61.40,45.51
2019-03-27T10:42:57Z,25.14,61.78,45.73
2019-03-27T10:43:12Z,26.72,62.03,45.59
2019-03-27T10:43:27Z,28.16,60.25,47.44
2019-03-27T10:43:42Z,29.18,62.04,45.66
2019-03-27T10:43:57Z,29.79,62.15,45.62
2019-03-27T10:44:12Z,31.46,61.84,47.12
2019-03-27T10:44:27Z,30.35,61.86,46.82
2019-03-27T10:44:42Z,29.79,60.72,45.86
2019-03-27T10:44:57Z,30.50,59.18,47.65
2019-03-27T10:45:12Z,29.45,57.56,45.86
2019-03-27T10:45:27Z,29.95,57.08,45.89
2019-03-27T10:45:42Z,31.03,55.33,47.66
2019-03-27T10:45:57Z,28.53,54.03,47.97
2019-03-27T10:46:12Z,27.78,54.20,49.34
2019-03-27T10:46:27Z,28.99,54.89,48.84
2019-03-27T10:46:42Z,26.58,55.43,48.89
2019-03-27T10:46:57Z,27.68,54.71,48.35
2019-03-27T10:47:12Z,27.45,53.77,48.03
2019-03-27T10:47:27Z,26.02,55.66,49.27
2019-03-27T10:47:42Z,23.74,53.95,50.80
2019-03-27T10:47:57Z,24.88,54.83,49.53
2019-03-27T10:48:12Z,22.69,54.41,49.27
2019-03-27T10:48:27Z,22.56,53.25,50.94
2019-03-27T10:48:42Z,22.56,51.92,52.01
2019-03-27T10:48:57Z,21.14,53.34,51.33
2019-03-27T10:49:12Z,23.65,55.18,52.02
2019-03-27T10:49:27Z,25.42,55.07,52.71
2019-03-27T10:49:42Z,27.76,55.76,52.50
2019-03-27T10:49:57Z,25.75,54.14,52.71
2019-03-27T10:50:12Z,27.84,52.86,52.83";

    }

}
