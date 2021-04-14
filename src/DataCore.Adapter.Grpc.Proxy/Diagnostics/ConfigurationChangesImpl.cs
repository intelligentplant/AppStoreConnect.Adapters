using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
        public async IAsyncEnumerable<Adapter.Diagnostics.ConfigurationChange> Subscribe(
            IAdapterCallContext context, 
            ConfigurationChangesSubscriptionRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

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

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcChannel = client.CreateConfigurationChangesPushChannel(
               grpcRequest,
               GetCallOptions(context, ctSource.Token)
            )) {
                // Read event messages.
                while (await grpcChannel.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcChannel.ResponseStream.Current == null) {
                        continue;
                    }

                    yield return grpcChannel.ResponseStream.Current.ToAdapterConfigurationChange();
                }
            }
        }

    }
}
