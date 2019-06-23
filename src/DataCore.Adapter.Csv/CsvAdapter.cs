using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CsvHelper;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using DataCore.Adapter.RealTimeData.Utilities;

namespace DataCore.Adapter.Csv {

    /// <summary>
    /// App Store Connect adapter that uses a looping CSV file as its source data.
    /// </summary>
    /// <seealso cref="CsvAdapterOptions"/>
    public class CsvAdapter : IAdapter, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues, IDisposable {

        /// <summary>
        /// The regular expression used to parse tag properties from a tag field.
        /// </summary>
        private static readonly Regex s_tagFieldPropertyRegex = new Regex(@"(?<pname>.+)=(?<pval>.+)\|?", RegexOptions.Compiled);

        /// <summary>
        /// Fires when the adapter is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedSource = new CancellationTokenSource();

        /// <summary>
        /// CSV parsing task.
        /// </summary>
        private readonly Task<CsvDataSet> _csvParseTask;

        /// <inheritdoc/>
        public AdapterDescriptor Descriptor { get; }

        /// <summary>
        /// The adapter features.
        /// </summary>
        private readonly AdapterFeaturesCollection _features = new AdapterFeaturesCollection();

        /// <inheritdoc/>
        public IAdapterFeaturesCollection Features => _features;


        /// <summary>
        /// Creates a new <see cref="CsvAdapter"/> object.
        /// </summary>
        /// <param name="descriptor">
        ///   The adapter descriptor.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="options"/> fails validation.
        /// </exception>
        public CsvAdapter(AdapterDescriptor descriptor, CsvAdapterOptions options) {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            // Validate options.
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            System.ComponentModel.DataAnnotations.Validator.ValidateObject(options, new System.ComponentModel.DataAnnotations.ValidationContext(options));

            // Construct adapter features.
            _features.AddFromProvider(this);
            _features.AddFromProvider(new ReadHistoricalTagValuesHelper(this, this));

            var snapshotPushUpdateInterval = options.SnapshotPushUpdateInterval;
            if (snapshotPushUpdateInterval > 0) {
                _features.AddFromProvider(new CsvAdapterSnapshotSubscriptionManager(this, TimeSpan.FromMilliseconds(snapshotPushUpdateInterval)));
            }

            _csvParseTask = ReadCsvDataInternal(options, _disposedSource.Token);
        }


        /// <summary>
        /// Adds or replaces an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature interface type.
        /// </typeparam>
        /// <typeparam name="TFeatureImpl">
        ///   The feature implementation type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        protected void AddFeature<TFeature, TFeatureImpl>(TFeatureImpl feature) where TFeature : IAdapterFeature where TFeatureImpl : class, TFeature {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            _features.Add<TFeature, TFeatureImpl>(feature);
        }
 

        /// <summary>
        /// Parses CSV data using the specified options.
        /// </summary>
        /// <param name="options">
        ///   The CSV adapter options.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The parse result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="options"/> fails validation.
        /// </exception>
        public static async Task<CsvDataSet> ReadCsvData(CsvAdapterOptions options, CancellationToken cancellationToken) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            System.ComponentModel.DataAnnotations.Validator.ValidateObject(options, new System.ComponentModel.DataAnnotations.ValidationContext(options));

            return await ReadCsvDataInternal(options, cancellationToken);
        }


        /// <summary>
        /// Parses a tag definition from a CSV header field.
        /// </summary>
        /// <param name="item">
        ///   The CSV header field.
        /// </param>
        /// <param name="options">
        ///   The CSV adapter options.
        /// </param>
        /// <returns>
        ///   The tag definition, or <see langword="null"/> if a tag could not be parsed.
        /// </returns>
        private static TagDefinition ParseTagDefinition(string item, CsvAdapterOptions options) {
            if (string.IsNullOrWhiteSpace(item)) {
                return null;
            }

            if (!item.StartsWith("[") || !item.EndsWith("]")) { 
                // Assume that the entire item is the tag name; set the ID to be the same 
                // as the name.
                return new TagDefinition(item, item, null, null, TagDataType.Numeric, null, null);
            }

            item = item.TrimStart('[').TrimEnd(']');

            // The tag configuration can be specified as a set of semi colon-delimited key=value pairs.
            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var subItem in item.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) {
                var m = s_tagFieldPropertyRegex.Match(subItem);
                if (!m.Success) {
                    // No match; skip this sub-item.
                    continue;
                }

                props[m.Groups["pname"].Value] = m.Groups["pval"].Value;
            }

            string name;
            string id;
            string description;
            string units;
            string dataType;

            props.TryGetValue(nameof(name), out name);
            props.TryGetValue(nameof(id), out id);

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(id)) {
                // No name or ID specified.
                return null;
            }

            var states = new Dictionary<string, int>();
            const string statePrefix = "STATE_";
            foreach (var prop in props.Where(x => x.Key.StartsWith(statePrefix, StringComparison.OrdinalIgnoreCase)).Where(x => x.Key.Length > statePrefix.Length)) {
                if (int.TryParse(prop.Value, NumberStyles.Integer, options.CultureInfo, out var stateVal)) {
                    states[prop.Key.Substring(statePrefix.Length)] = stateVal;
                }
            }

            return new TagDefinition(
                id ?? name,
                name ?? id,
                props.TryGetValue(nameof(description), out description) 
                    ? description 
                    : string.Empty,
                props.TryGetValue(nameof(units), out units)
                    ? units
                    : string.Empty,
                states.Count > 0
                    ? TagDataType.State
                    : props.TryGetValue(nameof(dataType), out dataType) && Enum.TryParse<TagDataType>(dataType, out var dataTypeActual)
                        ? dataTypeActual
                        : TagDataType.Numeric,
                states.Count > 0
                    ? states
                    : null,
                null
            );
        }


        /// <summary>
        /// Parses CSV data using the specified options.
        /// </summary>
        /// <param name="options">
        ///   The CSV adapter options.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The parse result.
        /// </returns>
        private static async Task<CsvDataSet> ReadCsvDataInternal(CsvAdapterOptions options, CancellationToken cancellationToken) {
            var timeZone = string.IsNullOrWhiteSpace(options.TimeZone)
                ? TimeZoneInfo.Local
                : TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);

            if (timeZone == null) {
                throw new ArgumentException($"Invalid time zone ID: {options.TimeZone}.", nameof(options));
            }

            var tags = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);
            var sampleTimes = new List<DateTime>();
            var values = new ConcurrentDictionary<string, SortedList<DateTime, TagValue>>(StringComparer.OrdinalIgnoreCase);

            var result = new CsvDataSet() {
                Tags = tags,
                Values = values,
                IsDataLoopingAllowed = options.IsDataLoopingAllowed
            };

            var csvConfig = new CsvHelper.Configuration.Configuration() {
                PrepareHeaderForMatch = (header, index) => header?.Trim()?.ToUpperInvariant(),
                CultureInfo = options?.CultureInfo ?? CultureInfo.CurrentCulture
            };

            var timeStampColumnIndex = options.TimeStampFieldIndex;
            var columnIndexToTagMap = new Dictionary<int, TagDefinition>();

            using (var stream = options.GetCsvStream())
            using (var reader = new StreamReader(stream))
            using (var csvParser = new CsvParser(reader, csvConfig)) {
                // Read fields.
                var fields = await csvParser.ReadAsync().ConfigureAwait(false);
                result.RowsRead++;

                if (timeStampColumnIndex > fields.Length) {
                    throw new ArgumentException($"Time stamp column index is set to {timeStampColumnIndex}, but the CSV only contains {fields.Length} columns.", nameof(options));
                }

                for (var i = 0; i < fields.Length; i++) {
                    if (i == timeStampColumnIndex) {
                        continue;
                    }

                    var fieldName = fields[i];

                    // Create tag definition for the field.
                    var tag = ParseTagDefinition(fieldName, options);
                    if (tag != null) {
                        tags[tag.Id] = tag;
                        columnIndexToTagMap[i] = tag;
                    }
                }

                var timeStampFormat = options.TimeStampFormat;
                var parseExact = !string.IsNullOrWhiteSpace(timeStampFormat);

                string[] currentRow;
                do {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    currentRow = await csvParser.ReadAsync().ConfigureAwait(false);
                    if (currentRow == null) {
                        // EOF
                        continue;
                    }

                    ++result.RowsRead;

                    var unparsedSampleTime = currentRow[timeStampColumnIndex];
                    if (string.IsNullOrWhiteSpace(unparsedSampleTime)) {
                        ++result.RowsSkipped;
                        continue;
                    }

                    DateTime sampleTime;

                    if (timeZone.Equals(TimeZoneInfo.Utc)) {
                        if (parseExact) {
                            if (!DateTime.TryParseExact(unparsedSampleTime, timeStampFormat, csvConfig.CultureInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out sampleTime)) {
                                result.RowsSkipped++;
                                continue;
                            }
                        }
                        else {
                            if (!DateTime.TryParse(unparsedSampleTime, csvConfig.CultureInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out sampleTime)) {
                                result.RowsSkipped++;
                                continue;
                            }
                        }
                    }
                    else {
                        if (parseExact) {
                            if (!DateTime.TryParseExact(unparsedSampleTime, timeStampFormat, csvConfig.CultureInfo, DateTimeStyles.None, out sampleTime)) {
                                result.RowsSkipped++;
                                continue;
                            }
                        }
                        else {
                            if (!DateTime.TryParse(unparsedSampleTime, csvConfig.CultureInfo, DateTimeStyles.None, out sampleTime)) {
                                result.RowsSkipped++;
                                continue;
                            }
                        }

                        sampleTime = TimeZoneInfo.ConvertTimeToUtc(sampleTime, timeZone);
                    }

                    var sampleTimeAdded = false;

                    foreach (var item in columnIndexToTagMap) {
                        var unparsedValue = currentRow[item.Key];
                        if (string.IsNullOrWhiteSpace(unparsedValue)) {
                            continue;
                        }

                        if (!sampleTimeAdded) {
                            sampleTimeAdded = true;
                            sampleTimes.Add(sampleTime);
                        }

                        var tag = item.Value;
                        var valuesForTag = values.GetOrAdd(tag.Id, key => new SortedList<DateTime, TagValue>());

                        var numericValue = tag.GetNumericValue(unparsedValue, csvConfig.CultureInfo);
                        var textValue = tag.GetTextValue(numericValue, csvConfig.CultureInfo);

                        valuesForTag.Add(
                            sampleTime,
                            TagValueBuilder
                                .Create()
                                .WithUtcSampleTime(sampleTime)
                                .WithValue(numericValue, textValue)
                                .WithStatus(TagValueStatus.Good)
                                .WithUnits(tag.Units)
                                .Build()
                        );
                    }
                } while (currentRow != null && !cancellationToken.IsCancellationRequested);
            }

            sampleTimes.Sort();
            result.TagsByName = result.Tags.Values.ToLookup(x => x.Name);
            result.UtcSampleTimes = sampleTimes.ToArray();
            result.UtcEarliestSampleTime = sampleTimes.Count > 0 ? sampleTimes.First() : DateTime.MinValue;
            result.UtcLatestSampleTime = sampleTimes.Count > 0 ? sampleTimes.Last() : DateTime.MinValue;
            result.DataSetDuration = result.UtcLatestSampleTime - result.UtcEarliestSampleTime;

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<TagDefinition> FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var dataSet = await _csvParseTask.WithCancellation(ct).ConfigureAwait(false);
                foreach (var item in dataSet.Tags.Values.Where(x => x.MatchesFilter(request)).SelectPage(request)) {
                    ch.TryWrite(item);
                }
            }, true, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<TagDefinition> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var dataSet = await _csvParseTask.WithCancellation(ct).ConfigureAwait(false);
                foreach (var item in request.Tags) {
                    var tag = GetTagByIdOrName(item, dataSet);
                    if (tag != null) {
                        ch.TryWrite(tag);
                    }
                }
            }, true, cancellationToken);

            return result;
        }


        /// <summary>
        /// Looks up a tag by ID or name.
        /// </summary>
        /// <param name="idOrName">
        ///   The tag ID or name.
        /// </param>
        /// <param name="dataSet">
        ///   The CSV data set containing the tag definitions.
        /// </param>
        /// <returns>
        ///   The matching tag definition, or <see langword="null"/>.
        /// </returns>
        private TagDefinition GetTagByIdOrName(string idOrName, CsvDataSet dataSet) {
            if (dataSet.Tags.TryGetValue(idOrName, out var tag)) {
                return tag;
            }

            return dataSet.TagsByName[idOrName]?.FirstOrDefault();
        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var dataSet = await _csvParseTask.WithCancellation(ct).ConfigureAwait(false);
                await ReadSnapshotTagValues(dataSet, request, ch, ct).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }


        /// <summary>
        /// Gets the snapshot values for the specified tags.
        /// </summary>
        /// <param name="dataSet">
        ///   The CSV data set.
        /// </param>
        /// <param name="request">
        ///   The snapshot request.
        /// </param>
        /// <param name="resultChannel">
        ///   The channel to write the query results to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will retrieve the snapshot tag values.
        /// </returns>
        private async Task ReadSnapshotTagValues(CsvDataSet dataSet, ReadSnapshotTagValuesRequest request, ChannelWriter<TagValueQueryResult> resultChannel, CancellationToken cancellationToken) {
            var tags = request.Tags.Select(x => GetTagByIdOrName(x, dataSet)).Where(x => x != null).ToArray();
            var dataSetDuration = dataSet.DataSetDuration;
            var earliestSampleTimeUtc = dataSet.UtcEarliestSampleTime;
            var latestSampleTimeUtc = dataSet.UtcLatestSampleTime;

            // If we don't have any valid tags in the request, or if we don't have any CSV data to work with, 
            // return a null value for each valid tag.
            if (tags.Length == 0 || dataSetDuration <= TimeSpan.Zero) {
                return;
            }

            var now = DateTime.UtcNow;

            if (!dataSet.IsDataLoopingAllowed) {
                foreach (var tag in tags) {
                    if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
                        continue;
                    }

                    var snapshot = now > dataSet.UtcLatestSampleTime
                        // We're past the end of the data set - the snapshot value is the last value 
                        // for the tag.
                        ? valuesForTag.Values.LastOrDefault()
                        : now < dataSet.UtcEarliestSampleTime
                            // We're before the start of the data set - the snapshot value is the 
                            // first value for the tag.
                            ? valuesForTag.Values.FirstOrDefault()
                            // We're inside the data set - the snapshot value is the last value earlier 
                            // than or at the current time.
                            : valuesForTag.Values.LastOrDefault(x => x.UtcSampleTime <= now);

                    if (snapshot != null && await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                        resultChannel.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, snapshot));
                    }
                }
                return;
            }

            if (now >= earliestSampleTimeUtc && now <= latestSampleTimeUtc) {
                // We're inside the data set - the snapshot value is the last value earlier than or 
                // at the current time.
                foreach (var tag in tags) {
                    if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
                        continue;
                    }

                    var snapshot = valuesForTag.Values.LastOrDefault(x => x.UtcSampleTime <= now);

                    if ( snapshot != null && await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                        resultChannel.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, snapshot));
                    }
                }
                return;
            }

            // We're outside of the data set. Offset the CSV dates until "now" is inside the set, and 
            // then get the value at or immediately before now.

            var offset = TimeSpan.Zero;

            if (now < earliestSampleTimeUtc) {
                // If utcStartTime is less than the earliest CSV sample time, we need to calculate a 
                // negative offset initially.
                var adjustedDataSetStartTime = earliestSampleTimeUtc;
                while (now < adjustedDataSetStartTime) {
                    offset = offset.Subtract(dataSetDuration);
                    adjustedDataSetStartTime = adjustedDataSetStartTime.Subtract(dataSetDuration);
                }
            }
            else if (now > latestSampleTimeUtc) {
                // If utcStartTime is greater than the latest CSV sample time, we need to calculate a 
                // positive offset initially.
                var adjustedDataSetEndTime = latestSampleTimeUtc;
                while (now > adjustedDataSetEndTime) {
                    offset = offset.Add(dataSetDuration);
                    adjustedDataSetEndTime = adjustedDataSetEndTime.Add(dataSetDuration);
                }
            }

            foreach (var tagNameOrId in request.Tags) {
                var tag = GetTagByIdOrName(tagNameOrId, dataSet);
                if (tag == null) {
                    continue;
                }
                if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
                    continue;
                }

                // Get the value at or immediately before now.
                var snapshot = valuesForTag.Values.LastOrDefault(x => x.UtcSampleTime.Add(offset) <= now);

                if (snapshot != null && await resultChannel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                    resultChannel.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, TagValueBuilder.CreateFromExisting(snapshot).WithUtcSampleTime(snapshot.UtcSampleTime.Add(offset)).Build()));
                }
            }

        }


        /// <inheritdoc/>
        public ChannelReader<TagValueQueryResult> ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var dataSet = await _csvParseTask.WithCancellation(ct).ConfigureAwait(false);
                await ReadRawTagValues(dataSet, request, ch, ct).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }


        /// <summary>
        /// Gets raw historical data for the specified tags.
        /// </summary>
        /// <param name="dataSet">
        ///   The CSV data set.
        /// </param>
        /// <param name="request">
        ///   The data query.
        /// </param>
        /// <param name="writer">
        ///   The channel to write the results to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will retrieve the raw values.
        /// </returns>
        private async Task ReadRawTagValues(CsvDataSet dataSet, ReadRawTagValuesRequest request, ChannelWriter<TagValueQueryResult> writer, CancellationToken cancellationToken) {
            var utcStartTime = request.UtcStartTime;
            var utcEndTime = request.UtcEndTime;
            var boundaryType = request.BoundaryType;
            var sampleCount = request.SampleCount;
            var tags = request.Tags.Select(x => GetTagByIdOrName(x, dataSet)).Where(x => x != null).ToArray();

            var utcEarliestSampleTime = dataSet.UtcEarliestSampleTime;
            var utcLatestSampleTime = dataSet.UtcLatestSampleTime;
            var dataSetDuration = dataSet.DataSetDuration;
            var utcSampleTimes = dataSet.UtcSampleTimes;

            // If the requested time range is inside the loaded data time range, or we are not 
            // allowed to loop round the data set, it's easy - we just get the raw values inside 
            // the requested time range for each valid tag.

            if (!dataSet.IsDataLoopingAllowed || (utcStartTime >= utcEarliestSampleTime && utcEndTime <= utcLatestSampleTime)) {
                // For every valid tag in the request, return the raw values inside the requested time range.

                foreach (var tag in tags) {
                    if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
                        continue;
                    }

                    var query = valuesForTag
                        .Values
                        .Where(x => x.UtcSampleTime >= utcStartTime && x.UtcSampleTime <= utcEndTime);

                    if (sampleCount > 0) {
                        query = query.Take(sampleCount);
                    }

                    foreach (var value in query) {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (await writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                            writer.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, value));
                        }
                    }
                }

                return;
            }

            // The time stamp offset that we have to apply to the original CSV samples in order to create 
            // raw samples between utcStartTime and utcEndTime. We'll calculate an initial offset based 
            // on utcStartTime's position compared to the earliest or latest sample in the CSV data set, 
            // and we'll then adjust it every time we iterate over the original CSV data.
            var offset = TimeSpan.Zero;

            if (utcStartTime < utcEarliestSampleTime) {
                // If utcStartTime is less than the earliest CSV sample time, we need to calculate a 
                // negative offset initially.
                var adjustedDataSetStartTime = utcEarliestSampleTime;
                while (utcStartTime < adjustedDataSetStartTime) {
                    offset = offset.Subtract(dataSetDuration);
                    adjustedDataSetStartTime = adjustedDataSetStartTime.Subtract(dataSetDuration);
                }
            }
            else if (utcStartTime > utcLatestSampleTime) {
                // If utcStartTime is greater than the latest CSV sample time, we need to calculate a 
                // positive offset initially.
                var adjustedDataSetEndTime = utcLatestSampleTime;
                while (utcStartTime > adjustedDataSetEndTime) {
                    offset = offset.Add(dataSetDuration);
                    adjustedDataSetEndTime = adjustedDataSetEndTime.Add(dataSetDuration);
                }
            }

            // Now that we've calculated our initial offset, we need to find the index of the sample time 
            // in the CSV data that, when shifted by our offset, is greater than or equal to utcStartTime.
            // This will be ur starting point as we iterate over the CSV data.
            var startingIndex = 0;
            for (; startingIndex < utcSampleTimes.Length; startingIndex++) {
                var tmp = utcSampleTimes[startingIndex].Add(offset);
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

            // We'll set this to false when we don't need to iterate over the CSV data any more.
            var @continue = true;
            var iterations = 0;
            var continueOnce = boundaryType == RawDataBoundaryType.Outside;
            do {
                // Starting at startingIndex, we'll iterate over the sample times in the CSV data.
                for (var i = startingIndex; i < utcSampleTimes.Length; i++) {
                    if (sampleCount > 0) {
                        ++iterations;
                        if (iterations > sampleCount) {
                            continueOnce = false;
                            @continue = false;
                            break;
                        }
                    }

                    // Get the unmodified CSV sample time.
                    var unmodifiedSampleTime = utcSampleTimes[i];
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
                        cancellationToken.ThrowIfCancellationRequested();

                        SortedList<DateTime, TagValue> csvValuesForTag;
                        // If there are no raw values for the current tag, or if we have already exceeded 
                        // the maximum number of raw samples we are allowed to use in a query, move to the 
                        // next tag.
                        if (!dataSet.Values.TryGetValue(tag.Id, out csvValuesForTag)) {
                            continue;
                        }

                        TagValue unmodifiedSample;
                        if (!csvValuesForTag.TryGetValue(unmodifiedSampleTime, out unmodifiedSample)) {
                            continue;
                        }

                        // If the time stamp offset is currently zero, we'll just use the original CSV 
                        // sample, to prevent us from creating unnecessary instances of DataCoreTagValue.
                        var sample = offset.Equals(TimeSpan.Zero)
                            ? unmodifiedSample
                            : TagValueBuilder.CreateFromExisting(unmodifiedSample).WithUtcSampleTime(sampleTimeThisIteration).Build();

                        if (await writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false)) {
                            writer.TryWrite(new TagValueQueryResult(tag.Id, tag.Name, sample));
                        }
                    }
                }

                if (@continue) {
                    // We've now iterated over the CSV data, but we still need more raw data before we 
                    // can stop.  We'll shift the offset forward by one iteration, and move back to the 
                    // first sample in the CSV data.
                    offset = offset.Add(dataSetDuration);
                    startingIndex = 0;
                }
            }
            while (@continue);
        }


        /// <summary>
        /// Releases managed resources.
        /// </summary>
        public void Dispose() {
            _disposedSource.Cancel();
            _disposedSource.Dispose();
            // Wait for parse task to finish.
            _csvParseTask.GetAwaiter().GetResult();
        }


        /// <summary>
        /// Helper class for providing <see cref="ISnapshotTagValuePush"/> functionality to a 
        /// <see cref="CsvAdapter"/>.
        /// </summary>
        private class CsvAdapterSnapshotSubscriptionManager : PollingSnapshotTagValueSubscriptionManager {

            /// <summary>
            /// The owning adapter.
            /// </summary>
            private readonly CsvAdapter _adapter;


            /// <summary>
            /// Creates a new <see cref="CsvAdapterSnapshotSubscriptionManager"/> object.
            /// </summary>
            /// <param name="adapter">
            ///   The owning adapter.
            /// </param>
            /// <param name="pollingInterval">
            ///   The interval to poll for new values at.
            /// </param>
            public CsvAdapterSnapshotSubscriptionManager(CsvAdapter adapter, TimeSpan pollingInterval) : base(pollingInterval) {
                _adapter = adapter;
            }


            /// <inheritdoc/>
            protected override ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                var channel = Channel.CreateUnbounded<TagIdentifier>(new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true
                });

                channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var dataSet = await _adapter._csvParseTask.WithCancellation(ct).ConfigureAwait(false);
                    foreach (var item in tagNamesOrIds) {
                        var tag = _adapter.GetTagByIdOrName(item, dataSet);
                        if (tag != null) {
                            ch.TryWrite(new TagIdentifier(tag.Id, tag.Name));
                        }
                    }
                }, true, cancellationToken);

                return channel;
            }


            /// <inheritdoc/>
            protected override async Task GetSnapshotTagValues(IEnumerable<string> tagIds, ChannelWriter<TagValueQueryResult> channel, CancellationToken cancellationToken) {
                var reader = _adapter.ReadSnapshotTagValues(null, new ReadSnapshotTagValuesRequest() {
                    Tags = tagIds.ToArray()
                }, cancellationToken);

                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var value)) {
                        continue;
                    }

                    channel.TryWrite(value);
                }
            }


            /// <inheritdoc/>
            protected override void OnPollingError(Exception error) {
                // Do nothing.
            }

        }

    }
}
