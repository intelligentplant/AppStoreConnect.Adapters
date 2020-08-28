using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
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
        ///   The result of the operation.
        /// </returns>
        Task<string> Invoke(
            IAdapterCallContext context, 
            Uri operationId, 
            string argument, 
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
        Task<ChannelReader<string>> Stream(
            IAdapterCallContext context, 
            Uri operationId, 
            string argument, 
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
        ///   A channel that will stream the inputs for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will stream the results of the operation.
        /// </returns>
        Task<ChannelReader<string>> DuplexStream(
            IAdapterCallContext context, 
            Uri operationId, 
            ChannelReader<string> channel, 
            CancellationToken cancellationToken
        );
    
    }

}
