using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {
    public partial class AdapterExtensionFeature {

        /// <summary>
        /// Generates an <see cref="ExtensionFeatureOperationDescriptorPartial"/> for an operation 
        /// binding.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type for the binding.
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
        ///   A new <see cref="ExtensionFeatureOperationDescriptorPartial"/> instance.
        /// </returns>
        private ExtensionFeatureOperationDescriptorPartial CreatePartialOperationDescriptor<TFeature>(
            Delegate handler,
            string? name,
            string? description,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters,
            System.Reflection.MethodInfo? descriptorProvider
        ) {
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

            return partialDescriptor;
        }

        #region [ Bind Invoke ]

        /// <summary>
        /// Binds an invoke operation.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="handler">
        ///   The operation handler.
        /// </param>
        /// <param name="partialDescriptor">
        ///   The partial operation descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully registered, or <see langword="false"/> 
        ///   if another <see cref="ExtensionFeatureOperationType.Invoke"/> operation with the same 
        ///   ID has already been registered.
        /// </returns>
        private bool BindInvoke<TFeature>(
            Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<InvocationResponse>> handler,
            ExtensionFeatureOperationDescriptorPartial partialDescriptor
        ) {
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

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler, 
                name, 
                description, 
                inputParameters, 
                outputParameters, 
                descriptorProvider
            );

            return BindInvoke<TFeature>(handler, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T">
        ///   The operation return type.
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
        /// <remarks>
        ///   The output type must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T>(
            Func<IAdapterCallContext, CancellationToken, Task<T?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var output = await handler.Invoke(ctx, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant [] { 
                        Variant.TryGetVariantType(typeof(T), out var _) 
                            ? Variant.FromValue(output) 
                            : this.ConvertToVariant(output) 
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T">
        ///   The operation return type.
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
        /// <remarks>
        ///   The output type must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T>(
            Func<IAdapterCallContext, T?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var output = handler.Invoke(ctx);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2>(
            Func<IAdapterCallContext, T1?, CancellationToken, Task<T2?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var output = await handler.Invoke(ctx, input1, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T2), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2>(
            Func<IAdapterCallContext, T1?, T2?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var output = handler.Invoke(ctx, input1);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T2), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3>(
            Func<IAdapterCallContext, T1?, T2?, CancellationToken, Task<T3?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var output = await handler.Invoke(ctx, input1, input2, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T3), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3>(
            Func<IAdapterCallContext, T1?, T2?, T3?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var output = handler.Invoke(ctx, input1, input2);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T3), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4>(
            Func<IAdapterCallContext, T1?, T2?, T3?, CancellationToken, Task<T4?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var output = await handler.Invoke(ctx, input1, input2, input3, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T4), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var output = handler.Invoke(ctx, input1, input2, input3);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T4), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, CancellationToken, Task<T5?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var output = await handler.Invoke(ctx, input1, input2, input3, input4, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T5), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var output = handler.Invoke(ctx, input1, input2, input3, input4);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T5), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, CancellationToken, Task<T6?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var output = await handler.Invoke(ctx, input1, input2, input3, input4, input5, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T6), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, T6?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var output = handler.Invoke(ctx, input1, input2, input3, input4, input5);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T6), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6, T7>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, T6?, CancellationToken, Task<T7?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var input6 = this.ConvertFromVariant<T6>(req.Arguments.ElementAtOrDefault(5));
                var output = await handler.Invoke(ctx, input1, input2, input3, input4, input5, input6, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T7), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6, T7>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, T6?, T7?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var input6 = this.ConvertFromVariant<T6>(req.Arguments.ElementAtOrDefault(5));
                var output = handler.Invoke(ctx, input1, input2, input3, input4, input5, input6);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T7), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6, T7, T8>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, T6?, T7?, CancellationToken, Task<T8?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var input6 = this.ConvertFromVariant<T6>(req.Arguments.ElementAtOrDefault(5));
                var input7 = this.ConvertFromVariant<T7>(req.Arguments.ElementAtOrDefault(6));
                var output = await handler.Invoke(ctx, input1, input2, input3, input4, input5, input6, input7, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T8), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6, T7, T8>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var input6 = this.ConvertFromVariant<T6>(req.Arguments.ElementAtOrDefault(5));
                var input7 = this.ConvertFromVariant<T7>(req.Arguments.ElementAtOrDefault(6));
                var output = handler.Invoke(ctx, input1, input2, input3, input4, input5, input6, input7);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T8), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The eighth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T9">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, CancellationToken, Task<T9?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var input6 = this.ConvertFromVariant<T6>(req.Arguments.ElementAtOrDefault(5));
                var input7 = this.ConvertFromVariant<T7>(req.Arguments.ElementAtOrDefault(6));
                var input8 = this.ConvertFromVariant<T8>(req.Arguments.ElementAtOrDefault(7));
                var output = await handler.Invoke(ctx, input1, input2, input3, input4, input5, input6, input7, input8, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T9), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                };
            }, partialDescriptor);
        }


        /// <summary>
        /// Binds a synchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The first operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The eighth operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T9">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindInvoke<TFeature, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            Func<IAdapterCallContext, T1?, T2?, T3?, T4?, T5?, T6?, T7?, T8?, T9?> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                var input2 = this.ConvertFromVariant<T2>(req.Arguments.ElementAtOrDefault(1));
                var input3 = this.ConvertFromVariant<T3>(req.Arguments.ElementAtOrDefault(2));
                var input4 = this.ConvertFromVariant<T4>(req.Arguments.ElementAtOrDefault(3));
                var input5 = this.ConvertFromVariant<T5>(req.Arguments.ElementAtOrDefault(4));
                var input6 = this.ConvertFromVariant<T6>(req.Arguments.ElementAtOrDefault(5));
                var input7 = this.ConvertFromVariant<T7>(req.Arguments.ElementAtOrDefault(6));
                var input8 = this.ConvertFromVariant<T8>(req.Arguments.ElementAtOrDefault(7));
                var output = handler.Invoke(ctx, input1, input2, input3, input4, input5, input6, input7, input8);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = new Variant[] {
                        Variant.TryGetVariantType(typeof(T9), out var _)
                            ? Variant.FromValue(output)
                            : this.ConvertToVariant(output)
                    }
                });
            }, partialDescriptor);
        }

        #endregion

        #region [ Bind Stream ]

        /// <summary>
        /// Binds a streaming operation.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="handler">
        ///   The operation handler.
        /// </param>
        /// <param name="partialDescriptor">
        ///   The partial operation descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully registered, or <see langword="false"/> 
        ///   if another <see cref="ExtensionFeatureOperationType.Stream"/> operation with the same 
        ///   ID has already been registered.
        /// </returns>
        private bool BindStream<TFeature>(
            Func<IAdapterCallContext, InvocationRequest, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            ExtensionFeatureOperationDescriptorPartial partialDescriptor
        ) {
            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.Stream,
                partialDescriptor.Name!,
                partialDescriptor.Description,
                partialDescriptor.Inputs,
                partialDescriptor.Outputs
            );

            if (_boundDescriptors.ContainsKey(descriptor.OperationId) || _boundStreamMethods.ContainsKey(descriptor.OperationId)) {
                return false;
            }

            _boundDescriptors[descriptor.OperationId] = descriptor;
            _boundStreamMethods[descriptor.OperationId] = handler;

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
            Func<IAdapterCallContext, InvocationRequest, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindStream<TFeature>(handler, partialDescriptor);
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindStream<TFeature, T>(
            Func<IAdapterCallContext, CancellationToken, IAsyncEnumerable<T?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                IAdapterCallContext ctx,
                InvocationRequest req,
                Func<IAdapterCallContext, CancellationToken, IAsyncEnumerable<T?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct    
            ) {
                await foreach (var item in handler.Invoke(ctx, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = new Variant[] {
                            Variant.TryGetVariantType(typeof(T), out var _)
                                ? Variant.FromValue(item)
                                : this.ConvertToVariant(item)
                        }
                    };
                }
            }

            return BindStream<TFeature>((ctx, req, ct) => RunHandler(ctx, req, handler, ct), partialDescriptor);
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindStream<TFeature, T1, T2>(
            Func<IAdapterCallContext, T1?, CancellationToken, IAsyncEnumerable<T2?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                IAdapterCallContext ctx,
                InvocationRequest req,
                Func<IAdapterCallContext, T1?, CancellationToken, IAsyncEnumerable<T2?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct
            ) {
                var input1 = this.ConvertFromVariant<T1>(req.Arguments.ElementAtOrDefault(0));
                await foreach (var item in handler.Invoke(ctx, input1, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = new Variant[] {
                            Variant.TryGetVariantType(typeof(T2), out var _)
                                ? Variant.FromValue(item)
                                : this.ConvertToVariant(item)
                        }
                    };
                }
            }

            return BindStream<TFeature>((ctx, req, ct) => RunHandler(ctx, req, handler, ct), partialDescriptor);
        }

        #endregion

        #region [ Bind Duplex Stream ]

        /// <summary>
        /// Binds a streaming operation.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="handler">
        ///   The operation handler.
        /// </param>
        /// <param name="partialDescriptor">
        ///   The partial operation descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully registered, or <see langword="false"/> 
        ///   if another <see cref="ExtensionFeatureOperationType.Stream"/> operation with the same 
        ///   ID has already been registered.
        /// </returns>
        private bool BindDuplexStream<TFeature>(
            Func<IAdapterCallContext, InvocationRequest, IAsyncEnumerable<InvocationStreamItem>, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            ExtensionFeatureOperationDescriptorPartial partialDescriptor
        ) {
            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.DuplexStream,
                partialDescriptor.Name!,
                partialDescriptor.Description,
                partialDescriptor.Inputs,
                partialDescriptor.Outputs
            );

            if (_boundDescriptors.ContainsKey(descriptor.OperationId) || _boundDuplexStreamMethods.ContainsKey(descriptor.OperationId)) {
                return false;
            }

            _boundDescriptors[descriptor.OperationId] = descriptor;
            _boundDuplexStreamMethods[descriptor.OperationId] = handler;

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
            Func<IAdapterCallContext, InvocationRequest, IAsyncEnumerable<InvocationStreamItem>, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            return BindDuplexStream<TFeature>(handler, partialDescriptor);
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.DuplexStream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="T1">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The operation return type.
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
        /// <remarks>
        ///   All input and output types must be pre-registered with <see cref="Common.TypeLibrary"/>, 
        ///   or must be annotated with <see cref="Common.DataTypeIdAttribute"/> or 
        ///   <see cref="ExtensionFeatureDataTypeAttribute"/>.
        /// </remarks>
        protected bool BindDuplexStream<TFeature, T1, T2>(
            Func<IAdapterCallContext, IAsyncEnumerable<T1?>, CancellationToken, IAsyncEnumerable<T2?>> handler,
            string? name = null,
            string? description = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters = null,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TFeature>(
                handler,
                name,
                description,
                inputParameters,
                outputParameters,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                IAdapterCallContext ctx,
                InvocationRequest req,
                IAsyncEnumerable<InvocationStreamItem> input,
                Func<IAdapterCallContext, IAsyncEnumerable<T1?>, CancellationToken, IAsyncEnumerable<T2?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct
            ) {
                var transformedInput = input.Transform(update => this.ConvertFromVariant<T1>(update?.Arguments?.ElementAtOrDefault(0) ?? Variant.Null), ct);
                await foreach (var item in handler.Invoke(ctx, transformedInput, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = new Variant[] {
                            Variant.TryGetVariantType(typeof(T2), out var _)
                                ? Variant.FromValue(item)
                                : this.ConvertToVariant(item)
                        }
                    };
                }
            }

            return BindDuplexStream<TFeature>((ctx, req, input, ct) => RunHandler(ctx, req, input, handler, ct));
        }

        #endregion

    }
}
