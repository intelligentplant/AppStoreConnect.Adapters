using DataCore.Adapter;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Options;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MqttAdapter {
    [AdapterMetadata(
        "https://my-company.com/app-store-connect/adapters/mqtt/",
        Name = "MQTT",
        Description = "Adapter for connecting to an MQTT broker",
        HelpUrl = "https://my-company.com/app-store-connect/adapters/mqtt/help"
    )]
    public partial class MyAdapter : AdapterBase<MyAdapterOptions> {

        private static readonly AdapterProperty s_tagCreatedAtPropertyDefinition = new AdapterProperty("UTC Created At", DateTime.MinValue, "The UTC creation time for the tag");

        private static readonly System.Diagnostics.Metrics.Counter<int> s_messagesReceived = Telemetry.Meter.CreateCounter<int>("mqtt.messages_received", "{messages}", "Count of messages received from the MQTT broker");

        private readonly TagManager _tagManager;

        private readonly SnapshotTagValueManager _snapshotManager;

        private readonly ConfigurationChanges _configurationChanges;

        private readonly MqttFactory _factory = new MqttFactory();

        private IManagedMqttClient? _mqttClient;

        private readonly Nito.AsyncEx.AsyncLock _mqttInitLock = new Nito.AsyncEx.AsyncLock();


        public MyAdapter(
            string id,
            IOptionsMonitor<MyAdapterOptions> options,
            IBackgroundTaskService backgroundTaskService,
            ILogger<MyAdapter> logger
        ) : base(id, options, backgroundTaskService, logger) {
            _configurationChanges = new ConfigurationChanges(new ConfigurationChangesOptions() {
                Id = id
            }, BackgroundTaskService, Logger);

            AddFeatures(_configurationChanges);

            // In both _tagManager and _snapshotManager we are passing null for the IKeyValueStore
            // parameter. This means that tag definitions and snapshot values will not be persisted
            // between restarts of the host.

            _tagManager = new TagManager(
                null,
                BackgroundTaskService,
                new[] { s_tagCreatedAtPropertyDefinition },
                _configurationChanges.NotifyAsync
            );

            AddFeatures(_tagManager);

            _snapshotManager = new SnapshotTagValueManager(new SnapshotTagValueManagerOptions() {
                Id = id,
                TagResolver = SnapshotTagValueManager.CreateTagResolverFromAdapter(this)
            }, BackgroundTaskService, null, Logger);

            AddFeatures(_snapshotManager);
        }


        protected override async Task StartAsync(CancellationToken cancellationToken) {
            using var ctSource = CreateCancellationTokenSource(cancellationToken);

            await _tagManager.InitAsync(ctSource.Token).ConfigureAwait(false);
            await InitMqttClientAsync(ctSource.Token);
        }


        protected override async Task StopAsync(CancellationToken cancellationToken) {
            if (_mqttClient != null) {
                await _mqttClient.StopAsync().WithCancellation(cancellationToken);
            }
        }


        protected override void OnOptionsChange(MyAdapterOptions options) {
            base.OnOptionsChange(options);
            BackgroundTaskService.QueueBackgroundWorkItem(InitMqttClientAsync);
        }


        protected override async Task<IEnumerable<HealthCheckResult>> CheckHealthAsync(
            IAdapterCallContext context,
            CancellationToken cancellationToken
        ) {
            var result = new List<HealthCheckResult>();
            result.AddRange(await base.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false));

            if (!IsRunning) {
                return result;
            }

            if (_mqttClient?.IsConnected ?? false) {
                result.Add(HealthCheckResult.Healthy("MQTT Server", $"Connected to {Options.Hostname}"));
            }
            else {
                result.Add(HealthCheckResult.Unhealthy("MQTT Server", "Disconnected"));
            }

            return result;
        }


        public static IEnumerable<string> GetTopics(string delimitedTopics) {
            if (string.IsNullOrWhiteSpace(delimitedTopics)) {
                yield break;
            }

            foreach (var item in delimitedTopics.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                yield return item;
            }
        }


        private async Task InitMqttClientAsync(CancellationToken cancellationToken) {
            using var @lock = await _mqttInitLock.LockAsync(cancellationToken);

            _mqttClient?.Dispose();
            if (!Options.IsEnabled) {
                return;
            }

            if (_mqttClient == null) {
                Logger.LogWarning("Initialising MQTT client.");
            }
            else {
                Logger.LogWarning("Re-initialising MQTT client.");
            }

            _mqttClient = _factory.CreateManagedMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(Options.Hostname, Options.Port)
                .Build();

            var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(mqttClientOptions)
                .Build();

            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            await _mqttClient.StartAsync(managedMqttClientOptions).WithCancellation(cancellationToken);

            var topics = GetTopics(Options.Topics).Select(x => new MqttTopicFilter() {
                Topic = x,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce
            }).ToList();

            await _mqttClient.SubscribeAsync(topics).WithCancellation(cancellationToken);
        }


        private Task OnConnectedAsync(MqttClientConnectedEventArgs args) {
            Logger.LogWarning("Connected to MQTT server.");
            OnHealthStatusChanged();
            return Task.CompletedTask;
        }


        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args) {
            Logger.LogWarning("Disconnected from MQTT server.");
            OnHealthStatusChanged();
            return Task.CompletedTask;
        }


        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args) {
            s_messagesReceived.Add(1, new KeyValuePair<string, object?>("adapter_id", Descriptor.Id));

            var topic = args.ApplicationMessage.Topic;
            var payload = args.ApplicationMessage.ConvertPayloadToString();

            var dataValue = new TagValueBuilder()
                .WithUtcSampleTime(DateTime.UtcNow)
                .WithValue(double.TryParse(payload, out var numericValue) ? numericValue : payload)
                .Build();

            var tag = await _tagManager.GetTagAsync(topic, StopToken);
            if (tag == null) {
                tag = new TagDefinitionBuilder(topic)
                    .WithDataType(dataValue.Value.Type)
                    .WithSupportsReadSnapshotValues()
                    .WithSupportsSnapshotValuePush()
                    .WithProperty(s_tagCreatedAtPropertyDefinition.Name, dataValue.UtcSampleTime)
                    .Build();
                await _tagManager.AddOrUpdateTagAsync(tag, StopToken);
            }

            await _snapshotManager.ValueReceived(new TagValueQueryResult(tag.Id, tag.Name, dataValue), StopToken);
        }


        protected override async ValueTask DisposeAsyncCore() {
            await base.DisposeAsyncCore();
            if (_mqttClient != null) {
                await _mqttClient.StopAsync();
                _mqttClient.Dispose();
            }
        }


        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                _mqttClient?.Dispose();
            }
        }

    }
}
