using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;

using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="TagConfigurationService.TagConfigurationServiceBase"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Arguments are passed by gRPC framework")]
    public class TagConfigurationServiceImpl : TagConfigurationService.TagConfigurationServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagConfigurationServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public TagConfigurationServiceImpl(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task CreateConfigurationChangesPushChannel(CreateTagConfigurationChangePushChannelRequest request, IServerStreamWriter<TagConfigurationChange> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<RealTimeData.ITagConfigurationChanges>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var subscription = await adapter.Feature.Subscribe(
                adapterCallContext, 
                new RealTimeData.TagConfigurationChangesSubscriptionRequest() {
                    Properties = new Dictionary<string, string>(request.Properties)
                },
                cancellationToken
            ).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested) {
                try {
                    var msg = await subscription.ReadAsync(cancellationToken).ConfigureAwait(false);
                    await responseStream.WriteAsync(msg.ToGrpcTagConfigurationChange()).ConfigureAwait(false);
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
