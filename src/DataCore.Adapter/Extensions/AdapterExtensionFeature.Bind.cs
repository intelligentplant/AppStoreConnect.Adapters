using System;
using System.Collections.Generic;
using System.Linq;
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
        ///   The delegate for the extension operation. If the <paramref name="handler"/> is a 
        ///   method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/>, 
        ///   operation metadata will be obtained from the attribute.
        /// </param>
        /// <param name="name">
        ///   The name of the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="description">
        ///   The description for the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="inputParameters">
        ///   The descriptions for the expected entries in the <see cref="InvocationRequest.Arguments"/> 
        ///   passed to the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="outputParameters">
        ///   The descriptions for the entries in the <see cref="InvocationResponse.Results"/> 
        ///   returned from the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="descriptorProvider">
        ///   A method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   that can be used to supply metadata for the operation registration. Specifying a 
        ///   value for this parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> parameter.
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
        ///   A name for the operation cannot be inferred from the <paramref name="handler"/>, 
        ///   <paramref name="name"/> or <paramref name="descriptorProvider"/> parameters.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   and annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </exception>
        protected bool BindInvoke<TFeature>(
            Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<InvocationResponse>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) { 
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = ExtensionFeatureOperationAttribute.CreateDescriptor(descriptorProvider)
                ?? ExtensionFeatureOperationAttribute.CreateDescriptor(handler.Method)
                ?? new ExtensionFeatureOperationDescriptorPartial();

            if (!string.IsNullOrWhiteSpace(name)) {
                partialDescriptor.Name = name;
            }

            if (!string.IsNullOrWhiteSpace(description)) {
                partialDescriptor.Description = description;
            }

            if (inputParameters != null) {
                partialDescriptor.Inputs = inputParameters.ToArray();
            }

            if (outputParameters != null) {
                partialDescriptor.Outputs = outputParameters.ToArray();
            }

            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.Invoke, 
                partialDescriptor.Name!, 
                partialDescriptor.Description, 
                partialDescriptor.Inputs, 
                partialDescriptor.Outputs
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
        ///   The delegate for the extension operation. If the <paramref name="handler"/> is a 
        ///   method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/>, 
        ///   operation metadata will be obtained from the attribute.
        /// </param>
        /// <param name="name">
        ///   The name of the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="description">
        ///   The description for the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="inputParameters">
        ///   The descriptions for the expected entries in the <see cref="InvocationRequest.Arguments"/> 
        ///   passed to the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="outputParameters">
        ///   The descriptions for the entries in the <see cref="InvocationResponse.Results"/> 
        ///   returned from the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="descriptorProvider">
        ///   A method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   that can be used to supply metadata for the operation registration. Specifying a 
        ///   value for this parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> parameter.
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
        ///   A name for the operation cannot be inferred from the <paramref name="handler"/>, 
        ///   <paramref name="name"/> or <paramref name="descriptorProvider"/> parameters.
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
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = ExtensionFeatureOperationAttribute.CreateDescriptor(descriptorProvider)
                ?? ExtensionFeatureOperationAttribute.CreateDescriptor(handler.Method)
                ?? new ExtensionFeatureOperationDescriptorPartial();

            if (!string.IsNullOrWhiteSpace(name)) {
                partialDescriptor.Name = name;
            }

            if (!string.IsNullOrWhiteSpace(description)) {
                partialDescriptor.Description = description;
            }

            if (inputParameters != null) {
                partialDescriptor.Inputs = inputParameters.ToArray();
            }

            if (outputParameters != null) {
                partialDescriptor.Outputs = outputParameters.ToArray();
            }

            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.Stream,
                partialDescriptor.Name!,
                partialDescriptor.Description,
                partialDescriptor.Inputs,
                partialDescriptor.Outputs
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
        ///   The delegate for the extension operation. If the <paramref name="handler"/> is a 
        ///   method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/>, 
        ///   operation metadata will be obtained from the attribute.
        /// </param>
        /// <param name="name">
        ///   The name of the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="description">
        ///   The description for the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="inputParameters">
        ///   The descriptions for the expected entries in the <see cref="InvocationRequest.Arguments"/> 
        ///   passed to the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="outputParameters">
        ///   The descriptions for the entries in the <see cref="InvocationResponse.Results"/> 
        ///   returned from the operation. Specifying a value for this parameter overrides metadata 
        ///   obtained from an <see cref="ExtensionFeatureOperationAttribute"/> annotation on the 
        ///   <paramref name="handler"/> or <paramref name="descriptorProvider"/> parameters.
        /// </param>
        /// <param name="descriptorProvider">
        ///   A method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   that can be used to supply metadata for the operation registration. Specifying a 
        ///   value for this parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully registered, or <see langword="false"/> 
        ///   if another <see cref="ExtensionFeatureOperationType.DuplexStream"/> operation with 
        ///   the same name has already been registered for the <typeparamref name="TFeature"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   A name for the operation cannot be inferred from the <paramref name="handler"/>, 
        ///   <paramref name="name"/> or <paramref name="descriptorProvider"/> parameters.
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
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = ExtensionFeatureOperationAttribute.CreateDescriptor(descriptorProvider)
                ?? ExtensionFeatureOperationAttribute.CreateDescriptor(handler.Method)
                ?? new ExtensionFeatureOperationDescriptorPartial();

            if (!string.IsNullOrWhiteSpace(name)) {
                partialDescriptor.Name = name;
            }

            if (!string.IsNullOrWhiteSpace(description)) {
                partialDescriptor.Description = description;
            }

            if (inputParameters != null) {
                partialDescriptor.Inputs = inputParameters.ToArray();
            }

            if (outputParameters != null) {
                partialDescriptor.Outputs = outputParameters.ToArray();
            }

            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.DuplexStream,
                partialDescriptor.Name!,
                partialDescriptor.Description,
                partialDescriptor.Inputs,
                partialDescriptor.Outputs
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
