using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public Adapter(
            string id,
            string name,
            string description = null,
            IBackgroundTaskService backgroundTaskService = null,
            ILogger<Adapter> logger = null
        ) : base(id, new AdapterOptions() { Name = name, Description = description }, backgroundTaskService, logger) { }


        private static DateTime CalculateSampleTime(DateTime queryTime) {
            var offset = queryTime.Ticks % TimeSpan.TicksPerSecond;
            return queryTime.Subtract(TimeSpan.FromTicks(offset));
        }


        private static double SinusoidWave(DateTime sampleTime, TimeSpan offset, double period, double amplitude) {
            var time = (sampleTime - DateTime.UtcNow.Date.Add(offset)).TotalSeconds;
            return amplitude * (Math.Sin(2 * Math.PI * (1 / period) * time));
        }


        protected override Task StartAsync(CancellationToken cancellationToken) {
            AddProperty("Startup Time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return Task.CompletedTask;
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

            await Task.CompletedTask.ConfigureAwait(false);

            var sampleTime = CalculateSampleTime(DateTime.UtcNow);

            using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
                foreach (var tag in request.Tags) {
                    if (ctSource.Token.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }

                    yield return new TagValueQueryResult(
                        tag,
                        tag,
                        new TagValueBuilder()
                            .WithUtcSampleTime(sampleTime)
                            .WithValue(SinusoidWave(sampleTime, TimeSpan.Zero, 60, 1))
                            .Build()
                    );
                }
            }
        }

    }
}
