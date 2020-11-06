using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData {
    /// <summary>
    /// <see cref="ITagConfigurationChanges"/> implementation.
    /// </summary>
    internal class TagConfigurationChangesImpl : ProxyAdapterFeature, ITagConfigurationChanges {

        /// <summary>
        /// Creates a new <see cref="TagConfigurationChangesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public TagConfigurationChangesImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.RealTimeData.TagConfigurationChange>> Subscribe(
            IAdapterCallContext context, 
            TagConfigurationChangesSubscriptionRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            GrpcAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateChannel<Adapter.RealTimeData.TagConfigurationChange>(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagConfigurationService.TagConfigurationServiceClient>();

                var grpcRequest = new CreateTagConfigurationChangePushChannelRequest() {
                    AdapterId = AdapterId,
                };

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

                        await result.Writer.WriteAsync(grpcChannel.ResponseStream.Current.ToAdapterTagConfigurationChange(), ct).ConfigureAwait(false);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
