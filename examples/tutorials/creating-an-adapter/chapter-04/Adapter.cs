using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase, IReadSnapshotTagValues {

        private readonly TagManager _tagManager;


        public Adapter(
            string id,
            string name,
            string description = null,
            IBackgroundTaskService backgroundTaskService = null,
            ILogger<Adapter> logger = null
        ) : base(
            id,
            name,
            description,
            backgroundTaskService, 
            logger
        ) {
            _tagManager = new TagManager(
                backgroundTaskService: BackgroundTaskService,
                tagPropertyDefinitions: new[] { CreateWaveTypeProperty(null) }
            );

            AddFeatures(_tagManager);

            AddFeatures(new PollingSnapshotTagValuePush(this, new PollingSnapshotTagValuePushOptions() {
                AdapterId = id,
                PollingInterval = TimeSpan.FromSeconds(1),
                TagResolver = SnapshotTagValuePush.CreateTagResolverFromAdapter(this)
            }, BackgroundTaskService, Logger));
        }


        private AdapterProperty CreateWaveTypeProperty(string waveType) {
            return AdapterProperty.Create("Wave Type", waveType ?? "Sinusoid", "The wave type for the tag");
        }


        private async Task CreateTagsAsync(CancellationToken cancellationToken) {
            var i = 0;
            foreach (var waveType in new[] { "Sinusoid", "Sawtooth", "Square", "Triangle" }) {
                ++i;
                var tagId = i.ToString();
                var tagName = string.Concat(waveType, "_Wave");

                var tag = new TagDefinitionBuilder(tagId, tagName)
                    .WithDescription($"A tag that returns a {waveType.ToLower()} wave value")
                    .WithDataType(VariantType.Double)
                    .WithProperties(CreateWaveTypeProperty(waveType))
                    .Build();

                await _tagManager.AddOrUpdateTagAsync(tag, cancellationToken).ConfigureAwait(false);
            }
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
            TagValueStatus status
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
                new TagValueBuilder()
                    .WithUtcSampleTime(utcSampleTime)
                    .WithValue(value)
                    .WithStatus(status)
                    .Build()
            );
        }


        protected override async Task StartAsync(CancellationToken cancellationToken) {
            await CreateTagsAsync(cancellationToken).ConfigureAwait(false);
            AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }


        protected override Task StopAsync(CancellationToken cancellationToken) {
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


        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
            IAdapterCallContext context,
            ReadSnapshotTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            ValidateInvocation(context, request);

            var sampleTime = CalculateSampleTime(DateTime.UtcNow);
            var rnd = new Random(sampleTime.GetHashCode());

            using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
                foreach (var tag in request.Tags) {
                    if (ctSource.Token.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }
                    var tagDef = await _tagManager.GetTagAsync(tag, ctSource.Token).ConfigureAwait(false);
                    if (tagDef == null) {
                        continue;
                    }

                    yield return CalculateValueForTag(tagDef, sampleTime, rnd.NextDouble() < 0.9 ? TagValueStatus.Good : TagValueStatus.Bad);
                }
            }
        }
    }
}
