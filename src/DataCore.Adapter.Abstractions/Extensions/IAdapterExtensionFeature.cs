using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Interface that all non-standard adapter features must implement.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   Extension features are defined by creating an interface that extends 
    ///   <see cref="IAdapterExtensionFeature"/>, and is annotated with 
    ///   <see cref="AdapterFeatureAttribute"/>.
    /// </para>
    /// 
    /// </remarks>
    public interface IAdapterExtensionFeature : IAdapterFeature {

        /// <summary>
        /// The <see cref="IObjectEncoder"/> instances to use when encoding or decoding <see cref="EncodedObject"/> 
        /// instances.
        /// </summary>
        public IEnumerable<IObjectEncoder> Encoders { get; }


        /// <summary>
        /// Gets the descriptor for the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI that the descriptor is being requested for. This is used as a hint 
        ///   to the implementing type, in case the type implements multiple extension feature 
        ///   contracts.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The extension feature descriptor.
        /// </returns>
        /// <remarks>
        ///   If the implementing type implements multiple extension features, and <paramref name="featureUri"/> 
        ///   is <see langword="null"/>, the implementing type should return its first available 
        ///   descriptor.
        /// </remarks>
        Task<FeatureDescriptor?> GetDescriptor(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Gets the operations that are supported by the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI that the operations are being requested for. This is used as a hint 
        ///   to the implementing type, in case the type implements multiple extension feature 
        ///   contracts.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation descriptors.
        /// </returns>
        /// <remarks>
        ///   If the implementing type implements multiple extension features, and <paramref name="featureUri"/> 
        ///   is <see langword="null"/>, the implementing type should return operation descriptors 
        ///   for all of its implemented features.
        /// </remarks>
        Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes an extension method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        Task<InvocationResponse> Invoke(
            IAdapterCallContext context, 
            InvocationRequest request, 
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes a streaming extension method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will stream the results of the operation.
        /// </returns>
        IAsyncEnumerable<InvocationResponse> Stream(
            IAdapterCallContext context, 
            InvocationRequest request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes a bidirectional streaming extension method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <param name="channel">
        ///   An <see cref="IAsyncEnumerable{T}"/> that will stream the inputs for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        IAsyncEnumerable<InvocationResponse> DuplexStream(
            IAdapterCallContext context,
            DuplexStreamInvocationRequest request, 
            IAsyncEnumerable<InvocationStreamItem> channel, 
            CancellationToken cancellationToken
        );
    
    }

}
