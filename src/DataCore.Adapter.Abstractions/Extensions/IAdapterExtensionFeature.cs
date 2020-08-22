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
    ///   <see cref="IAdapterExtensionFeature"/> defines general-purpose methods for invoking 
    ///   extension feature operations. These are intended for use by clients that do not have 
    ///   a strongly-typed definition of the feature (e.g. to allow a caller to invoke an extension 
    ///   feature indirectly via an Industrial App Store API endpoint without requiring the 
    ///   endpoint to have a strongly-typed contract that defines the feature). 
    /// </para>
    ///   
    /// <para>
    ///   The <see cref="AdapterExtensionFeature"/> class provides a base implementation of 
    ///   <see cref="IAdapterExtensionFeature"/> that helps to simplify invocation of methods on 
    ///   your feature via the general purpose methods inherited from <see cref="IAdapterExtensionFeature"/>.
    ///   Implementers can use the <see cref="IValueEncoder"/> type to convert value to or from the 
    ///   <see cref="EncodedValue"/> type used in the methods defined in the 
    ///   <see cref="IAdapterExtensionFeature"/> interface.
    /// </para>
    /// 
    /// <para>
    ///   Note that conversion to or from <see cref="EncodedValue"/> requires that the type being 
    ///   converted is annotated with <see cref="ExtensionTypeAttribute"/>.    
    /// </para>
    ///   
    /// </remarks>
    /// <seealso cref="AdapterExtensionFeature"/>
    /// <seealso cref="EncodedValue"/>
    /// <seealso cref="IValueEncoder"/>
    public interface IAdapterExtensionFeature : IAdapterFeature {

        /// <summary>
        /// Invokes a unary method on the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="methodName">
        ///   The name of the method to invoke.
        /// </param>
        /// <param name="request">
        ///   The encoded request object.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return an <see cref="EncodedValue"/> 
        ///   representing the result of the operation.
        /// </returns>
        Task<EncodedValue> Invoke(
            IAdapterCallContext context, 
            string methodName, 
            EncodedValue request, 
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes a streaming method on the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="methodName">
        ///   The name of the method to invoke.
        /// </param>
        /// <param name="request">
        ///   The encoded request object.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a channel that streams <see cref="EncodedValue"/> 
        ///   objects back to the caller.
        /// </returns>
        Task<ChannelReader<EncodedValue>> InvokeServerStream(
            IAdapterCallContext context,
            string methodName,
            EncodedValue request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes a client-to-server streaming method on the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="methodName">
        ///   The name of the method to invoke.
        /// </param>
        /// <param name="channel">
        ///   A channel that will stream items to the extension feature.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return an <see cref="EncodedValue"/> 
        ///   representing the result of the operation.
        /// </returns>
        Task<EncodedValue> InvokeClientStream(
            IAdapterCallContext context,
            string methodName,
            ChannelReader<EncodedValue> channel,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes a bidirectional streaming method on the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="methodName">
        ///   The name of the method to invoke.
        /// </param>
        /// <param name="channel">
        ///   A channel that will stream items to the extension feature.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a channel that streams <see cref="EncodedValue"/> 
        ///   objects back to the caller.
        /// </returns>
        Task<ChannelReader<EncodedValue>> InvokeBidirectionalStream(
            IAdapterCallContext context,
            string methodName,
            ChannelReader<EncodedValue> channel,
            CancellationToken cancellationToken
        );

    }

}
