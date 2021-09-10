using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Json;
using DataCore.Adapter.Proxy;

using IntelligentPlant.BackgroundTasks;

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
        public AdapterExtensionFeatureImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        protected override async Task<Common.FeatureDescriptor?> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var response = client.GetDescriptorAsync(new GetExtensionDescriptorRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                FeatureUri = featureUri?.ToString() ?? string.Empty
            }, Proxy.GetCallOptions(context, ctSource.Token))) {
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
            Proxy.ValidateInvocation(context);

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var response = client.GetOperationsAsync(new GetExtensionOperationsRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                FeatureUri = featureUri?.ToString() ?? string.Empty
            }, Proxy.GetCallOptions(context, ctSource.Token))) {
                var result = await response.ResponseAsync.ConfigureAwait(false);
                return result.Operations.Select(x => x.ToAdapterExtensionOperatorDescriptor()).ToArray();
            }
        }


        /// <inheritdoc/>
        protected override async Task<Adapter.Extensions.InvocationResponse> InvokeCore(IAdapterCallContext context, Adapter.Extensions.InvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context);

            var client = Proxy.CreateClient<ExtensionFeaturesService.ExtensionFeaturesServiceClient>();
            var req = new InvokeExtensionRequest() {
                AdapterId = Proxy.RemoteDescriptor.Id,
                OperationId = request.OperationId?.ToString() ?? string.Empty
            };

            if (request.Arguments != null) {
                req.Arguments = Google.Protobuf.ByteString.CopyFrom(request.Arguments.Value.SerializeToUtf8Bytes());
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var response = client.InvokeExtensionAsync(req, Proxy.GetCallOptions(context, ctSource.Token))) {
                var result = await response.ResponseAsync.ConfigureAwait(false);
                return new InvocationResponse() {
                    Results = JsonElementExtensions.DeserializeJsonFromUtf8Bytes(result.Results.ToByteArray()) ?? default,
                    Status = result.StatusCode
                };
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<InvocationResponse> StreamCore(
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

            if (request.Arguments != null) {
                req.Arguments = Google.Protobuf.ByteString.CopyFrom(request.Arguments.Value.SerializeToUtf8Bytes());
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var response = client.InvokeStreamingExtension(req, Proxy.GetCallOptions(context, ctSource.Token))) {
                while (await response.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    yield return new InvocationResponse() {
                        Results = JsonElementExtensions.DeserializeJsonFromUtf8Bytes(response.ResponseStream.Current.Results.ToByteArray()) ?? default,
                        Status = response.ResponseStream.Current.StatusCode
                    };
                }
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<InvocationResponse> DuplexStreamCore(
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

                            if (val.Arguments != null) {
                                req.Arguments = Google.Protobuf.ByteString.CopyFrom(val.Arguments.Value.SerializeToUtf8Bytes());
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
                        Results = JsonElementExtensions.DeserializeJsonFromUtf8Bytes(stream.ResponseStream.Current.Results.ToByteArray()) ?? default,
                        Status = stream.ResponseStream.Current.StatusCode
                    };
                }
            }
        }

    }
}
