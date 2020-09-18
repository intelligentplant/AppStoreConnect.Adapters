using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter;
using DataCore.Adapter.Diagnostics;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase {

        public Adapter(
            string id,
            string name,
            string description = null,
            IBackgroundTaskService backgroundTaskService = null,
            ILogger<Adapter> logger = null
        ) : base(id, name, description, backgroundTaskService, logger) { }


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

    }
}
