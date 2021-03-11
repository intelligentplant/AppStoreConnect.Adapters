using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {
    public partial class AdapterExtensionFeature {

        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="handler">
        ///   The delegate for the extension operation.
        /// </param>
        /// <param name="name">
        ///   The name of the operation. The operation ID will be derived from the name. The name 
        ///   must be unique for the <see cref="ExtensionFeatureOperationType"/> on the <typeparamref name="TFeature"/>. 
        ///   That is, you can register different operations with the same name, as long as they 
        ///   are not both <see cref="ExtensionFeatureOperationType.Invoke"/> operations.
        /// </param>
        /// <param name="description">
        ///   The description for the operation.
        /// </param>
        /// <param name="inputParameters">
        ///   The descriptions for the expected entries in the <see cref="InvocationRequest.Arguments"/> 
        ///   passed to the operation.
        /// </param>
        /// <param name="outputParameters">
        ///   The descriptions for the entries in the <see cref="InvocationResponse.Results"/> 
        ///   returned from the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully registered, or <see langword="false"/> 
        ///   if another <see cref="ExtensionFeatureOperationType.Invoke"/> operation with the same 
        ///   name has already been registered for the <typeparamref name="TFeature"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   and annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </exception>
        protected bool BindInvoke<TFeature>(
            Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<InvocationResponse>> handler,
            string name,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null
        ) { 
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.Invoke, 
                name, 
                description, 
                inputParameters, 
                outputParameters
            );

            if (_boundDescriptors.ContainsKey(descriptor.OperationId) || _boundInvokeMethods.ContainsKey(descriptor.OperationId)) {
                return false;
            }

            _boundDescriptors[descriptor.OperationId] = descriptor;
            _boundInvokeMethods[descriptor.OperationId] = handler;

            return true;
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="handler">
        ///   The delegate for the extension operation.
        /// </param>
        /// <param name="name">
        ///   The name of the operation. The operation ID will be derived from the name. The name 
        ///   must be unique for the <see cref="ExtensionFeatureOperationType"/> on the <typeparamref name="TFeature"/>. 
        ///   That is, you can register different operations with the same name, as long as they 
        ///   are not both <see cref="ExtensionFeatureOperationType.Stream"/> operations.
        /// </param>
        /// <param name="description">
        ///   The description for the operation.
        /// </param>
        /// <param name="inputParameters">
        ///   The descriptions for the expected entries in the <see cref="InvocationRequest.Arguments"/> 
        ///   passed to the operation.
        /// </param>
        /// <param name="outputParameters">
        ///   The descriptions for the entries in each <see cref="InvocationResponse.Results"/> 
        ///   returned from the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully registered, or <see langword="false"/> 
        ///   if another <see cref="ExtensionFeatureOperationType.Stream"/> operation with the same 
        ///   name has already been registered for the <typeparamref name="TFeature"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   and annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </exception>
        protected bool BindStream<TFeature>(
            Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<ChannelReader<InvocationResponse>>> handler,
            string name,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.Stream,
                name,
                description,
                inputParameters,
                outputParameters
            );

            if (_boundDescriptors.ContainsKey(descriptor.OperationId) || _boundInvokeMethods.ContainsKey(descriptor.OperationId)) {
                return false;
            }

            _boundDescriptors[descriptor.OperationId] = descriptor;
            _boundStreamMethods[descriptor.OperationId] = handler;

            return true;
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.DuplexStream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="handler">
        ///   The delegate for the extension operation.
        /// </param>
        /// <param name="name">
        ///   The name of the operation. The operation ID will be derived from the name. The name 
        ///   must be unique for the <see cref="ExtensionFeatureOperationType"/> on the <typeparamref name="TFeature"/>. 
        ///   That is, you can register different operations with the same name, as long as they 
        ///   are not both <see cref="ExtensionFeatureOperationType.DuplexStream"/> operations.
        /// </param>
        /// <param name="description">
        ///   The description for the operation.
        /// </param>
        /// <param name="inputParameters">
        ///   The descriptions for the expected entries in each <see cref="InvocationRequest.Arguments"/> 
        ///   or <see cref="InvocationStreamItem.Arguments"/> passed to the operation.
        /// </param>
        /// <param name="outputParameters">
        ///   The descriptions for the entries in each <see cref="InvocationResponse.Results"/> 
        ///   returned from the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully registered, or <see langword="false"/> 
        ///   if another <see cref="ExtensionFeatureOperationType.DuplexStream"/> operation with the same 
        ///   name has already been registered for the <typeparamref name="TFeature"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   and annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </exception>
        protected bool BindDuplexStream<TFeature>(
            Func<IAdapterCallContext, InvocationRequest, ChannelReader<InvocationStreamItem>, CancellationToken, Task<ChannelReader<InvocationResponse>>> handler,
            string name,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.DuplexStream,
                name,
                description,
                inputParameters,
                outputParameters
            );

            if (_boundDescriptors.ContainsKey(descriptor.OperationId) || _boundInvokeMethods.ContainsKey(descriptor.OperationId)) {
                return false;
            }

            _boundDescriptors[descriptor.OperationId] = descriptor;
            _boundDuplexStreamMethods[descriptor.OperationId] = handler;

            return true;
        }

    }
}
