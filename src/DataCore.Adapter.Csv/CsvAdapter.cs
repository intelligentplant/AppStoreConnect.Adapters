﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CsvHelper;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;



namespace DataCore.Adapter.Csv {

    /// <summary>
    /// App Store Connect adapter that uses a looping CSV file as its source data.
    /// </summary>
    /// <seealso cref="CsvAdapterOptions"/>
    [AdapterMetadata(
        "https://www.intelligentplant.com/app-store-connect/adapters/csv",
        ResourceType = typeof(Resources),
        Name = nameof(Resources.AdapterMetadata_DisplayName),
        Description = nameof(Resources.AdapterMetadata_Description),
        HelpUrl = "https://github.com/intelligentplant/AppStoreConnect.Adapters/tree/main/src/DataCore.Adapter.Csv"
    )]
    public class CsvAdapter : AdapterBase<CsvAdapterOptions>, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues {

        /// <summary>
        /// The regular expression used to parse tag properties from a tag field.
        /// </summary>
        private static readonly Regex s_tagFieldPropertyRegex = new Regex(@"(?<pname>.+)=(?<pval>.+)\|?", RegexOptions.Compiled);

        /// <summary>
        /// CSV parsing task.
        /// </summary>
        private Lazy<Task<CsvDataSet>> _csvParseTask = default!;


        /// <summary>
        /// Creates a new <see cref="CsvAdapter"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="loggerFactory">
        ///   The logger factory for the adapter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="options"/> fails validation.
        /// </exception>
        public CsvAdapter(string id, CsvAdapterOptions options, IBackgroundTaskService backgroundTaskService, ILoggerFactory loggerFactory)
            : base(id, options, backgroundTaskService, loggerFactory) {
            AddFeatures();
        }


        /// <summary>
        /// Creates a new <see cref="CsvAdapter"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger factory for the adapter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="options"/> fails validation.
        /// </exception>
        public CsvAdapter(string id, Microsoft.Extensions.Options.IOptions<CsvAdapterOptions> options, IBackgroundTaskService backgroundTaskService, ILoggerFactory logger)
            : base(id, options, backgroundTaskService, logger) {
            AddFeatures();
        }



        /// <summary>
        /// Creates a new <see cref="CsvAdapter"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. Specify <see langword="null"/> or white space to generate an ID 
        ///   automatically.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger factory for the adapter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="options"/> fails validation.
        /// </exception>
        public CsvAdapter(string id, Microsoft.Extensions.Options.IOptionsMonitor<CsvAdapterOptions> options, IBackgroundTaskService backgroundTaskService, ILoggerFactory logger)
            : base(id, options, backgroundTaskService, logger) {
            AddFeatures();
        }


        private void AddFeatures() {
            // Construct adapter features.
            AddFeatures(ReadHistoricalTagValues.ForAdapter(this));

            var snapshotPushUpdateInterval = Options.SnapshotPushUpdateInterval;
            if (snapshotPushUpdateInterval > 0) {
                var simulatedPush = new PollingSnapshotTagValuePush(
                    this.GetFeature<IReadSnapshotTagValues>()!,
                    new PollingSnapshotTagValuePushOptions() {
                        Id = Descriptor.Id,
                        PollingInterval = TimeSpan.FromMilliseconds(snapshotPushUpdateInterval),
                        TagResolver = SnapshotTagValuePush.CreateTagResolverFromAdapter(this)
                    },
                    BackgroundTaskService,
                    LoggerFactory.CreateLogger<PollingSnapshotTagValuePush>()
                );
                AddFeature(typeof(ISnapshotTagValuePush), simulatedPush);
            }
        }


        /// <inheritdoc/>
        protected override async Task StartAsync(CancellationToken cancellationToken) {
            _csvParseTask = new Lazy<Task<CsvDataSet>>(() => ReadCsvDataInternal(Options, this, StopToken), LazyThreadSafetyMode.ExecutionAndPublication);
            await _csvParseTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
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

            return await ReadCsvDataInternal(options, null, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Parses a tag definition from a CSV header field.
        /// </summary>
        /// <param name="definition">
        ///   The CSV header field.
        /// </param>
        /// <param name="options">
        ///   The CSV adapter options.
        /// </param>
        /// <param name="adapter">
        ///   The adapter instance that the tag belongs to.
        /// </param>
        /// <returns>
        ///   The tag definition, or <see langword="null"/> if a tag could not be parsed.
        /// </returns>
        private static TagDefinition ParseTagDefinition(string definition, CsvAdapterOptions options, CsvAdapter? adapter) {
            if (string.IsNullOrWhiteSpace(definition)) {
                return null!;
            }

            if (!definition.StartsWith("[", StringComparison.Ordinal) || !definition.EndsWith("]", StringComparison.Ordinal)) {
                // Assume that the entire item is the tag name; set the ID to be the same 
                // as the name.
                return new TagDefinitionBuilder(definition, definition)
                    .WithDataType(VariantType.Double)
                    .WithProperty(nameof(definition), definition)
                    .WithLabels("CSV")
                    .Build();
            }

            var definitionOriginal = definition;
            definition = definition.TrimStart('[').TrimEnd(']');

            // The tag configuration can be specified as a set of semi colon-delimited key=value pairs.
            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var subItem in definition.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) {
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

            props.TryGetValue(nameof(name), out name!);
            props.TryGetValue(nameof(id), out id!);

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(id)) {
                // No name or ID specified.
                return null!;
            }

            var states = new Dictionary<string, int>();
            const string statePrefix = "STATE_";
            foreach (var prop in props.Where(x => x.Key.StartsWith(statePrefix, StringComparison.OrdinalIgnoreCase)).Where(x => x.Key.Length > statePrefix.Length)) {
                if (int.TryParse(prop.Value, NumberStyles.Integer, options.CultureInfo, out var stateVal)) {
                    states[prop.Key.Substring(statePrefix.Length)] = stateVal;
                }
            }

            return new TagDefinitionBuilder()
                .WithId(id ?? name!)
                .WithName(name ?? id!)
                .WithDescription(props.TryGetValue(nameof(description), out description!)
                    ? description
                    : string.Empty)
                .WithUnits(props.TryGetValue(nameof(units), out units!)
                    ? units
                    : string.Empty)
                .WithDataType(states.Count > 0
                    ? VariantType.Int32
                    : props.TryGetValue(nameof(dataType), out dataType!) && Enum.TryParse<VariantType>(dataType, out var dataTypeActual)
                        ? dataTypeActual
                        : VariantType.Double)
                .WithDigitalStates(states.Count > 0
                    ? states.Select(x => DigitalState.Create(x.Key, x.Value))
                    : null)
                .WithSupportedFeatures(adapter!, RealTimeData.Utilities.AggregationHelper.GetDefaultDataFunctions())
                .WithProperty(nameof(definition), definitionOriginal)
                .WithLabels("CSV")
                .Build();
        }


        /// <summary>
        /// Parses CSV data using the specified options.
        /// </summary>
        /// <param name="options">
        ///   The CSV adapter options.
        /// </param>
        /// <param name="adapter">
        ///   The adapter instance that the data is being parsed by.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The parse result.
        /// </returns>
        private static async Task<CsvDataSet> ReadCsvDataInternal(CsvAdapterOptions options, CsvAdapter? adapter, CancellationToken cancellationToken) {
            var timeZone = string.IsNullOrWhiteSpace(options.TimeZone)
                ? TimeZoneInfo.Local
                : TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);

            if (timeZone == null) {
                throw new ArgumentException($"Invalid time zone ID: {options.TimeZone}.", nameof(options));
            }

            var cultureInfo = options.CultureInfo ?? CultureInfo.CurrentCulture;

            var tags = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);
            var sampleTimes = new List<DateTime>();
            var values = new ConcurrentDictionary<string, SortedList<DateTime, TagValueExtended>>(StringComparer.OrdinalIgnoreCase);

            var result = new CsvDataSet() {
                Tags = tags,
                Values = values,
                IsDataLoopingAllowed = options.IsDataLoopingAllowed
            };
            
            var csvConfig = new CsvHelper.Configuration.CsvConfiguration(cultureInfo) {
                PrepareHeaderForMatch = args => args.Header?.Trim()?.ToUpperInvariant()!
            };

            var timeStampColumnIndex = options.TimeStampFieldIndex;
            var columnIndexToTagMap = new Dictionary<int, TagDefinition>();

            if (string.IsNullOrWhiteSpace(options.CsvFile) && options.GetCsvStream == null) {
                throw new ArgumentException(string.Format(cultureInfo, Resources.Error_NoCsvFileDefined, nameof(CsvAdapterOptions.CsvFile), nameof(CsvAdapterOptions.GetCsvStream)), nameof(options));
            }

            Func<Stream> getCsvStream;
            if (string.IsNullOrWhiteSpace(options.CsvFile)) {
                getCsvStream = options.GetCsvStream!;
            }
            else {
                var csvFile = Path.IsPathRooted(options.CsvFile)
                    ? options.CsvFile
                    : Path.Combine(AppContext.BaseDirectory, options.CsvFile);
                getCsvStream = () => new FileStream(csvFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            using (var stream = getCsvStream())
            using (var reader = new StreamReader(stream))
            using (var csvParser = new CsvParser(reader, csvConfig)) {
                // Read fields.
                if (!await csvParser.ReadAsync().ConfigureAwait(false)) {
                    throw new InvalidOperationException("Unable to read CSV header.");
                }
                var fields = csvParser.Record;
                if (fields == null) {
                    return result;
                }
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
                    var tag = ParseTagDefinition(fieldName, options, adapter);
                    if (tag != null) {
                        tags[tag.Id] = tag;
                        columnIndexToTagMap[i] = tag;
                    }
                }

                var timeStampFormat = options.TimeStampFormat;
                var parseExact = !string.IsNullOrWhiteSpace(timeStampFormat);

                string[]? currentRow;
                do {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    currentRow = await csvParser.ReadAsync().ConfigureAwait(false) 
                        ? csvParser.Record 
                        : null;

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
                        var isDigitalTag = tag.IsDigitalStateTag();
                        var valuesForTag = values.GetOrAdd(tag.Id, key => new SortedList<DateTime, TagValueExtended>());

                        var hasNumericValue = double.TryParse(unparsedValue, NumberStyles.Any, csvConfig.CultureInfo, out var numericValue);

                        // If the parsed value is numeric, we will pass that as the value of the 
                        // sample, casting it to an int if this is a digital tag (since digital 
                        // state values must be integers). Otherwise, we will pass the unparsed 
                        // string value as the value of the sample.
                        var primaryValue = hasNumericValue
                            ? isDigitalTag
                                ? (int) Math.Truncate(numericValue)
                                // Explicitly cast to object, so that the truncated numeric value 
                                // above is not implicitly recast from int back to double; we want  
                                // the Variant that contains the value to infer the correct  
                                // underlying type for the value.
                                : (object) numericValue
                            : unparsedValue;

                        string? displayValue = null;
                        if (hasNumericValue && isDigitalTag) {
                            // This is a digital tag; we'll add the digital state name as a secondary value.
                            var state = tag.States.FirstOrDefault(x => x.Value == numericValue);
                            if (state != null) {
                                displayValue = state.Name;
                            }
                        }

                        var builder = new TagValueBuilder()
                            .WithUtcSampleTime(sampleTime)
                            .WithValue(primaryValue, displayValue)
                            .WithStatus(TagValueStatus.Good)
                            .WithUnits(tag.Units);

                        valuesForTag.Add(sampleTime, builder.Build());
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


        /// <summary>
        /// Adds tags to the adapter that are not defined in the CSV.
        /// </summary>
        /// <param name="tags">
        ///   The tags to add.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will add the tags.
        /// </returns>
        protected async Task AddTags(IEnumerable<TagDefinition> tags, CancellationToken cancellationToken) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            var dataSet = await _csvParseTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            var rebuildLookup = false;

            foreach (var tag in tags) {
                if (tag == null) {
                    continue;
                }
                dataSet.Tags[tag.Id] = tag;
                rebuildLookup = true;
            }
            
            if (rebuildLookup) {
                dataSet.TagsByName = dataSet.Tags.Values.ToLookup(x => x.Name);
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var dataSet = await _csvParseTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
            foreach (var item in dataSet.Tags.Values.ApplyFilter(request)) {
                yield return item.Clone(request.ResultFields);
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagDefinition> GetTags(
            IAdapterCallContext context,
            GetTagsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var dataSet = await _csvParseTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
            foreach (var item in request.Tags) {
                var tag = GetTagByIdOrName(item, dataSet);
                if (tag == null) {
                    continue;
                }
                yield return TagDefinition.FromExisting(tag);
            }
        }


        /// <inheritdoc/>
        public IAsyncEnumerable<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request, 
            CancellationToken cancellationToken
        ) {
            return Array.Empty<AdapterProperty>().ToAsyncEnumerable(cancellationToken);
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
        private static TagDefinition? GetTagByIdOrName(string idOrName, CsvDataSet dataSet) {
            if (dataSet.Tags.TryGetValue(idOrName, out var tag)) {
                return tag;
            }

            return dataSet.TagsByName[idOrName]?.FirstOrDefault();
        }


        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
            IAdapterCallContext context,
            ReadSnapshotTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var dataSet = await _csvParseTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
            foreach (var item in ReadSnapshotTagValuesInternal(dataSet, request, cancellationToken)) {
                yield return item;
            }
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will retrieve the snapshot tag values.
        /// </returns>
        private static IEnumerable<TagValueQueryResult> ReadSnapshotTagValuesInternal(
            CsvDataSet dataSet, 
            ReadSnapshotTagValuesRequest request, 
            CancellationToken cancellationToken
        ) {
            var tags = request.Tags.Select(x => GetTagByIdOrName(x, dataSet)).Where(x => x != null).ToArray();
            var dataSetDuration = dataSet.DataSetDuration;
            var earliestSampleTimeUtc = dataSet.UtcEarliestSampleTime;
            var latestSampleTimeUtc = dataSet.UtcLatestSampleTime;

            // If we don't have any valid tags in the request, or if we don't have any CSV data to work with, 
            // return a null value for each valid tag.
            if (tags.Length == 0 || dataSetDuration <= TimeSpan.Zero) {
                yield break;
            }

            var now = DateTime.UtcNow;

            if (!dataSet.IsDataLoopingAllowed) {
                foreach (var tag in tags) {
                    cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
#pragma warning restore CS8602 // Dereference of a possibly null reference.
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

                    if (snapshot == null) {
                        continue;
                    }

                    yield return TagValueQueryResult.Create(tag.Id, tag.Name, snapshot);
                }
                yield break;
            }

            if (now >= earliestSampleTimeUtc && now <= latestSampleTimeUtc) {
                // We're inside the data set - the snapshot value is the last value earlier than or 
                // at the current time.
                foreach (var tag in tags) {
                    cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        continue;
                    }

                    var snapshot = valuesForTag.Values.LastOrDefault(x => x.UtcSampleTime <= now);
                    if (snapshot == null) {
                        continue;
                    }

                    yield return TagValueQueryResult.Create(tag.Id, tag.Name, snapshot);
                }
                yield break;
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
                cancellationToken.ThrowIfCancellationRequested();

                var tag = GetTagByIdOrName(tagNameOrId, dataSet);
                if (tag == null) {
                    continue;
                }
                if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
                    continue;
                }

                // Get the value at or immediately before now.
                var snapshot = valuesForTag.Values.LastOrDefault(x => x.UtcSampleTime.Add(offset) <= now);

                if (snapshot == null) {
                    continue;
                }

                yield return TagValueQueryResult.Create(tag.Id, tag.Name, new TagValueBuilder(snapshot).WithUtcSampleTime(snapshot.UtcSampleTime.Add(offset)).Build());
            }

        }


        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<TagValueQueryResult> ReadRawTagValues(
            IAdapterCallContext context,
            ReadRawTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await Task.Yield();

            var dataSet = await _csvParseTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
            foreach (var item in ReadRawTagValues(dataSet, request, cancellationToken)) {
                yield return item;
            }
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The raw values.
        /// </returns>
        private static IEnumerable<TagValueQueryResult> ReadRawTagValues(
            CsvDataSet dataSet, 
            ReadRawTagValuesRequest request, 
            CancellationToken cancellationToken
        ) {
            var utcStartTime = request.UtcStartTime;
            var utcEndTime = request.UtcEndTime;
            var boundaryType = request.BoundaryType;
            var sampleCount = request.SampleCount;
            var tags = request.Tags.Select(x => GetTagByIdOrName(x, dataSet)).Where(x => x != null).ToArray();

            var utcEarliestSampleTime = dataSet.UtcEarliestSampleTime;
            var utcLatestSampleTime = dataSet.UtcLatestSampleTime;
            var dataSetDuration = dataSet.DataSetDuration;
            var utcSampleTimes = dataSet.UtcSampleTimes.ToArray();

            // If the requested time range is inside the loaded data time range, or we are not 
            // allowed to loop round the data set, it's easy - we just get the raw values inside 
            // the requested time range for each valid tag.

            if (!dataSet.IsDataLoopingAllowed || (utcStartTime >= utcEarliestSampleTime && utcEndTime <= utcLatestSampleTime)) {
                // For every valid tag in the request, return the raw values inside the requested time range.

                foreach (var tag in tags) {
                    cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (!dataSet.Values.TryGetValue(tag.Id, out var valuesForTag)) {
#pragma warning restore CS8602 // Dereference of a possibly null reference.
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
                        yield return TagValueQueryResult.Create(tag.Id, tag.Name, value);
                    }
                }

                yield break;
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
            // This will be our starting point as we iterate over the CSV data.
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

                        SortedList<DateTime, TagValueExtended> csvValuesForTag;
                        // If there are no raw values for the current tag, or if we have already exceeded 
                        // the maximum number of raw samples we are allowed to use in a query, move to the 
                        // next tag.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (!dataSet.Values.TryGetValue(tag.Id, out csvValuesForTag!)) {
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            continue;
                        }

                        TagValueExtended unmodifiedSample;
                        if (!csvValuesForTag.TryGetValue(unmodifiedSampleTime, out unmodifiedSample!)) {
                            continue;
                        }

                        // If the time stamp offset is currently zero, we'll just use the original CSV 
                        // sample, to prevent us from creating unnecessary instances of DataCoreTagValue.
                        var sample = offset.Equals(TimeSpan.Zero)
                            ? unmodifiedSample
                            : new TagValueBuilder(unmodifiedSample).WithUtcSampleTime(sampleTimeThisIteration).Build();

                        yield return TagValueQueryResult.Create(tag.Id, tag.Name, sample);
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

    }
}
