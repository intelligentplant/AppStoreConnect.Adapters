using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
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

            featureUri = UriExtensions.EnsurePathHasTrailingSlash(featureUri);

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            return (await resolved.Feature.GetDescriptor(
                adapterCallContext, 
                featureUri, 
                Context.ConnectionAborted
            ).ConfigureAwait(false))!;
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

            featureUri = UriExtensions.EnsurePathHasTrailingSlash(featureUri);

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            var ops = await resolved.Feature.GetOperations(
                adapterCallContext,
                featureUri,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            return ops?.Where(x => x != null)?.ToArray() ?? Array.Empty<ExtensionFeatureOperationDescriptor>();
        }


        /// <summary>
        /// Invokes an extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="operationId">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <param name="argument">
        ///   The argument for the operation.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        public async Task<string> InvokeExtension(
            string adapterId, 
            Uri operationId, 
            string argument
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            if (operationId == null || !operationId.IsAbsoluteUri) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, operationId), nameof(operationId));
            }

            operationId = UriExtensions.EnsurePathHasTrailingSlash(operationId);
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId, out var featureUri, out var error)) {
                throw new ArgumentException(error, nameof(operationId));
            }

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext, 
                adapterId, 
                featureUri, 
                Context.ConnectionAborted
            ).ConfigureAwait(false);

#pragma warning disable CS8603 // Possible null reference return.
            return await resolved.Feature.Invoke(
                adapterCallContext, 
                operationId, 
                argument, 
                Context.ConnectionAborted
            ).ConfigureAwait(false);
#pragma warning restore CS8603 // Possible null reference return.
        }


        /// <summary>
        /// Invokes a streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="operationId">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <param name="argument">
        ///   The argument for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will stream the operation results back to the caller.
        /// </returns>
        public async Task<ChannelReader<string>> InvokeStreamingExtension(
            string adapterId,
            Uri operationId,
            string argument,
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            if (operationId == null || !operationId.IsAbsoluteUri) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, operationId), nameof(operationId));
            }

            operationId = UriExtensions.EnsurePathHasTrailingSlash(operationId);
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId, out var featureUri, out var error)) {
                throw new ArgumentException(error, nameof(operationId));
            }

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                cancellationToken
            ).ConfigureAwait(false);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            return await resolved.Feature.Stream(
                adapterCallContext,
                operationId,
                argument,
                cancellationToken
            ).ConfigureAwait(false);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        }

#if NETSTANDARD2_0 == false

        /// <summary>
        /// Invokes a duplex streaming extension feature on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="operationId">
        ///   The URI of the operation to invoke.
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
        public async Task<ChannelReader<string>> InvokeDuplexStreamingExtension(
            string adapterId,
            Uri operationId,
            ChannelReader<string> channel,
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            if (operationId == null || !operationId.IsAbsoluteUri) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, operationId), nameof(operationId));
            }

            operationId = UriExtensions.EnsurePathHasTrailingSlash(operationId);
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId, out var featureUri, out var error)) {
                throw new ArgumentException(error, nameof(operationId));
            }

            var resolved = await ResolveAdapterAndExtensionFeature(
                adapterCallContext,
                adapterId,
                featureUri,
                cancellationToken
            ).ConfigureAwait(false);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            return await resolved.Feature.DuplexStream(
                adapterCallContext,
                operationId,
                channel!,
                cancellationToken
            ).ConfigureAwait(false);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        }

#else

        /// <summary>
        /// Invokes a duplex streaming extension feature on an adapter for a single input and output.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="operationId">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <param name="value">
        ///   The input value for the operation.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        public async Task<string> InvokeDuplexStreamingExtension(
            string adapterId,
            Uri operationId,
            string value
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            if (operationId == null || !operationId.IsAbsoluteUri) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, operationId), nameof(operationId));
            }

            operationId = UriExtensions.EnsurePathHasTrailingSlash(operationId);
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
                    var inChannel = Channel.CreateUnbounded<string>();
                    inChannel.Writer.TryWrite(value);
                    inChannel.Writer.TryComplete();

                    var outChannel = await resolved.Feature.DuplexStream(adapterCallContext, operationId, inChannel, cancellationToken).ConfigureAwait(false);
                    return await outChannel.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                finally {
                    ctSource.Cancel();
                }
            }
        }

#endif

    }
}
