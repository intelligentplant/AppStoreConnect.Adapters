using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Proxy;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Proxy.Extensions {
    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
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
            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();

            using (var response = client.GetDescriptorAsync(new GetExtensionDescriptorRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                FeatureUri = featureUri?.ToString() ?? string.Empty
            }, Proxy.GetCallOptions(context, cancellationToken))) {
                var result = await response.ResponseAsync.ConfigureAwait(false);
                return result.ToAdapterFeatureDescriptor();
            }
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<Adapter.Extensions.ExtensionFeatureOperationDescriptor>> GetOperationsFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();

            using (var response = client.GetOperationsAsync(new GetExtensionOperationsRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                FeatureUri = featureUri?.ToString() ?? string.Empty
            }, Proxy.GetCallOptions(context, cancellationToken))) {
                var result = await response.ResponseAsync.ConfigureAwait(false);
                return result.Operations.Select(x => x.ToAdapterExtensionOperatorDescriptor()).ToArray();
            }
        }


        /// <inheritdoc/>
        protected override async Task<Adapter.Extensions.InvocationResponse> InvokeInternal(IAdapterCallContext context, Adapter.Extensions.InvocationRequest request, CancellationToken cancellationToken) {
            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var req = new InvokeExtensionRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                OperationId = request.OperationId?.ToString() ?? string.Empty
            };

            foreach (var item in request.Arguments) {
                req.Arguments.Add(item.ToGrpcVariant());
            }

            using (var response = client.InvokeExtensionAsync(req, Proxy.GetCallOptions(context, cancellationToken))) {
                var result = await response.ResponseAsync.ConfigureAwait(false);
                return new InvocationResponse() {
                    Results = result.Results.Select(x => x.ToAdapterVariant()).ToArray()
                };
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<InvocationResponse> StreamInternal(
            IAdapterCallContext context, 
            InvocationRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();

            var req = new InvokeExtensionRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                OperationId = request.OperationId?.ToString() ?? string.Empty
            };

            foreach (var item in request.Arguments) {
                req.Arguments.Add(item.ToGrpcVariant());
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var response = client.InvokeStreamingExtension(req, Proxy.GetCallOptions(context, ctSource.Token))) {
                while (await response.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    yield return new InvocationResponse() {
                        Results = response.ResponseStream.Current.Results.Select(x => x.ToAdapterVariant()).ToArray()
                    };
                }
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<InvocationResponse> DuplexStreamInternal(
            IAdapterCallContext context,
            DuplexStreamInvocationRequest request, 
            IAsyncEnumerable<InvocationStreamItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, channel);

            var result = Channel.CreateUnbounded<InvocationResponse>();

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var stream = client.InvokeDuplexStreamingExtension(Proxy.GetCallOptions(context, ctSource.Token))) {
                var req = new InvokeExtensionRequest() {
                    AdapterId = Proxy.RemoteDescriptor.Id,
                    OperationId = request.OperationId?.ToString() ?? string.Empty
                };

                await stream.RequestStream.WriteAsync(req).ConfigureAwait(false);

                Proxy.BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    try {
                        await foreach (var val in channel.WithCancellation(ct).ConfigureAwait(false)) {
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
                }, ctSource.Token);

                while (await stream.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    yield return new InvocationResponse() {
                        Results = stream.ResponseStream.Current.Results.Select(x => x.ToAdapterVariant()).ToArray()
                    };
                }
            }
        }

    }
}
