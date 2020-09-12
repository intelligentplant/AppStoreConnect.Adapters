using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
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
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="json">
        ///   The JSON-serialized operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The JSON-serialized result of the operation.
        /// </returns>
        Task<string> Invoke(
            IAdapterCallContext context, 
            Uri operationId, 
            string json, 
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes a streaming extension method.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="json">
        ///   The JSON-serialized operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the JSON-serialized results of the operation.
        /// </returns>
        Task<ChannelReader<string>> Stream(
            IAdapterCallContext context, 
            Uri operationId, 
            string json, 
            CancellationToken cancellationToken
        );


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
        ///   A channel that will stream the JSON-serialized inputs for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the JSON-serialized results of the operation.
        /// </returns>
        Task<ChannelReader<string>> DuplexStream(
            IAdapterCallContext context, 
            Uri operationId, 
            ChannelReader<string> channel, 
            CancellationToken cancellationToken
        );
    
    }

}
