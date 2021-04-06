using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Extensions;
using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for invoking extension adapter features.

    public partial class AdapterHub {

        /// <summary>
        /// Gets the descriptor for the specified extension feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="featureUri">
        ///   The extension feature URI to retrieve the descriptor for.
        /// </param>
        /// <returns>
        ///   The feature descriptor.
        /// </returns>
        public async Task<FeatureDescriptor> GetDescriptor(
            string adapterId,
            Uri featureUri
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);

            if (featureUri == null || !featureUri.IsAbsoluteUri) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, featureUri), nameof(featureUri));
            }

            featureUri = featureUri.EnsurePathHasTrailingSlash();

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            using (Telemetry.ActivitySource.StartGetDescriptorActivity(resolved.Adapter.Descriptor.Id, featureUri)) {
                return (await resolved.Feature.GetDescriptor(
                    adapterCallContext,
                    featureUri,
                    Context.ConnectionAborted
                ).ConfigureAwait(false))!;
            }
        }


        /// <summary>
        /// Gets the available operations for the specified extension feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="featureUri">
        ///   The extension feature URI to retrieve the operation descriptors for.
        /// </param>
        /// <returns>
        ///   The available operations.
        /// </returns>
        public async Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetExtensionOperations(
            string adapterId,
            Uri featureUri
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);

            if (featureUri == null || !featureUri.IsAbsoluteUri) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, featureUri), nameof(featureUri));
            }

            featureUri = featureUri.EnsurePathHasTrailingSlash();

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            using (Telemetry.ActivitySource.StartGetOperationsActivity(resolved.Adapter.Descriptor.Id, featureUri)) {
                var ops = await resolved.Feature.GetOperations(
                    adapterCallContext,
                    featureUri,
                    Context.ConnectionAborted
                ).ConfigureAwait(false);

                return ops?.Where(x => x != null)?.ToArray() ?? Array.Empty<ExtensionFeatureOperationDescriptor>();
            }
        }


        /// <summary>
        /// Invokes an extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        public async Task<InvocationResponse> InvokeExtension(
            string adapterId, 
            InvocationRequest request
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            ValidateObject(request);

            var operationId = request.OperationId.EnsurePathHasTrailingSlash();
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId, out var featureUri, out var error)) {
                throw new ArgumentException(error, nameof(operationId));
            }

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext, 
                adapterId, 
                featureUri, 
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            using (Telemetry.ActivitySource.StartInvokeActivity(resolved.Adapter.Descriptor.Id, request)) {
                return await resolved.Feature.Invoke(
                    adapterCallContext,
                    request,
                    Context.ConnectionAborted
                ).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Invokes a streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will stream the operation results back to the caller.
        /// </returns>
        public async Task<ChannelReader<InvocationResponse>> InvokeStreamingExtension(
            string adapterId,
            InvocationRequest request,
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            ValidateObject(request);

            var operationId = request.OperationId.EnsurePathHasTrailingSlash();
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId, out var featureUri, out var error)) {
                throw new ArgumentException(error, nameof(operationId));
            }

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateChannel<InvocationResponse>(DefaultChannelCapacity);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartStreamActivity(resolved.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await resolved.Feature.Stream(adapterCallContext, request, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

#if NETSTANDARD2_0 == false

        /// <summary>
        /// Invokes a duplex streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <param name="channel">
        ///   The input channel for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will stream the operation results back to the caller.
        /// </returns>
        public async Task<ChannelReader<InvocationResponse>> InvokeDuplexStreamingExtension(
            string adapterId,
            InvocationRequest request,
            ChannelReader<InvocationStreamItem> channel,
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            ValidateObject(request);

            var operationId = request.OperationId.EnsurePathHasTrailingSlash();
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId, out var featureUri, out var error)) {
                throw new ArgumentException(error, nameof(operationId));
            }

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateChannel<InvocationResponse>(DefaultChannelCapacity);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (Telemetry.ActivitySource.StartDuplexStreamActivity(resolved.Adapter.Descriptor.Id, request)) {
                    var resultChannel = await resolved.Feature.DuplexStream(adapterCallContext, request, channel, ct).ConfigureAwait(false);
                    var outputItems = await resultChannel.Forward(ch, ct).ConfigureAwait(false);
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

#else

        /// <summary>
        /// Invokes a duplex streaming extension feature on an adapter for a single input and output.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        public async Task<InvocationResponse> InvokeDuplexStreamingExtension(
            string adapterId,
            InvocationRequest request
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            ValidateObject(request);

            var operationId = request.OperationId.EnsurePathHasTrailingSlash();
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId, out var featureUri, out var error)) {
                throw new ArgumentException(error, nameof(operationId));
            }

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            using (var ctSource = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted)) {
                var cancellationToken = ctSource.Token;
                try {
                    var inChannel = Channel.CreateUnbounded<InvocationStreamItem>();
                    inChannel.Writer.TryComplete();

                    using (Telemetry.ActivitySource.StartDuplexStreamActivity(resolved.Adapter.Descriptor.Id, request)) {
                        var outChannel = await resolved.Feature.DuplexStream(adapterCallContext, request, inChannel, cancellationToken).ConfigureAwait(false);
                        return await outChannel.ReadAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally {
                    ctSource.Cancel();
                }
            }
        }

#endif

    }
}
