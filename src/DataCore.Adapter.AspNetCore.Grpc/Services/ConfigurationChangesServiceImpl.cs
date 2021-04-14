using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Diagnostics;

using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="ConfigurationChangesService.ConfigurationChangesServiceBase"/>.
    /// </summary>
    public class ConfigurationChangesServiceImpl : ConfigurationChangesService.ConfigurationChangesServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="ConfigurationChangesServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public ConfigurationChangesServiceImpl(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task CreateConfigurationChangesPushChannel(CreateConfigurationChangePushChannelRequest request, IServerStreamWriter<ConfigurationChange> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<Diagnostics.IConfigurationChanges>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new ConfigurationChangesSubscriptionRequest() {
                ItemTypes = request.ItemTypes,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartConfigurationChangesSubscribeActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                long outputItems = 0;

                await foreach (var msg in adapter.Feature.Subscribe(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                    try {
                        await responseStream.WriteAsync(msg.ToGrpcConfigurationChange()).ConfigureAwait(false);
                        activity.SetResponseItemCountTag(++outputItems);
                    }
                    catch (OperationCanceledException) {
                        // Do nothing
                    }
                    catch (System.Threading.Channels.ChannelClosedException) {
                        // Do nothing
                    }
                }
            }
        }

    }
}
