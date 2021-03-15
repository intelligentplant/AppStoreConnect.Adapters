using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace MyAdapter {
    public class Adapter : AdapterBase {

        public Adapter(
            string id,
            string name,
            string description,
            IBackgroundTaskService backgroundTaskService = null,
            ILogger<Adapter> logger = null
        ) : base(id, new AdapterOptions() { Name = name, Description = description }, backgroundTaskService, logger) {
            AddExtensionFeatures(new PingPongExtension(BackgroundTaskService, DataCore.Adapter.Json.JsonObjectEncoder.Default));
        }

        protected override Task StartAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
