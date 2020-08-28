using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Provides a base implementation of <see cref="IAdapterExtensionFeature"/>. Extend from this 
    /// class when writing extension features and override the protected <see cref="Invoke"/>, 
    /// <see cref="Stream"/> and <see cref="DuplexStream"/> methods to allow calls to be 
    /// dynamically dispatched to your feature implementation.
    /// </summary>
    public abstract class AdapterExtensionFeature : IAdapterExtensionFeature {

        /// <inheritdoc/>
        Task<IEnumerable<ExtensionFeatureOperationDescriptor>> IAdapterExtensionFeature.GetOperations(
            IAdapterCallContext context, 
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            if (featureUri == null) {
                throw new ArgumentNullException(nameof(featureUri));
            }
            return GetOperations(context, featureUri, cancellationToken);
        }


        /// <inheritdoc/>
        Task<string> IAdapterExtensionFeature.Invoke(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }
            return Invoke(context, operationId, argument, cancellationToken);
        }


        /// <inheritdoc/>
        Task<ChannelReader<string>> IAdapterExtensionFeature.Stream(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }
            return Stream(context, operationId, argument, cancellationToken);
        }


        /// <inheritdoc/>
        Task<ChannelReader<string>> IAdapterExtensionFeature.DuplexStream(IAdapterCallContext context, Uri operationId, ChannelReader<string> channel, CancellationToken cancellationToken) {
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }
            return DuplexStream(context, operationId, channel, cancellationToken);
        }


        /// <summary>
        /// Gets the operations that are supported by the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI that operations are being requested for. If the implementing type 
        ///   implements multiple extension features, this can be used to identify which feature 
        ///   definition is being queried
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation descriptors.
        /// </returns>
        /// <remarks>
        ///   The <see cref="GetOperationUri"/> method can be used to generate URIs for extension 
        ///   feature operations.
        /// </remarks>
        protected abstract Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
            IAdapterCallContext context, 
            Uri featureUri,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes an extension method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will will return the result of the operation.
        /// </returns>
        /// <remarks>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every invocation.
        /// </remarks>
        protected virtual Task<string> Invoke(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            throw new MissingMethodException(operationId?.ToString());
        }


        /// <summary>
        /// Invokes a streaming extension method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will stream the results of the operation.
        /// </returns>
        /// <remarks>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every invocation.
        /// </remarks>
        protected virtual Task<ChannelReader<string>> Stream(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            throw new MissingMethodException(operationId?.ToString());
        }


        /// <summary>
        /// Invokes a bidirectional streaming extension method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="channel">
        ///   A channel that will stream the inputs for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will stream the results of the operation.
        /// </returns>
        /// <remarks>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every invocation.
        /// </remarks>
        protected virtual Task<ChannelReader<string>> DuplexStream(IAdapterCallContext context, Uri operationId, ChannelReader<string> channel, CancellationToken cancellationToken) {
            throw new MissingMethodException(operationId?.ToString());
        }


        /// <summary>
        /// Gets the operation URI for the specified unqualified extension operation name.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be an interface derived from 
        ///   <see cref="IAdapterExtensionFeature"/> that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="unqualifiedName">
        ///   The unqualified operation name.
        /// </param>
        /// <returns>
        ///   The operation URI.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not an interface derived from <see cref="IAdapterExtensionFeature"/> 
        ///   and annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="unqualifiedName"/> is <see langword="null"/> or white space.
        /// </exception>
        protected static Uri GetOperationUri<TFeature>(string unqualifiedName) where TFeature : IAdapterExtensionFeature {
            var featureType = typeof(TFeature);
            if (!featureType.IsExtensionAdapterFeature()) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_NotAnExtensionFeatureInterface, nameof(IAdapterExtensionFeature), nameof(AdapterFeatureAttribute)));
            }

            if (string.IsNullOrWhiteSpace(unqualifiedName)) {
                throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(unqualifiedName));
            }

            var featureUri = featureType.GetAdapterFeatureUri();
            return new Uri(
                featureUri, 
                unqualifiedName.EndsWith("/", StringComparison.Ordinal) 
                    ? unqualifiedName 
                    : unqualifiedName + "/"
            );
        }

    }
}
