using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase, ITagSearch, IReadSnapshotTagValues, IReadRawTagValues {

        private readonly ConcurrentDictionary<string, TagDefinition> _tagsById = new ConcurrentDictionary<string, TagDefinition>();

        private readonly ConcurrentDictionary<string, TagDefinition> _tagsByName = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);


        public Adapter(
            string id,
            string name,
            string description = null,
            IBackgroundTaskService backgroundTaskService = null,
            ILogger<Adapter> logger = null
        ) : base(
            id,
            new AdapterOptions() { Name = name, Description = description },
            backgroundTaskService, 
            logger
        ) {
            AddFeatures(new PollingSnapshotTagValuePush(this, new PollingSnapshotTagValuePushOptions() {
                PollingInterval = TimeSpan.FromSeconds(1),
                TagResolver = SnapshotTagValuePush.CreateTagResolverFromAdapter(this)
            }, BackgroundTaskService, Logger));

            AddFeatures(ReadHistoricalTagValues.ForAdapter(this));
        }


        private AdapterProperty CreateWaveTypeProperty(string waveType) {
            return AdapterProperty.Create("Wave Type", waveType ?? "Sinusoid", "The wave type for the tag");
        }


        private void CreateTags() {
            var i = 0;
            foreach (var waveType in new[] { "Sinusoid", "Sawtooth", "Square", "Triangle" }) {
                ++i;
                var tagId = i.ToString();
                var tagName = string.Concat(waveType, "_Wave");

                var tag = TagDefinitionBuilder
                    .Create(tagId, tagName)
                    .WithDescription($"A tag that returns a {waveType.ToLower()} wave value")
                    .WithDataType(VariantType.Double)
                    .WithProperties(CreateWaveTypeProperty(waveType))
                    .Build();

                _tagsById[tag.Id] = tag;
                _tagsByName[tag.Name] = tag;
            }
        }


        private void DeleteTags() {
            _tagsById.Clear();
            _tagsByName.Clear();
        }


        private static DateTime CalculateSampleTime(DateTime queryTime) {
            var offset = queryTime.Ticks % TimeSpan.TicksPerSecond;
            return queryTime.Subtract(TimeSpan.FromTicks(offset));
        }


        private static double SinusoidWave(DateTime sampleTime, double period, double amplitude) {
            var time = (sampleTime - DateTime.UtcNow.Date).TotalSeconds;
            return amplitude * (Math.Sin(2 * Math.PI * (1 / period) * time));
        }


        private static double SawtoothWave(DateTime sampleTime, double period, double amplitude) {
            var time = (sampleTime - DateTime.UtcNow.Date).TotalSeconds;
            return (2 * amplitude / Math.PI) * Math.Atan(1 / Math.Tan(Math.PI / period * time));
        }


        private static double SquareWave(DateTime sampleTime, double period, double amplitude) {
            return Math.Sign(SinusoidWave(sampleTime, period, amplitude));
        }


        private static double TriangleWave(DateTime sampleTime, double period, double amplitude) {
            var time = (sampleTime - DateTime.UtcNow.Date).TotalSeconds;
            return (2 * amplitude / Math.PI) * Math.Asin(Math.Sin(2 * Math.PI / period * time));
        }


        private static TagValueQueryResult CalculateValueForTag(
            TagDefinition tag,
            DateTime utcSampleTime,
            TagValueStatus status = TagValueStatus.Good
        ) {
            var waveType = tag.Properties.FindProperty("Wave Type")?.Value.GetValueOrDefault("Sinusoid");
            double value;

            switch (waveType) {
                case "Sawtooth":
                    value = SawtoothWave(utcSampleTime, 60, 1);
                    break;
                case "Square":
                    value = SquareWave(utcSampleTime, 60, 1);
                    break;
                case "Triangle":
                    value = TriangleWave(utcSampleTime, 60, 1);
                    break;
                default:
                    value = SinusoidWave(utcSampleTime, 60, 1);
                    break;
            }

            return new TagValueQueryResult(
                tag.Id,
                tag.Name,
                TagValueBuilder
                    .Create()
                    .WithUtcSampleTime(utcSampleTime)
                    .WithValue(value)
                    .WithStatus(status)
                    .Build()
            );
        }


        protected override Task StartAsync(CancellationToken cancellationToken) {
            CreateTags();
            AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return Task.CompletedTask;
        }


        protected override Task StopAsync(CancellationToken cancellationToken) {
            DeleteTags();
            return Task.CompletedTask;
        }


        protected override Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(
            IAdapterCallContext context, 
            CancellationToken cancellationToken
        ) {
            return Task.FromResult<IEnumerable<HealthCheckResult>>(new[] {
                HealthCheckResult.Healthy("All systems normal!")
            });
        }


        public Task<ChannelReader<AdapterProperty>> GetTagProperties(
            IAdapterCallContext context,
            GetTagPropertiesRequest request,
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);

            var result = new[] {
                CreateWaveTypeProperty(null),
            }.OrderBy(x => x.Name).SelectPage(request).PublishToChannel();

            return Task.FromResult(result);
        }


        public Task<ChannelReader<TagDefinition>> GetTags(
            IAdapterCallContext context,
            GetTagsRequest request,
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagDefinition>();

            BackgroundTaskService.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }

                    if (_tagsById.TryGetValue(tag, out var t) || _tagsByName.TryGetValue(tag, out t)) {
                        result.Writer.TryWrite(t);
                    }
                }
            }, result.Writer, true, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<TagDefinition>> FindTags(
            IAdapterCallContext context,
            FindTagsRequest request,
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagDefinition>();

            BackgroundTaskService.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in _tagsById.Values.ApplyFilter(request)) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    ch.TryWrite(tag);
                }
            }, result.Writer, true, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(
            IAdapterCallContext context,
            ReadSnapshotTagValuesRequest request,
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagValueQueryResult>();
            var sampleTime = CalculateSampleTime(DateTime.UtcNow);

            BackgroundTaskService.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }
                    if (!_tagsById.TryGetValue(tag, out var t) && !_tagsByName.TryGetValue(tag, out t)) {
                        continue;
                    }

                    ch.TryWrite(CalculateValueForTag(t, sampleTime));
                }
            }, result.Writer, true, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<TagValueQueryResult>> ReadRawTagValues(
            IAdapterCallContext context, 
            ReadRawTagValuesRequest request, 
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagValueQueryResult>();

            BackgroundTaskService.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }
                    if (!_tagsById.TryGetValue(tag, out var t) && !_tagsByName.TryGetValue(tag, out t)) {
                        continue;
                    }

                    var sampleCount = 0;
                    var ts = CalculateSampleTime(request.UtcStartTime).AddSeconds(-1);
                    var rnd = new Random(ts.GetHashCode());

                    do {
                        ts = ts.AddSeconds(1);
                        if (request.BoundaryType == RawDataBoundaryType.Inside && (ts < request.UtcStartTime || ts > request.UtcEndTime)) {
                            continue;
                        }
                        ch.TryWrite(CalculateValueForTag(t, ts, rnd.NextDouble() < 0.9 ? TagValueStatus.Good : TagValueStatus.Bad));
                    } while (ts < request.UtcEndTime && (request.SampleCount < 1 || sampleCount <= request.SampleCount));
                }
                
            }, result.Writer, true, cancellationToken);

            return Task.FromResult(result.Reader);
        }
    }
}
