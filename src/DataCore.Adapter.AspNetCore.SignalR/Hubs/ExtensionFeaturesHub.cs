using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public async IAsyncEnumerable<InvocationResponse> InvokeStreamingExtension(
            string adapterId,
            InvocationRequest request,
            [EnumeratorCancellation]
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

            using (Telemetry.ActivitySource.StartStreamActivity(resolved.Adapter.Descriptor.Id, request)) {
                long outputItems = 0;
                try {
                    await foreach (var item in resolved.Feature.Stream(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++outputItems;
                        yield return item;
                    }
                }
                finally {
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }
        }

#if NET48 == false

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
        public async IAsyncEnumerable<InvocationResponse> InvokeDuplexStreamingExtension(
            string adapterId,
            DuplexStreamInvocationRequest request,
            IAsyncEnumerable<InvocationStreamItem> channel,
            [EnumeratorCancellation]
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

            using (Telemetry.ActivitySource.StartDuplexStreamActivity(resolved.Adapter.Descriptor.Id, request)) {
                long outputItems = 0;
                try {
                    await foreach (var item in resolved.Feature.DuplexStream(adapterCallContext, request, channel, cancellationToken).ConfigureAwait(false)) {
                        ++outputItems;
                        yield return item;
                    }
                }
                finally {
                    Activity.Current.SetResponseItemCountTag(outputItems);
                }
            }
        }

#else

        /// <summary>
        /// Invokes a duplex streaming extension feature on an adapter for a single input and output.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request message.
        /// </param>
        /// <param name="streamItem">
        ///   The stream item.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        public async Task<InvocationResponse> InvokeDuplexStreamingExtension(
            string adapterId,
            DuplexStreamInvocationRequest request,
            InvocationStreamItem streamItem
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            ValidateObject(request);
            ValidateObject(streamItem);

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
                    inChannel.Writer.TryWrite(streamItem);
                    inChannel.Writer.TryComplete();

                    using (Telemetry.ActivitySource.StartDuplexStreamActivity(resolved.Adapter.Descriptor.Id, request)) {
                        var result = await resolved.Feature.DuplexStream(adapterCallContext, request, inChannel.Reader.ReadAllAsync(cancellationToken), cancellationToken).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                        Activity.Current.SetResponseItemCountTag(result == null ? 0 : 1);
                        return result!;
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
