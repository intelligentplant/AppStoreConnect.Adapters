using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.WaveGenerator {

    /// <summary>
    /// Adapter for generating wave signals.
    /// </summary>
    /// <seealso cref="WaveGeneratorAdapterOptions"/>
    /// <seealso cref="WaveGeneratorOptions"/>
    [AdapterMetadata(
        "https://www.intelligentplant.com/app-store-connect/adapters/wave-generator",
        ResourceType = typeof(Resources),
        Name = nameof(Resources.AdapterMetadata_DisplayName),
        Description = nameof(Resources.AdapterMetadata_Description)
    )]
    public class WaveGeneratorAdapter : AdapterBase<WaveGeneratorAdapterOptions>, ITagInfo, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues, IReadTagValuesAtTimes {

        /// <summary>
        /// The base timestamp to use for wave generator functions.
        /// </summary>
        private static readonly DateTime s_waveBaseTime =
#if NETSTANDARD2_1
            DateTime.UnixEpoch;
#else
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

        /// <summary>
        /// Default wave generator sample interval to use.
        /// </summary>
        private static readonly TimeSpan s_defaultSampleInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Holds pre-defined wave generators, indexed by name.
        /// </summary>
        private readonly ConcurrentDictionary<string, WaveGeneratorOptions> _tagDefinitions = new ConcurrentDictionary<string, WaveGeneratorOptions>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The definitions for properties included in tag definitions.
        /// </summary>
        private static readonly AdapterProperty[] s_tagPropertyDefinitions = { 
            AdapterProperty.Create(nameof(WaveGeneratorOptions.Type), Variant.Null),
            AdapterProperty.Create(nameof(WaveGeneratorOptions.Period), Variant.Null),
            AdapterProperty.Create(nameof(WaveGeneratorOptions.Amplitude), Variant.Null),
            AdapterProperty.Create(nameof(WaveGeneratorOptions.Phase), Variant.Null),
            AdapterProperty.Create(nameof(WaveGeneratorOptions.Offset), Variant.Null)
        };

        /// <summary>
        /// A regular expression for matching <c>name=value</c> pairs in a literal wave generator 
        /// function string.
        /// </summary>
        private static readonly Regex s_waveGeneratorLiteralRegex = new Regex(@"(?<name>[A-Za-z0-9\s]+?)=(?<value>[^;]+);?");


        /// <summary>
        /// Creates a new <see cref="WaveGeneratorAdapter"/> instance.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> for the adapter.
        /// </param>
        /// <param name="logger">
        ///   The <see cref="ILogger"/> for the adapter.
        /// </param>
        public WaveGeneratorAdapter(
            string id, 
            WaveGeneratorAdapterOptions options, 
            IBackgroundTaskService? backgroundTaskService = null, 
            ILogger<WaveGeneratorAdapter>? logger = null
        ) : base(id, options, backgroundTaskService, logger) {
            AddFeatures(PollingSnapshotTagValuePush.ForAdapter(this, GetSampleInterval()));
            AddFeatures(ReadHistoricalTagValues.ForAdapter(this));
        }


        /// <inheritdoc/>
        protected override Task StartAsync(CancellationToken cancellationToken) {
            LoadTagDefinitions();
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        protected override Task StopAsync(CancellationToken cancellationToken) {
            _tagDefinitions.Clear();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Loads default tag definitions, and tag definitions defined in the adapter options.
        /// </summary>
        private void LoadTagDefinitions() {
            _tagDefinitions.Clear();

            if (Options.Tags != null) {
                foreach (var item in Options.Tags) {
                    if (!TryParseWaveGeneratorOptions(item.Value, out var opts)) {
                        continue;
                    }

                    opts!.Name = item.Key;
                    _tagDefinitions[item.Key] = opts!;
                }
            }

            LoadDefaultTagDefinitions();
        }


        /// <summary>
        /// Loads default tag definitions.
        /// </summary>
        private void LoadDefaultTagDefinitions() {
            _tagDefinitions[nameof(WaveType.Sawtooth)] = new WaveGeneratorOptions() { Name = nameof(WaveType.Sawtooth), Type = WaveType.Sawtooth };
            _tagDefinitions[nameof(WaveType.Sinusoid)] = new WaveGeneratorOptions() { Name = nameof(WaveType.Sinusoid), Type = WaveType.Sinusoid };
            _tagDefinitions[nameof(WaveType.Square)] = new WaveGeneratorOptions() { Name = nameof(WaveType.Square), Type = WaveType.Square };
            _tagDefinitions[nameof(WaveType.Triangle)] = new WaveGeneratorOptions() { Name = nameof(WaveType.Triangle), Type = WaveType.Triangle };
        }


        /// <summary>
        /// Gets the sample interval to use for wave generator functions.
        /// </summary>
        /// <returns>
        ///   The sample interval to use.
        /// </returns>
        private TimeSpan GetSampleInterval() {
            return Options.SampleInterval <= TimeSpan.Zero 
                ? s_defaultSampleInterval 
                : Options.SampleInterval;
        }


        /// <summary>
        /// Tries to parse a literal string into a <see cref="WaveGeneratorOptions"/> instance.
        /// </summary>
        /// <param name="s">
        ///   The string to parse.
        /// </param>
        /// <param name="options">
        ///   The parsed <see cref="WaveGeneratorOptions"/> instance.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string was successfully parsed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   <paramref name="s"/> is specified as a set of semicolon-delimited <c>key=value</c> 
        ///   pairs, with each case-insensitive key corresponding to a member on the <see cref="WaveGeneratorOptions"/> 
        ///   type (e.g. <see cref="WaveGeneratorOptions.Type"/>, <see cref="WaveGeneratorOptions.Amplitude"/>).
        /// </para>
        /// 
        /// <para>Examples:</para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>Type=Sinusoid;Period=3600</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>Type=Triangle;Amplitude=50;Phase=60;Offset=27.664</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>type=square;amplitude=100</c></description>
        ///   </item>
        /// </list>
        /// 
        /// <para>
        ///   Any <see cref="WaveGeneratorOptions"/> member that is not specified in <paramref name="s"/> 
        ///   will be set to a default value.
        /// </para>
        /// 
        /// </remarks>
        public static bool TryParseWaveGeneratorOptions(string s, out WaveGeneratorOptions? options) {
            options = null;

            if (string.IsNullOrWhiteSpace(s)) {
                return false;
            }

            var m = s_waveGeneratorLiteralRegex.Match(s);
            if (!m.Success) {
                return false;
            }

            options = new WaveGeneratorOptions();
            for (; m.Success; m = m.NextMatch()) {
                var pName = m.Groups["name"].Value;
                var pVal = m.Groups["value"].Value;

                if (string.Equals(pName, nameof(WaveGeneratorOptions.Type), StringComparison.OrdinalIgnoreCase) && Enum.TryParse<WaveType>(pVal, out var waveType)) {
                    options.Type = waveType;
                }
                else if (string.Equals(pName, nameof(WaveGeneratorOptions.Period), StringComparison.OrdinalIgnoreCase) && double.TryParse(pVal, out var period) && period > 0) {
                    options.Period = period;
                }
                else if (string.Equals(pName, nameof(WaveGeneratorOptions.Amplitude), StringComparison.OrdinalIgnoreCase) && double.TryParse(pVal, out var amplitude) && amplitude > 0) {
                    options.Amplitude = amplitude;
                }
                else if (string.Equals(pName, nameof(WaveGeneratorOptions.Phase), StringComparison.OrdinalIgnoreCase) && double.TryParse(pVal, out var phase)) {
                    options.Phase = phase;
                }
                else if (string.Equals(pName, nameof(WaveGeneratorOptions.Offset), StringComparison.OrdinalIgnoreCase) && double.TryParse(pVal, out var offset)) {
                    options.Offset = offset;
                }
            }

            return true;
        }


        /// <summary>
        /// Tries to get the wave generator options for the specified generator name.
        /// </summary>
        /// <param name="name">
        ///   The wave generator name.
        /// </param>
        /// <param name="options">
        ///   The wave generator options.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the wave generator name was resolved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   If <see cref="WaveGeneratorAdapterOptions.EnableAdHocGenerators"/> is <see langword="false"/>, 
        ///   <paramref name="name"/> must correspond to a pre-defined wave generator function. If 
        ///   <see cref="WaveGeneratorAdapterOptions.EnableAdHocGenerators"/> is <see langword="true"/>, 
        ///   name can be a pre-defined function or it can be a literal wave generator configuration 
        ///   that will be parsed using <see cref="TryParseWaveGeneratorOptions"/>.
        /// </remarks>
        private bool TryGetWaveGeneratorOptions(string name, out WaveGeneratorOptions? options) {
            options = null;
            if (string.IsNullOrWhiteSpace(name)) {
                return false;
            }

            if (_tagDefinitions.TryGetValue(name, out options)) {
                return true;
            }

            if (Options.EnableAdHocGenerators && TryParseWaveGeneratorOptions(name, out options)) {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Creates a <see cref="TagDefinition"/> instance using the specified ID/name and wave 
        /// generator options.
        /// </summary>
        /// <param name="name">
        ///   The tag ID and name.
        /// </param>
        /// <param name="options">
        ///   The wave generator options for the tag.
        /// </param>
        /// <param name="fields">
        ///   The fields to include in the generated tag definition.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinition"/> object.
        /// </returns>
        private static TagDefinition ToTagDefinition(string name, WaveGeneratorOptions options, TagDefinitionFields fields) {
            var result = TagDefinitionBuilder
                .Create(name, name)
                .WithProperty(nameof(WaveGeneratorOptions.Type), options.Type.ToString())
                .WithProperty(nameof(WaveGeneratorOptions.Period), options.Period)
                .WithProperty(nameof(WaveGeneratorOptions.Amplitude), options.Amplitude)
                .WithProperty(nameof(WaveGeneratorOptions.Phase), options.Phase)
                .WithProperty(nameof(WaveGeneratorOptions.Offset), options.Offset)
                .Build();

            if (fields == TagDefinitionFields.All) {
                return result;
            }

            return result.Clone(fields);
        }


        /// <summary>
        /// Calculates the value of a wave generator function at the specified sample time.
        /// </summary>
        /// <param name="utcSampleTime">
        ///   The UTC sample time to calculate the value at.
        /// </param>
        /// <param name="options">
        ///   The options for the wave generator.
        /// </param>
        /// <returns>
        ///   The computed value.
        /// </returns>
        private static double CalculateValue(DateTime utcSampleTime, WaveGeneratorOptions options) {
            var time = (utcSampleTime - s_waveBaseTime).TotalSeconds;

            switch (options.Type) {
                case WaveType.Sawtooth:
                    return SawtoothWave(time, options.Period, options.Amplitude, options.Phase, options.Offset);
                case WaveType.Square:
                    return SquareWave(time, options.Period, options.Amplitude, options.Phase, options.Offset);
                case WaveType.Triangle:
                    return TriangleWave(time, options.Period, options.Amplitude, options.Phase, options.Offset);
                case WaveType.Sinusoid:
                default:
                    return SinusoidWave(time, options.Period, options.Amplitude, options.Phase, options.Offset);
            }
        }


        /// <summary>
        /// Sinusoid wave function.
        /// </summary>
        /// <param name="time">
        ///   The sample time.
        /// </param>
        /// <param name="period">
        ///   The wave period.
        /// </param>
        /// <param name="amplitude">
        ///   The amplitude.
        /// </param>
        /// <param name="phase">
        ///   The phase of the wave (in seconds) relative to the base function.
        /// </param>
        /// <param name="offset">
        ///   The value offset of the wave, relative to the base function.
        /// </param>
        /// <returns>
        ///   The sinusoid value.
        /// </returns>
        private static double SinusoidWave(
            double time,
            double period, 
            double amplitude,
            double phase,
            double offset
        ) {
            return amplitude * (Math.Sin(2 * Math.PI * (1 / period) * (time + phase))) + offset;
        }


        /// <summary>
        /// Sawtooth wave function.
        /// </summary>
        /// <param name="time">
        ///   The sample time.
        /// </param>
        /// <param name="period">
        ///   The wave period.
        /// </param>
        /// <param name="amplitude">
        ///   The amplitude.
        /// </param>
        /// <param name="phase">
        ///   The phase of the wave (in seconds) relative to the base function.
        /// </param>
        /// <param name="offset">
        ///   The value offset of the wave, relative to the base function.
        /// </param>
        /// <returns>
        ///   The sawtooth value.
        /// </returns>
        private static double SawtoothWave(
            double time, 
            double period, 
            double amplitude,
            double phase,
            double offset
        ) {
            return (2 * amplitude / Math.PI) * Math.Atan(1 / Math.Tan(Math.PI / period * (time + phase))) + offset;
        }


        /// <summary>
        /// Square wave function.
        /// </summary>
        /// <param name="time">
        ///   The sample time.
        /// </param>
        /// <param name="period">
        ///   The wave period.
        /// </param>
        /// <param name="amplitude">
        ///   The amplitude.
        /// </param>
        /// <param name="phase">
        ///   The phase of the wave (in seconds) relative to the base function.
        /// </param>
        /// <param name="offset">
        ///   The value offset of the wave, relative to the base function.
        /// </param>
        private static double SquareWave(
            double time, 
            double period,
            double amplitude,
            double phase,
            double offset
        ) {
            return amplitude * Math.Sign(SinusoidWave(time, period, 1, phase, 0)) + offset;
        }


        /// <summary>
        /// Triangle wave function.
        /// </summary>
        /// <param name="time">
        ///   The sample time.
        /// </param>
        /// <param name="period">
        ///   The wave frequency.
        /// </param>
        /// <param name="amplitude">
        ///   The amplitude.
        /// </param>
        /// <param name="phase">
        ///   The phase of the wave (in seconds) relative to the base function.
        /// </param>
        /// <param name="offset">
        ///   The value offset of the wave, relative to the base function.
        /// </param>
        /// <returns>
        ///   The square value.
        /// </returns>
        private static double TriangleWave(
            double time, 
            double period, 
            double amplitude,
            double phase,
            double offset
        ) {
            return (2 * amplitude / Math.PI) * Math.Asin(Math.Sin(2 * Math.PI / period * (time + phase))) + offset;
        }


        /// <summary>
        /// Rounds the specified timestamp down to the nearest interval. For example, if the 
        /// timestamp is <c>07:27:44</c> and the interval is <c>00:00:30</c>, the resulting time 
        /// will be <c>07:27:30</c>.
        /// </summary>
        /// <param name="timestamp">
        ///   The timestamp.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval to round down to.
        /// </param>
        /// <returns>
        ///   The rounded timestamp.
        /// </returns>
        private static DateTime RoundDownToNearestSampleTime(DateTime timestamp, TimeSpan sampleInterval) {
            var delta = timestamp.Ticks % sampleInterval.Ticks;
            return new DateTime(timestamp.Ticks - delta, timestamp.Kind);
        }


        /// <summary>
        /// Rounds the specified timestamp up to the nearest interval. For example, if the 
        /// timestamp is <c>07:27:44</c> and the interval is <c>00:00:30</c>, the resulting time 
        /// will be <c>07:28:00</c>.
        /// </summary>
        /// <param name="timestamp">
        ///   The timestamp.
        /// </param>
        /// <param name="sampleInterval">
        ///   The sample interval to round up to.
        /// </param>
        /// <returns>
        ///   The rounded timestamp.
        /// </returns>
        private static DateTime RoundUpToNearestSampleTime(DateTime timestamp, TimeSpan sampleInterval) {
            var modTicks = timestamp.Ticks % sampleInterval.Ticks;
            var delta = modTicks != 0 
                ? sampleInterval.Ticks - modTicks 
                : 0;
            return new DateTime(timestamp.Ticks + delta, timestamp.Kind);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<AdapterProperty>> GetTagProperties(IAdapterCallContext context, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            ValidateContext(context);
            ValidateRequest(request);

            return Task.FromResult(s_tagPropertyDefinitions.PublishToChannel());
        }


        /// <inheritdoc/>
        public Task<ChannelReader<TagDefinition>> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            ValidateContext(context);
            ValidateRequest(request);

            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => { 
                foreach (var item in request.Tags) {
                    if (!TryGetWaveGeneratorOptions(item, out var tagOptions)) {
                        continue;
                    }
                    await ch.WriteAsync(ToTagDefinition(tagOptions?.Name ?? item, tagOptions!, TagDefinitionFields.All), ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<TagDefinition>> FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            ValidateContext(context);
            ValidateRequest(request);

            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                IEnumerable<KeyValuePair<string, WaveGeneratorOptions>> selectedItems;

                if (string.IsNullOrEmpty(request.Name)) {
                    // No name filter; we will just select a page of results from the available definitions.
                    selectedItems = _tagDefinitions
                        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .SelectPage(request)
                        .ToArray();
                }
                else {
                    selectedItems = _tagDefinitions
                        .Where(x => x.Key.Like(request.Name!))
                        .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .SelectPage(request)
                        .ToArray();
                }

                foreach (var item in selectedItems) {
                    await ch.WriteAsync(ToTagDefinition(item.Key, item.Value, request.ResultFields), ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            ValidateContext(context);
            ValidateRequest(request);

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var sampleTime = RoundDownToNearestSampleTime(DateTime.UtcNow, GetSampleInterval());
                foreach (var tag in request.Tags) {
                    if (!TryGetWaveGeneratorOptions(tag, out var tagOptions)) {
                        continue;
                    }

                    var tagId = tagOptions?.Name ?? tag;

                    var val = TagValueBuilder
                        .Create()
                        .WithUtcSampleTime(sampleTime)
                        .WithValue(CalculateValue(sampleTime, tagOptions!))
                        .Build();

                    await ch.WriteAsync(new TagValueQueryResult(tagId, tagId, val), ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<TagValueQueryResult>> ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            ValidateContext(context);
            ValidateRequest(request);

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var sampleInterval = GetSampleInterval();
                var startTime = RoundDownToNearestSampleTime(request.UtcStartTime, sampleInterval);
                var endTime = RoundUpToNearestSampleTime(request.UtcEndTime, sampleInterval);

                foreach (var tag in request.Tags) {
                    if (!TryGetWaveGeneratorOptions(tag, out var tagOptions)) {
                        continue;
                    }

                    var tagId = tagOptions?.Name ?? tag;
                    var valuesEmittedForTag = 0;

                    for (var sampleTime = startTime; sampleTime <= endTime; sampleTime = sampleTime.Add(sampleInterval)) {
                        if (request.SampleCount > 0 && valuesEmittedForTag >= request.SampleCount) {
                            // No need to emit any more samples for this tag.
                            break;
                        }
                        if (sampleTime < request.UtcStartTime && request.BoundaryType != RawDataBoundaryType.Outside) {
                            // This is the sample immediately before the query start time.
                            continue;
                        }
                        if (sampleTime > request.UtcEndTime && request.BoundaryType != RawDataBoundaryType.Outside) {
                            // This is the sample immediately after the query start time.
                            continue;
                        }

                        var val = TagValueBuilder
                            .Create()
                            .WithUtcSampleTime(sampleTime)
                            .WithValue(CalculateValue(sampleTime, tagOptions!))
                            .Build();

                        await ch.WriteAsync(new TagValueQueryResult(tagId, tagId, val), ct).ConfigureAwait(false);

                        if (request.SampleCount > 0) {
                            ++valuesEmittedForTag;
                        }
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            ValidateContext(context);
            ValidateRequest(request);

            var result = ChannelExtensions.CreateTagValueChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (!TryGetWaveGeneratorOptions(tag, out var tagOptions)) {
                        continue;
                    }

                    var tagId = tagOptions?.Name ?? tag;

                    foreach (var sampleTime in request.UtcSampleTimes) {
                        var val = TagValueBuilder
                            .Create()
                            .WithUtcSampleTime(sampleTime)
                            .WithValue(CalculateValue(sampleTime, tagOptions!))
                            .Build();

                        await ch.WriteAsync(new TagValueQueryResult(tagId, tagId, val), ct).ConfigureAwait(false);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }
    }
}
