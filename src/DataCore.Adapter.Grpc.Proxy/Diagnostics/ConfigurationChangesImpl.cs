using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData {
    /// <summary>
    /// <see cref="IConfigurationChanges"/> implementation.
    /// </summary>
    internal class ConfigurationChangesImpl : ProxyAdapterFeature, IConfigurationChanges {

        /// <summary>
        /// Creates a new <see cref="ConfigurationChangesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ConfigurationChangesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.Diagnostics.ConfigurationChange>> Subscribe(
            IAdapterCallContext context, 
            ConfigurationChangesSubscriptionRequest request, 
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var result = ChannelExtensions.CreateChannel<Adapter.Diagnostics.ConfigurationChange>(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<ConfigurationChangesService.ConfigurationChangesServiceClient>();

                var grpcRequest = new CreateConfigurationChangePushChannelRequest() {
                    AdapterId = AdapterId,
                };

                if (request.ItemTypes != null) {
                    grpcRequest.ItemTypes.AddRange(request.ItemTypes);
                }

                if (request.Properties != null) {
                    foreach (var item in request.Properties) {
                        grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                using (var grpcChannel = client.CreateConfigurationChangesPushChannel(
                   grpcRequest,
                   GetCallOptions(context, ct)
                )) {
                    // Read event messages.
                    while (await grpcChannel.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                        if (grpcChannel.ResponseStream.Current == null) {
                            continue;
                        }

                        await result.Writer.WriteAsync(grpcChannel.ResponseStream.Current.ToAdapterConfigurationChange(), ct).ConfigureAwait(false);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
