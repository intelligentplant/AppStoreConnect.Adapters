using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// <see cref="AdapterExtensionFeature"/> provides a base implementation of <see cref="IAdapterExtensionFeature"/>. 
    /// Extend from this class when implementing an interface derived from <see cref="IAdapterExtensionFeature"/>.
    /// </summary>
    public abstract class AdapterExtensionFeature : IAdapterExtensionFeature {

        /// <summary>
        /// The <see cref="IValueEncoder"/> to use when encoding and decoding <see cref="EncodedValue"/> 
        /// instances.
        /// </summary>
        private readonly IValueEncoder _valueEncoder;


        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeature"/> object.
        /// </summary>
        /// <param name="valueEncoder">
        ///   The <see cref="IValueEncoder"/> to use when encoding and decoding 
        ///   <see cref="EncodedValue"/> instances.
        /// </param>
        protected AdapterExtensionFeature(IValueEncoder valueEncoder) {
            _valueEncoder = valueEncoder ?? throw new ArgumentNullException(nameof(valueEncoder));
        }


        /// <inheritdoc/>
        async Task<EncodedValue> IAdapterExtensionFeature.Invoke(IAdapterCallContext context, string methodName, EncodedValue request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (methodName == null) {
                throw new ArgumentNullException(nameof(methodName));
            }

            if (!await Authorize(context, methodName, cancellationToken).ConfigureAwait(false)) {
                throw new NotAuthorizedException();
            }

            return await Invoke(context, methodName, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async Task<ChannelReader<EncodedValue>> IAdapterExtensionFeature.InvokeServerStream(IAdapterCallContext context, string methodName, EncodedValue request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (methodName == null) {
                throw new ArgumentNullException(nameof(methodName));
            }

            if (!await Authorize(context, methodName, cancellationToken).ConfigureAwait(false)) {
                throw new NotAuthorizedException();
            }

            return await InvokeServerStream(context, methodName, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async Task<EncodedValue> IAdapterExtensionFeature.InvokeClientStream(IAdapterCallContext context, string methodName, ChannelReader<EncodedValue> channel, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (methodName == null) {
                throw new ArgumentNullException(nameof(methodName));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            if (!await Authorize(context, methodName, cancellationToken).ConfigureAwait(false)) {
                throw new NotAuthorizedException();
            }

            return await InvokeClientStream(context, methodName, channel, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async Task<ChannelReader<EncodedValue>> IAdapterExtensionFeature.InvokeBidirectionalStream(IAdapterCallContext context, string methodName, ChannelReader<EncodedValue> channel, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (methodName == null) {
                throw new ArgumentNullException(nameof(methodName));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            if (!await Authorize(context, methodName, cancellationToken).ConfigureAwait(false)) {
                throw new NotAuthorizedException();
            }

            return await InvokeBidirectionalStream(context, methodName, channel, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Encodes a value.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type. The type must be annotated with <see cref="ExtensionTypeAttribute"/>.
        /// </typeparam>
        /// <param name="value">
        ///   The value to encode.
        /// </param>
        /// <returns>
        ///   A new <see cref="EncodedValue"/> object.
        /// </returns>
        protected EncodedValue EncodeValue<T>(T value) {
            return _valueEncoder.Encode(value);
        }


        /// <summary>
        /// Decodes a value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to decode to. The type must be annotated with <see cref="ExtensionTypeAttribute"/>.
        /// </typeparam>
        /// <param name="value">
        ///   The value to decode.
        /// </param>
        /// <returns>
        ///   An instance of <typeparamref name="T"/>.
        /// </returns>
        protected T DecodeValue<T>(EncodedValue value) {
            return _valueEncoder.Decode<T>(value);
        }


        /// <summary>
        /// Authorizes a call to the specified method on the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="methodName">
        ///   The method name.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return <see langword="true"/> if the caller 
        ///   is authorized to call the method, or <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        ///   Override this method to customize authorization for calls to methods on the extension 
        ///   feature. The default behaviour is to authorize all callers.
        /// </remarks>
        protected virtual Task<bool> Authorize(IAdapterCallContext context, string methodName, CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }


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
        /// <remarks>
        ///   Override this method to intercept general unary method invocations and invoke the 
        ///   appropriate strongly-typed method on your derived class. The default behaviour is to 
        ///   throw a <see cref="MissingMethodException"/> for every invocation.
        /// </remarks>
        protected virtual Task<EncodedValue> Invoke(IAdapterCallContext context, string methodName, EncodedValue request, CancellationToken cancellationToken) {
            throw new MissingMethodException(GetType().FullName, methodName);
        }


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
        /// <remarks>
        ///   Override this method to intercept general server streaming method invocations and  
        ///   invoke the appropriate strongly-typed method on your derived class. The default 
        ///   behaviour is to throw a <see cref="MissingMethodException"/> for every invocation.
        /// </remarks>
        protected virtual Task<ChannelReader<EncodedValue>> InvokeServerStream(IAdapterCallContext context, string methodName, EncodedValue request, CancellationToken cancellationToken) {
            throw new MissingMethodException(GetType().FullName, methodName);
        }


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
        /// <remarks>
        ///   Override this method to intercept general client streaming method invocations and  
        ///   invoke the appropriate strongly-typed method on your derived class. The default 
        ///   behaviour is to throw a <see cref="MissingMethodException"/> for every invocation.
        /// </remarks>
        protected virtual Task<EncodedValue> InvokeClientStream(IAdapterCallContext context, string methodName, ChannelReader<EncodedValue> channel, CancellationToken cancellationToken) {
            throw new MissingMethodException(GetType().FullName, methodName);
        }


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
        /// <remarks>
        ///   Override this method to intercept general biderectional streaming method invocations
        ///   and invoke the appropriate strongly-typed method on your derived class. The default 
        ///   behaviour is to throw a <see cref="MissingMethodException"/> for every invocation.
        /// </remarks>
        protected virtual Task<ChannelReader<EncodedValue>> InvokeBidirectionalStream(IAdapterCallContext context, string methodName, ChannelReader<EncodedValue> channel, CancellationToken cancellationToken) {
            throw new MissingMethodException(GetType().FullName, methodName);
        }

    }
}
