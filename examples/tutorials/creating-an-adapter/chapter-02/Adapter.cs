using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase, IReadSnapshotTagValues {

        public Adapter(
            string id,
            string name,
            string description = null,
            IBackgroundTaskService scheduler = null,
            ILogger<Adapter> logger = null
        ) : base(id, name, description, scheduler, logger) { }


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


        public Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(
            IAdapterCallContext context, 
            ReadSnapshotTagValuesRequest request, 
            CancellationToken cancellationToken
        ) {
            ValidateRequest(request);
            var result = Channel.CreateUnbounded<TagValueQueryResult>();
            var sampleTime = CalculateSampleTime(DateTime.UtcNow);

            TaskScheduler.QueueBackgroundChannelOperation((ch, ct) => {
                foreach (var tag in request.Tags) {
                    if (ct.IsCancellationRequested) {
                        break;
                    }
                    if (string.IsNullOrWhiteSpace(tag)) {
                        continue;
                    }

                    ch.TryWrite(new TagValueQueryResult(
                        tag,
                        tag,
                        TagValueBuilder
                            .Create()
                            .WithUtcSampleTime(sampleTime)
                            .WithValue(SinusoidWave(sampleTime, TimeSpan.Zero, 60, 1))
                            .Build()
                    ));
                }
            }, result.Writer, true, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
