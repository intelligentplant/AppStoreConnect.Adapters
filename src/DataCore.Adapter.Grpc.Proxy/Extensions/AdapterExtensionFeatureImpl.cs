using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Proxy;

namespace DataCore.Adapter.Grpc.Proxy.Extensions {
    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    public class AdapterExtensionFeatureImpl : ExtensionFeatureProxyBase<GrpcAdapterProxy> {

        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeatureImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        public AdapterExtensionFeatureImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        protected override async Task<Common.FeatureDescriptor> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var response = client.GetDescriptorAsync(new GetExtensionDescriptorRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                FeatureUri = featureUri?.ToString() ?? string.Empty
            }, Proxy.GetCallOptions(context, cancellationToken));

            var result = await response.ResponseAsync.ConfigureAwait(false);

            return result.ToAdapterFeatureDescriptor();
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<Adapter.Extensions.ExtensionFeatureOperationDescriptor>> GetOperationsFromRemoteAdapter(
            IAdapterCallContext context,
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var response = client.GetOperationsAsync(new GetExtensionOperationsRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                FeatureUri = featureUri?.ToString() ?? string.Empty
            }, Proxy.GetCallOptions(context, cancellationToken));

            var result = await response.ResponseAsync.ConfigureAwait(false);

            return result.Operations.Select(x => x.ToAdapterExtensionOperatorDescriptor()).ToArray();
        }


        /// <inheritdoc/>
        protected override async Task<string> Invoke(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var response = client.InvokeExtensionAsync(new InvokeExtensionRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                OperationId = operationId?.ToString() ?? string.Empty,
                Argument = argument ?? string.Empty
            }, Proxy.GetCallOptions(context, cancellationToken));

            var result = await response.ResponseAsync.ConfigureAwait(false);
            return result.Result;
        }


        /// <inheritdoc/>
        protected override Task<ChannelReader<string>> Stream(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var result = Channel.CreateUnbounded<string>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
                var response = client.InvokeStreamingExtension(new InvokeExtensionRequest() {
                    AdapterId = Proxy.RemoteDescriptor.Id,
                    OperationId = operationId?.ToString() ?? string.Empty,
                    Argument = argument ?? string.Empty
                }, Proxy.GetCallOptions(context, cancellationToken));

                while (await response.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                    await result.Writer.WriteAsync(response.ResponseStream.Current.Result, ct).ConfigureAwait(false);
                }
            }, true, Proxy.BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        protected override Task<ChannelReader<string>> DuplexStream(IAdapterCallContext context, Uri operationId, ChannelReader<string> channel, CancellationToken cancellationToken) {
            var result = Channel.CreateUnbounded<string>();

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var stream = client.InvokeDuplexStreamingExtension(Proxy.GetCallOptions(context, cancellationToken));

            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        var val = await ch.ReadAsync(ct).ConfigureAwait(false);
                        await stream.RequestStream.WriteAsync(new InvokeExtensionRequest() {
                            AdapterId = Proxy.RemoteDescriptor.Id,
                            OperationId = operationId?.ToString() ?? string.Empty,
                            Argument = val
                        }).ConfigureAwait(false);
                    }
                }
                finally {
                    await stream.RequestStream.CompleteAsync().ConfigureAwait(false);
                }
            }, Proxy.BackgroundTaskService, cancellationToken);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (await stream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                    await result.Writer.WriteAsync(stream.ResponseStream.Current.Result, ct).ConfigureAwait(false);
                }
            }, true, Proxy.BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
