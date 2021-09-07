using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace DataCore.Adapter.Extensions {
    public partial class AdapterExtensionFeature {

        #region [ Create Operation Descriptors ]

        /// <summary>
        /// Generates an <see cref="ExtensionFeatureOperationDescriptorPartial"/> for an operation 
        /// binding.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The input parameter type for the binding.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response type for the binding.
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        private ExtensionFeatureOperationDescriptorPartial CreatePartialOperationDescriptor<TRequest, TResponse>(
            Delegate handler,
            string? name,
            string? description,
            JsonElement? requestSchema,
            JsonElement? responseSchema,
            System.Reflection.MethodInfo? descriptorProvider
        ) {
            return CreatePartialOperationDescriptor(
                handler, 
                descriptorProvider,
                partialDescriptor => {
                    if (!string.IsNullOrWhiteSpace(name)) {
                        // Override operation name.
                        partialDescriptor.Name = name;
                    }

                    if (!string.IsNullOrWhiteSpace(description)) {
                        // Override operation description.
                        partialDescriptor.Description = description;
                    }

                    if (requestSchema != null) {
                        // Override request schema.
                        partialDescriptor.RequestSchema = requestSchema.Value;
                    }
                    else if (partialDescriptor.RequestSchema == null) {
                        // No request schema has been defined, but we known the request type, so
                        // we will automatically generate the schema.
                        partialDescriptor.RequestSchema = GetSerializedJsonSchema<TRequest>(JsonOptions);
                    }

                    if (responseSchema != null) {
                        // Override response schema.
                        partialDescriptor.ResponseSchema = responseSchema.Value;
                    }
                    else if (partialDescriptor.ResponseSchema == null) {
                        // No response schema has been defined, but we known the response type, so
                        // we will automatically generate the schema.
                        partialDescriptor.ResponseSchema = GetSerializedJsonSchema<TResponse>(JsonOptions);
                    }
                }
            );
        }


        /// <summary>
        /// Generates an <see cref="ExtensionFeatureOperationDescriptorPartial"/> for an operation 
        /// binding.
        /// </summary>
        /// <typeparam name="TResponse">
        ///   The response type for the binding.
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
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        private ExtensionFeatureOperationDescriptorPartial CreatePartialOperationDescriptor<TResponse>(
            Delegate handler,
            string? name,
            string? description,
            JsonElement? responseSchema,
            System.Reflection.MethodInfo? descriptorProvider
        ) {
            return CreatePartialOperationDescriptor(
                handler,
                descriptorProvider,
                partialDescriptor => {
                    if (!string.IsNullOrWhiteSpace(name)) {
                        // Override operation name.
                        partialDescriptor.Name = name;
                    }

                    if (!string.IsNullOrWhiteSpace(description)) {
                        // Override operation description.
                        partialDescriptor.Description = description;
                    }

                    if (responseSchema != null) {
                        // Override response schema.
                        partialDescriptor.ResponseSchema = responseSchema.Value;
                    }
                    else if (partialDescriptor.ResponseSchema == null) {
                        // No response schema has been defined, but we known the response type, so
                        // we will automatically generate the schema.
                        partialDescriptor.ResponseSchema = GetSerializedJsonSchema<TResponse>(JsonOptions);
                    }
                }
            );
        }


        /// <summary>
        /// Generates an <see cref="ExtensionFeatureOperationDescriptorPartial"/> for an operation 
        /// binding.
        /// </summary>
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
        private ExtensionFeatureOperationDescriptorPartial CreatePartialOperationDescriptor(
            Delegate handler,
            string? name,
            string? description,
            JsonElement? requestSchema,
            JsonElement? responseSchema,
            System.Reflection.MethodInfo? descriptorProvider
        ) {
            return CreatePartialOperationDescriptor(
                handler,
                descriptorProvider,
                partialDescriptor => {
                    if (!string.IsNullOrWhiteSpace(name)) {
                        // Override operation name.
                        partialDescriptor.Name = name;
                    }

                    if (!string.IsNullOrWhiteSpace(description)) {
                        // Override operation description.
                        partialDescriptor.Description = description;
                    }

                    if (requestSchema != null) {
                        // Override request schema.
                        partialDescriptor.RequestSchema = requestSchema.Value;
                    }

                    if (responseSchema != null) {
                        // Override response schema.
                        partialDescriptor.ResponseSchema = responseSchema.Value;
                    }
                }
            );
        }


        /// <summary>
        /// Generates an <see cref="ExtensionFeatureOperationDescriptorPartial"/> for an operation 
        /// binding.
        /// </summary>
        /// <param name="handler">
        ///   The delegate for the extension operation. If the <paramref name="handler"/> is a 
        ///   method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/>, 
        ///   operation metadata will be obtained from the attribute.
        /// </param>
        /// <param name="descriptorProvider">
        ///   A method that is annotated with an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   that can be used to supply metadata for the operation registration. Specifying a 
        ///   value for this parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> parameter.
        /// </param>
        /// <param name="configure">
        ///   A delegate that can be used to modify the <see cref="ExtensionFeatureOperationDescriptorPartial"/> 
        ///   before it is returned.
        /// </param>
        /// <returns>
        ///   A new <see cref="ExtensionFeatureOperationDescriptorPartial"/> instance.
        /// </returns>
        private ExtensionFeatureOperationDescriptorPartial CreatePartialOperationDescriptor(
            Delegate handler,
            System.Reflection.MethodInfo? descriptorProvider,
            Action<ExtensionFeatureOperationDescriptorPartial>? configure
        ) {
            var partialDescriptor = ExtensionFeatureOperationAttribute.CreateDescriptor(descriptorProvider)
                ?? ExtensionFeatureOperationAttribute.CreateDescriptor(handler.Method)
                ?? new ExtensionFeatureOperationDescriptorPartial();

            configure?.Invoke(partialDescriptor);

            return partialDescriptor;
        }

        #endregion

        #region [ Bind Invoke (with IAdapterCallContext) ]

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
                partialDescriptor.RequestSchema,
                partialDescriptor.ResponseSchema
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) { 
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler, 
                name, 
                description, 
                requestSchema, 
                responseSchema, 
                descriptorProvider
            );

            return BindInvoke<TFeature>(handler, partialDescriptor);
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            Func<IAdapterCallContext, InvocationRequest, InvocationResponse> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => Task.FromResult(handler(ctx, req)), partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TResponse>(
            Func<IAdapterCallContext, CancellationToken, Task<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TResponse>(
                handler,
                name,
                description,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var output = await handler.Invoke(ctx, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
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
        /// <typeparam name="TResponse">
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
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TResponse>(
            Func<IAdapterCallContext, TResponse?> handler,
            string? name = null,
            string? description = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TResponse>(
                handler,
                name,
                description,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var output = handler.Invoke(ctx);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
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
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TRequest, TResponse>(
            Func<IAdapterCallContext, TRequest?, CancellationToken, Task<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input = req.Arguments == null 
                    ? default 
                    : DeserializeFromJsonElement<TRequest>(req.Arguments.Value, JsonOptions);
                var output = await handler.Invoke(ctx, input, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
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
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TRequest, TResponse>(
            Func<IAdapterCallContext, TRequest?, TResponse?> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input = req.Arguments == null
                    ? default
                    : DeserializeFromJsonElement<TRequest>(req.Arguments.Value, JsonOptions);
                var output = handler.Invoke(ctx, input);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
                });
            }, partialDescriptor);
        }

        #endregion

        #region [ Bind Invoke (without IAdapterCallContext) ]

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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            Func<InvocationRequest, CancellationToken, Task<InvocationResponse>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => handler(req, ct), partialDescriptor);
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            Func<InvocationRequest, InvocationResponse> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => Task.FromResult(handler(req)), partialDescriptor);
        }


        /// <summary>
        /// Binds an asynchronous extension operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TResponse>(
            Func<CancellationToken, Task<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TResponse>(
                handler,
                name,
                description,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var output = await handler.Invoke(ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
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
        /// <typeparam name="TResponse">
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
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TResponse>(
            Func<TResponse?> handler,
            string? name = null,
            string? description = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TResponse>(
                handler,
                name,
                description,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var output = handler.Invoke();

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
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
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TRequest, TResponse>(
            Func<TRequest?, CancellationToken, Task<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>(async (ctx, req, ct) => {
                var input = req.Arguments == null
                    ? default
                    : DeserializeFromJsonElement<TRequest>(req.Arguments.Value, JsonOptions);
                var output = await handler.Invoke(input, ct).ConfigureAwait(false);

                if (output is InvocationResponse ir) {
                    return ir;
                }

                return new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
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
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindInvoke<TFeature, TRequest, TResponse>(
            Func<TRequest?, TResponse?> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindInvoke<TFeature>((ctx, req, ct) => {
                var input = req.Arguments == null
                    ? default
                    : DeserializeFromJsonElement<TRequest>(req.Arguments.Value, JsonOptions);
                var output = handler.Invoke(input);

                if (output is InvocationResponse ir) {
                    return Task.FromResult(ir);
                }

                return Task.FromResult(new InvocationResponse() {
                    Results = SerializeToJsonElement(output)
                }); ;
            }, partialDescriptor);
        }

        #endregion

        #region [ Bind Stream (with IAdapterCallContext) ]

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
                partialDescriptor.RequestSchema,
                partialDescriptor.ResponseSchema
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
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
        /// <typeparam name="TResponse">
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
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindStream<TFeature, TResponse>(
            Func<IAdapterCallContext, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TResponse>(
                handler,
                name,
                description,
                responseSchema,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                IAdapterCallContext ctx,
                InvocationRequest req,
                Func<IAdapterCallContext, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct    
            ) {
                await foreach (var item in handler.Invoke(ctx, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = SerializeToJsonElement(item)
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
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindStream<TFeature, TRequest, TResponse>(
            Func<IAdapterCallContext, TRequest?, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                IAdapterCallContext ctx,
                InvocationRequest req,
                Func<IAdapterCallContext, TRequest?, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct
            ) {
                var input = req.Arguments == null
                    ? default
                    : DeserializeFromJsonElement<TRequest>(req.Arguments.Value, JsonOptions);
                await foreach (var item in handler.Invoke(ctx, input, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = SerializeToJsonElement(item)
                    };
                }
            }

            return BindStream<TFeature>((ctx, req, ct) => RunHandler(ctx, req, handler, ct), partialDescriptor);
        }

        #endregion

        #region [ Bind Stream (without IAdapterCallContext) ]

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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            Func<InvocationRequest, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindStream<TFeature>((ctx, req, ct) => handler(req, ct), partialDescriptor);
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindStream<TFeature, TResponse>(
            Func<CancellationToken, IAsyncEnumerable<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TResponse>(
                handler,
                name,
                description,
                responseSchema,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                InvocationRequest req,
                Func<CancellationToken, IAsyncEnumerable<TResponse?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct
            ) {
                await foreach (var item in handler.Invoke(ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = SerializeToJsonElement(item)
                    };
                }
            }

            return BindStream<TFeature>((ctx, req, ct) => RunHandler(req, handler, ct), partialDescriptor);
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindStream<TFeature, TRequest, TResponse>(
            Func<TRequest?, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                InvocationRequest req,
                Func<TRequest?, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct
            ) {
                var input = req.Arguments == null
                    ? default
                    : DeserializeFromJsonElement<TRequest>(req.Arguments.Value, JsonOptions);
                await foreach (var item in handler.Invoke(input, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = SerializeToJsonElement(item)
                    };
                }
            }

            return BindStream<TFeature>((ctx, req, ct) => RunHandler(req, handler, ct), partialDescriptor);
        }

        #endregion

        #region [ Bind Duplex Stream (with IAdapterCallContext) ]

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
            Func<IAdapterCallContext, DuplexStreamInvocationRequest, IAsyncEnumerable<InvocationStreamItem>, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            ExtensionFeatureOperationDescriptorPartial partialDescriptor
        ) {
            var descriptor = CreateOperationDescriptor<TFeature>(
                ExtensionFeatureOperationType.DuplexStream,
                partialDescriptor.Name!,
                partialDescriptor.Description,
                partialDescriptor.RequestSchema,
                partialDescriptor.ResponseSchema
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            Func<IAdapterCallContext, DuplexStreamInvocationRequest, IAsyncEnumerable<InvocationStreamItem>, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
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
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindDuplexStream<TFeature, TRequest, TResponse>(
            Func<IAdapterCallContext, IAsyncEnumerable<TRequest?>, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                IAdapterCallContext ctx,
                DuplexStreamInvocationRequest req,
                IAsyncEnumerable<InvocationStreamItem> input,
                Func<IAdapterCallContext, IAsyncEnumerable<TRequest?>, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct
            ) {
                var transformedInput = input.Transform(update => update.Arguments == null ? default : DeserializeFromJsonElement<TRequest>(update.Arguments.Value, JsonOptions), ct);
                await foreach (var item in handler.Invoke(ctx, transformedInput, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = SerializeToJsonElement(item)
                    };
                }
            }

            return BindDuplexStream<TFeature>((ctx, req, input, ct) => RunHandler(ctx, req, input, handler, ct), partialDescriptor);
        }

        #endregion

        #region [ Bind Duplex Stream (without IAdapterCallContext) ]

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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters.
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
            Func<DuplexStreamInvocationRequest, IAsyncEnumerable<InvocationStreamItem>, CancellationToken, IAsyncEnumerable<InvocationResponse>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            return BindDuplexStream<TFeature>((ctx, req, ch, ct) => handler(req, ch, ct), partialDescriptor);
        }


        /// <summary>
        /// Binds an extension operation with a type of <see cref="ExtensionFeatureOperationType.DuplexStream"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <typeparam name="TRequest">
        ///   The operation input parameter type.
        /// </typeparam>
        /// <typeparam name="TResponse">
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
        /// <param name="requestSchema">
        ///   The JSON schema for the operation's input parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.RequestSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseSchema">
        ///   The JSON schema for the operation's output parameter. Specifying a value for this 
        ///   parameter overrides metadata obtained from an <see cref="ExtensionFeatureOperationAttribute"/> 
        ///   annotation on the <paramref name="handler"/> or <paramref name="descriptorProvider"/> 
        ///   parameters. If the <see cref="ExtensionFeatureOperationDescriptorPartial.ResponseSchema"/> 
        ///   for the partial descriptor is <see langword="null"/>, a schema will be automatically 
        ///   generated from <typeparamref name="TResponse"/>.
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
        protected bool BindDuplexStream<TFeature, TRequest, TResponse>(
            Func<IAsyncEnumerable<TRequest?>, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
            string? name = null,
            string? description = null,
            JsonElement? requestSchema = null,
            JsonElement? responseSchema = null,
            System.Reflection.MethodInfo? descriptorProvider = null
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var partialDescriptor = CreatePartialOperationDescriptor<TRequest, TResponse>(
                handler,
                name,
                description,
                requestSchema,
                responseSchema,
                descriptorProvider
            );

            async IAsyncEnumerable<InvocationResponse> RunHandler(
                DuplexStreamInvocationRequest req,
                IAsyncEnumerable<InvocationStreamItem> input,
                Func<IAsyncEnumerable<TRequest?>, CancellationToken, IAsyncEnumerable<TResponse?>> handler,
                [EnumeratorCancellation]
                CancellationToken ct
            ) {
                var transformedInput = input.Transform(update => update.Arguments == null ? default : DeserializeFromJsonElement<TRequest>(update.Arguments.Value, JsonOptions), ct);
                await foreach (var item in handler.Invoke(transformedInput, ct).ConfigureAwait(false)) {
                    if (item is InvocationResponse ir) {
                        yield return ir;
                        continue;
                    }

                    yield return new InvocationResponse() {
                        Results = SerializeToJsonElement(item)
                    };
                }
            }

            return BindDuplexStream<TFeature>((ctx, req, input, ct) => RunHandler(req, input, handler, ct), partialDescriptor);
        }

        #endregion

    }
}
