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
    public class AdapterExtensionFeatureImpl : ExtensionFeatureProxyBase<GrpcAdapterProxy, GrpcAdapterProxyOptions> {

        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeatureImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        public AdapterExtensionFeatureImpl(GrpcAdapterProxy proxy) : base(proxy, proxy.Encoders) { }


        /// <inheritdoc/>
        protected override async Task<Common.FeatureDescriptor?> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);

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
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var response = client.GetOperationsAsync(new GetExtensionOperationsRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                FeatureUri = featureUri?.ToString() ?? string.Empty
            }, Proxy.GetCallOptions(context, cancellationToken));

            var result = await response.ResponseAsync.ConfigureAwait(false);

            return result.Operations.Select(x => x.ToAdapterExtensionOperatorDescriptor()).ToArray();
        }


        /// <inheritdoc/>
        protected override async Task<Adapter.Extensions.InvocationResponse> InvokeInternal(IAdapterCallContext context, Adapter.Extensions.InvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context);

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var req = new InvokeExtensionRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                OperationId = request.OperationId?.ToString() ?? string.Empty
            };

            foreach (var item in request.Arguments) {
                req.Arguments.Add(item.ToGrpcVariant());
            }

            var response = client.InvokeExtensionAsync(req, Proxy.GetCallOptions(context, cancellationToken));

            var result = await response.ResponseAsync.ConfigureAwait(false);
            return new Adapter.Extensions.InvocationResponse() { 
                Results = result.Results.Select(x => x.ToAdapterVariant()).ToArray()
            };
        }


        /// <inheritdoc/>
        protected override Task<ChannelReader<Adapter.Extensions.InvocationResponse>> StreamInternal(IAdapterCallContext context, Adapter.Extensions.InvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context);

            var result = Channel.CreateUnbounded<Adapter.Extensions.InvocationResponse>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();

                var req = new InvokeExtensionRequest() {
                    AdapterId = Proxy.RemoteDescriptor.Id,
                    OperationId = request.OperationId?.ToString() ?? string.Empty
                };

                foreach (var item in request.Arguments) {
                    req.Arguments.Add(item.ToGrpcVariant());
                }

                var response = client.InvokeStreamingExtension(req, Proxy.GetCallOptions(context, cancellationToken));

                while (await response.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                    await result.Writer.WriteAsync(new Adapter.Extensions.InvocationResponse() { 
                        Results = response.ResponseStream.Current.Results.Select(x => x.ToAdapterVariant()).ToArray()
                    }, ct).ConfigureAwait(false);
                }
            }, true, Proxy.BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        protected override async Task<ChannelReader<Adapter.Extensions.InvocationResponse>> DuplexStreamInternal(IAdapterCallContext context, Adapter.Extensions.InvocationRequest request, ChannelReader<Adapter.Extensions.InvocationStreamItem> channel, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, channel);

            var result = Channel.CreateUnbounded<Adapter.Extensions.InvocationResponse>();

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var stream = client.InvokeDuplexStreamingExtension(Proxy.GetCallOptions(context, cancellationToken));

            var req = new InvokeExtensionRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                OperationId = request.OperationId?.ToString() ?? string.Empty
            };

            foreach (var item in request.Arguments) {
                req.Arguments.Add(item.ToGrpcVariant());
            }

            await stream.RequestStream.WriteAsync(req).ConfigureAwait(false);

            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (!cancellationToken.IsCancellationRequested) {
                        var val = await ch.ReadAsync(ct).ConfigureAwait(false);

                        var req = new InvokeExtensionRequest() {
                            AdapterId = Proxy.RemoteDescriptor.Id,
                            OperationId = request.OperationId?.ToString() ?? string.Empty
                        };

                        foreach (var item in val.Arguments) {
                            req.Arguments.Add(item.ToGrpcVariant());
                        }

                        await stream.RequestStream.WriteAsync(req).ConfigureAwait(false);
                    }
                }
                finally {
                    await stream.RequestStream.CompleteAsync().ConfigureAwait(false);
                }
            }, Proxy.BackgroundTaskService, cancellationToken);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (await stream.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                    await result.Writer.WriteAsync(new Adapter.Extensions.InvocationResponse() {
                        Results = stream.ResponseStream.Current.Results.Select(x => x.ToAdapterVariant()).ToArray()
                    }, ct).ConfigureAwait(false);
                }
            }, true, Proxy.BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }
}
