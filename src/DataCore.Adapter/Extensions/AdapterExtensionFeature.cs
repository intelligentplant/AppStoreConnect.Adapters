using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Extensions {

#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    /// Provides a base implementation of <see cref="IAdapterExtensionFeature"/>. Extend from this 
    /// class when writing extension features and use call the <see cref="BindInvoke"/>, 
    /// <see cref="BindStream"/>, and <see cref="BindDuplexStream"/> overloads in your 
    /// class constructor to register available operations.
    /// </summary>
    public abstract partial class AdapterExtensionFeature : IAdapterExtensionFeature, IBackgroundTaskServiceProvider {
#pragma warning restore CS0419 // Ambiguous reference in cref attribute

        /// <summary>
        /// Base URI for extension features.
        /// </summary>
        internal static Uri ExtensionUriBase { get; } = new Uri(WellKnownFeatures.Extensions.BaseUri);

        /// <summary>
        /// The <see cref="IBackgroundTaskService"/> that can be used to run background tasks.
        /// </summary>
        public IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// The <see cref="IObjectEncoder"/> instances to use when encoding or decoding <see cref="EncodedObject"/> 
        /// instances.
        /// </summary>
        private readonly IEnumerable<IObjectEncoder> _encoders;

#pragma warning disable CS0419 // Ambiguous reference in cref attribute
        /// <summary>
        /// Operation descriptors created from calls to one of the <see cref="BindInvoke"/>, 
        /// <see cref="BindStream"/>, and <see cref="BindDuplexStream"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, ExtensionFeatureOperationDescriptor> _boundDescriptors = new ConcurrentDictionary<Uri, ExtensionFeatureOperationDescriptor>();

        /// <summary>
        /// Handlers for invocation methods created by a call to one of the <see cref="BindInvoke"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<InvocationResponse>>> _boundInvokeMethods = new ConcurrentDictionary<Uri, Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<InvocationResponse>>>();

        /// <summary>
        /// Handlers for streaming methods created by a call to one of the <see cref="BindStream"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<ChannelReader<InvocationResponse>>>> _boundStreamMethods = new ConcurrentDictionary<Uri, Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<ChannelReader<InvocationResponse>>>>();

        /// <summary>
        /// Handlers for duplex streaming methods created by a call to one of the <see cref="BindDuplexStream"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, Func<IAdapterCallContext, InvocationRequest, ChannelReader<InvocationStreamItem>, CancellationToken, Task<ChannelReader<InvocationResponse>>>> _boundDuplexStreamMethods = new ConcurrentDictionary<Uri, Func<IAdapterCallContext, InvocationRequest, ChannelReader<InvocationStreamItem>, CancellationToken, Task<ChannelReader<InvocationResponse>>>>();
#pragma warning restore CS0419 // Ambiguous reference in cref attribute


        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeature"/> object.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks. Can be 
        ///   <see langword="null"/>.
        /// </param>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to use when encoding or decoding 
        ///   <see cref="EncodedObject"/> instances.
        /// </param>
        protected AdapterExtensionFeature(IBackgroundTaskService? backgroundTaskService, IEnumerable<IObjectEncoder> encoders) {
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            _encoders = encoders?.ToArray() ?? Array.Empty<IObjectEncoder>();

            foreach (var featureUri in GetType().GetAdapterFeatureUris().Where(x => !ExtensionUriBase.Equals(x) && ExtensionUriBase.IsBaseOf(x))) {
                // Create auto-binding for GetDescriptor method
                var opUri = GetOperationUri(
                    featureUri,
                    nameof(IAdapterExtensionFeature.GetDescriptor),
                    ExtensionFeatureOperationType.Invoke
                );
                _boundInvokeMethods[opUri] = async (ctx, req, ct) => {
                    var desc = await ((IAdapterExtensionFeature) this).GetDescriptor(ctx, featureUri, ct).ConfigureAwait(false);
                    return new InvocationResponse() { 
                        Results = new[] {
                            Encode(desc)!
                        }
                    };
                };

                // Create auto-binding for GetOperations method
                opUri = GetOperationUri(
                    featureUri, 
                    nameof(IAdapterExtensionFeature.GetOperations), 
                    ExtensionFeatureOperationType.Invoke
                );
                _boundInvokeMethods[opUri] = async (ctx, arg, ct) => {
                    var ops = await ((IAdapterExtensionFeature) this).GetOperations(ctx, featureUri, ct).ConfigureAwait(false);
                    return new InvocationResponse() { 
                        Results = ops.Select(x => Encode(x)!).ToArray()
                    };
                };
            }
        }


        /// <summary>
        /// Encodes the specified value as an <see cref="EncodedObject"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the object to encode.
        /// </typeparam>
        /// <param name="value">
        ///   The object to encode.
        /// </param>
        /// <returns>
        ///   The encoded object.
        /// </returns>
        protected EncodedObject? Encode<T>(T? value) {
            var encoder = _encoders.FirstOrDefault(x => x.CanEncode(typeof(T)));
            if (encoder == null) {
                return null;
            }

            return encoder.Encode(value);
        }


        /// <summary>
        /// Decodes the specified <see cref="EncodedObject"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to decode the <see cref="EncodedObject"/> to.
        /// </typeparam>
        /// <param name="value">
        ///   The object to encode.
        /// </param>
        /// <returns>
        ///   The encoded object.
        /// </returns>
        protected T? Decode<T>(EncodedObject? value) {
            if (value == null) {
                return default;
            }

            if (typeof(T) == typeof(EncodedObject)) {
                // Special case for EncodedObject; return the value directly (via some casting 
                // jiggery pokery to keep the compiler happy).
                return (T) ((object) value);
            }

            var encoder = _encoders.FirstOrDefault(x => x.CanDecode(typeof(T), value.Encoding));
            if (encoder == null) {
                return default;
            }

            return encoder.Decode<T>(value);
        }


        /// <summary>
        /// Decodes the specified <see cref="EncodedObject"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to decode the <see cref="EncodedObject"/> to.
        /// </typeparam>
        /// <param name="value">
        ///   The object to decode.
        /// </param>
        /// <returns>
        ///   The encoded object.
        /// </returns>
        protected T? Decode<T>(Variant value) {
            if (value.Type != VariantType.ExtensionObject) {
                return default;
            }

            var extensionObject = (EncodedObject?) value;
            if (extensionObject == null) {
                return default;
            }

            var encoder = _encoders.FirstOrDefault(x => x.CanDecode(typeof(T), extensionObject.Encoding));
            if (encoder == null) {
                return default;
            }

            return encoder.Decode<T>(value);
        }


        /// <inheritdoc/>
        public Task<FeatureDescriptor?> GetDescriptor(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            return GetDescriptorInternal(
                context, 
                featureUri == null 
                    ? null 
                    : featureUri.EnsurePathHasTrailingSlash(), 
                cancellationToken
            );
        }


        /// <inheritdoc/>
        public Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            return GetOperationsInternal(
                context,
                featureUri == null
                    ? null
                    : featureUri.EnsurePathHasTrailingSlash(),
                cancellationToken
            );
        }


        /// <inheritdoc/>
        public Task<InvocationResponse> Invoke(
            IAdapterCallContext context,
            InvocationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);

            return InvokeInternal(context, request, cancellationToken);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<InvocationResponse>> Stream(
            IAdapterCallContext context,
            InvocationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);

            return StreamInternal(context, request, cancellationToken);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<InvocationResponse>> DuplexStream(IAdapterCallContext context, InvocationRequest request, ChannelReader<InvocationStreamItem> channel, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            return DuplexStreamInternal(context, request, channel, cancellationToken);
        }


        /// <summary>
        /// Gets the descriptor for the extension feature.
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
        ///   The extension feature descriptor.
        /// </returns>
        /// <remarks>
        ///   It is not normally required to override this method. Override only if the feature 
        ///   descriptor is not generated using an <see cref="ExtensionFeatureAttribute"/>.
        /// </remarks>
        protected virtual Task<FeatureDescriptor?> GetDescriptorInternal(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            var result = featureUri == null
                ? GetType().CreateFeatureDescriptor()
                : GetType()
                    .GetAdapterFeatureTypes()
                    .FirstOrDefault(x => x.HasAdapterFeatureUri(featureUri))
                    ?.CreateFeatureDescriptor();

            return Task.FromResult(result);
        }


#pragma warning disable CS0419 // Ambiguous reference in cref attribute
        /// <summary>
        /// Gets the operations that are supported by the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="featureUri">
        ///   The requested feature URI.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation descriptors.
        /// </returns>
        /// <remarks>
        ///  
        /// <para>
        ///   It is only necessary to override this method if your extension feature implementation 
        ///   defines operations that cannot be registered via a call to one of the <see cref="BindInvoke"/>, 
        ///   <see cref="BindStream"/>, or <see cref="BindDuplexStream"/> method overloads.
        /// </para>
        /// 
        /// <para>
        ///   The <see cref="GetOperationUri"/> method should be used to generate URIs for extension 
        ///   feature operations.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsInternal(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            var result = _boundDescriptors
                .Values
                .Where(x => {
                    if (featureUri == null) {
                        return true;
                    }

                    return x.OperationId.IsChildOf(featureUri);
                })
                .ToArray();

            return Task.FromResult<IEnumerable<ExtensionFeatureOperationDescriptor>>(result);
        }



#pragma warning disable CS0419 // Ambiguous reference in cref attribute
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
        /// <remarks>
        /// 
        /// <para>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every invocation that has not been registered via a call to <see cref="BindInvoke"/>.
        /// </para>
        /// 
        /// <para>
        ///   When overriding this method, use the <see cref="Encode{T}"/> method to 
        ///   simplify creation of values if your extension method returns <see cref="Variant"/> 
        ///   values with a type of <see cref="VariantType.ExtensionObject"/>.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task<InvocationResponse> InvokeInternal(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context, 
            InvocationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (_boundInvokeMethods.TryGetValue(request.OperationId, out var handler)) {
                return handler(context, request, cancellationToken);
            }
            throw new MissingMethodException(request.OperationId?.ToString());
        }


#pragma warning disable CS0419 // Ambiguous reference in cref attribute
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
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will stream the results of the operation.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every streaming invocation that has not been registered via a call to <see cref="BindStream"/>.
        /// </para>
        /// 
        /// <para>
        ///   When overriding this method, use the <see cref="Encode{T}"/> method to 
        ///   simplify creation of values if your extension method returns <see cref="Variant"/> 
        ///   values with a type of <see cref="VariantType.ExtensionObject"/>.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task<ChannelReader<InvocationResponse>> StreamInternal(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context, 
            InvocationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (_boundStreamMethods.TryGetValue(request.OperationId, out var handler)) {
                return handler(context, request, cancellationToken);
            }
            throw new MissingMethodException(request.OperationId?.ToString());
        }



#pragma warning disable CS0419 // Ambiguous reference in cref attribute
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
        ///   A channel that will stream additional inputs for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will stream the results of the operation.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every duplex streaming operation that has not been registered via a call to 
        ///   <see cref="BindDuplexStream"/>.
        /// </para>
        /// 
        /// <para>
        ///   When overriding this method, use the <see cref="Encode{T}"/> method to 
        ///   simplify creation of values if your extension method returns <see cref="Variant"/> 
        ///   values with a type of <see cref="VariantType.ExtensionObject"/>.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task<ChannelReader<InvocationResponse>> DuplexStreamInternal(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context, 
            InvocationRequest request, 
            ChannelReader<InvocationStreamItem> channel, 
            CancellationToken cancellationToken
        ) {
            if (_boundDuplexStreamMethods.TryGetValue(request.OperationId, out var handler)) {
                return handler(context, request, channel, cancellationToken);
            }
            throw new MissingMethodException(request.OperationId?.ToString());
        }


        /// <summary>
        /// For the specified <paramref name="method"/>, attempts to find the equivalent method 
        /// declaration in an interface implemented by the method's implementing type.
        /// </summary>
        /// <param name="method">
        ///   The <see cref="MethodInfo"/> implementation to retrieve the interface declaration for.
        /// </param>
        /// <param name="interfaceMethod">
        ///   The equivalent <see cref="MethodInfo"/> as declared on the interface type that defines
        ///   the method.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if an equivalent method was found in an interface implemented 
        ///   by the <see cref="MemberInfo.ReflectedType"/> for the <paramref name="method"/>, or
        ///   <see langword="false"/> otherwise.
        /// </returns>
        private static bool TryGetInterfaceMethodDeclaration(MethodInfo method, out MethodInfo interfaceMethod) {
            interfaceMethod = null!;

            // Get all of the extension feature interface mappings defined by the implementing type 
            // for the method.
            var interfaceMappings = method
                .ReflectedType!
                .GetTypeInfo()
                .ImplementedInterfaces
                .Where(x => x.IsExtensionAdapterFeature())
                .Select(x => method.ReflectedType!.GetInterfaceMap(x));

            foreach (var mapping in interfaceMappings) {
                var methodIndex = -1;

                // The target methods for the mapping contain the actual method implementations i.e. 
                // the method parameter.
                for (var i = 0; i < mapping.TargetMethods.Length; i++) {
                    var targetMethod = mapping.TargetMethods[i];
                    if (targetMethod != method) {
                        // Not the method we're looking for.
                        continue;
                    }

                    // We're found our method implementation; make a note of the index so that we 
                    // can get the equivalent method on the interface type.
                    methodIndex = i;
                    break;
                }

                if (methodIndex >= 0) {
                    // We found our method implementation in the current interface mapping. The 
                    // method definition on the interface will be at the same index as the method 
                    // definition was found at on the implementing class.
                    interfaceMethod = mapping.InterfaceMethods[methodIndex];
                    break;
                }
            }

            return interfaceMethod != null;
        }


        /// <summary>
        /// Creates an <see cref="ExtensionFeatureOperationDescriptor"/> for a feature operation.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="operationType">
        ///   The operation type.
        /// </param>
        /// <param name="name">
        ///   The operation name.
        /// </param>
        /// <param name="description">
        ///   The operation description.
        /// </param>
        /// <param name="inputParameters">
        ///   The input parameters for the operation.
        /// </param>
        /// <param name="outputParameters">
        ///   The output parameters for the operation.
        /// </param>
        /// <returns>
        ///   A new <see cref="ExtensionFeatureOperationDescriptor"/> object.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   and annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </exception>
        protected static ExtensionFeatureOperationDescriptor CreateOperationDescriptor<TFeature>(
            ExtensionFeatureOperationType operationType,
            string name,
            string? description,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? inputParameters,
            IEnumerable<ExtensionFeatureOperationParameterDescriptor>? outputParameters
        ) {
            var featureType = typeof(TFeature);
            var operationId = GetOperationUri(featureType, name, operationType);
            return new ExtensionFeatureOperationDescriptor() { 
                OperationId = operationId,
                OperationType = operationType,
                Name = name,
                Description = description,
                Inputs = inputParameters?.ToArray() ?? Array.Empty<ExtensionFeatureOperationParameterDescriptor>(),
                Outputs = outputParameters?.ToArray() ?? Array.Empty<ExtensionFeatureOperationParameterDescriptor>()
            };
        }


        /// <summary>
        /// Gets the operation URI for the specified unqualified extension operation name.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </typeparam>
        /// <param name="unqualifiedName">
        ///   The unqualified operation name.
        /// </param>
        /// <param name="operationType">
        ///   The operation type.
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
        protected static Uri GetOperationUri<TFeature>(string unqualifiedName, ExtensionFeatureOperationType operationType) where TFeature : IAdapterExtensionFeature {
            return GetOperationUri(typeof(TFeature), unqualifiedName, operationType);
        }


        /// <summary>
        /// Gets the operation URI for the specified unqualified extension operation name.
        /// </summary>
        /// <param name="featureType">
        ///   The extension feature type. This must be an type derived from 
        ///   <see cref="IAdapterExtensionFeature"/> that is annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </param>
        /// <param name="unqualifiedName">
        ///   The unqualified operation name.
        /// </param>
        /// <param name="operationType">
        ///   The operation type.
        /// </param>
        /// <returns>
        ///   The operation URI.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="featureType"/> is not a type derived from <see cref="IAdapterExtensionFeature"/> 
        ///   and annotated with <see cref="AdapterFeatureAttribute"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="unqualifiedName"/> is <see langword="null"/> or white space.
        /// </exception>
        protected static Uri GetOperationUri(Type featureType, string unqualifiedName, ExtensionFeatureOperationType operationType) {
            if (!featureType.IsExtensionAdapterFeature()) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_NotAnExtensionFeatureInterface, nameof(IAdapterExtensionFeature), nameof(AdapterFeatureAttribute)));
            }

            if (string.IsNullOrWhiteSpace(unqualifiedName)) {
                throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(unqualifiedName));
            }

            var featureUri = featureType.GetAdapterFeatureUri();
            return GetOperationUri(featureUri!, unqualifiedName, operationType);
        }


        /// <summary>
        /// Gets the operation URI for the specified unqualified extension operation name.
        /// </summary>
        /// <param name="featureUri">
        ///   The extension feature URI.
        /// </param>
        /// <param name="unqualifiedName">
        ///   The unqualified operation name.
        /// </param>
        /// <param name="operationType">
        ///   The operation type.
        /// </param>
        /// <returns>
        ///   The operation URI.
        /// </returns>
        private static Uri GetOperationUri(Uri featureUri, string unqualifiedName, ExtensionFeatureOperationType operationType) {
            return new Uri(
                featureUri,
                unqualifiedName.EndsWith("/", StringComparison.Ordinal)
                    ? string.Concat(operationType.ToString().ToLowerInvariant(), "/", unqualifiedName)
                    : string.Concat(operationType.ToString().ToLowerInvariant(), "/", unqualifiedName, "/")
            ).EnsurePathHasTrailingSlash();
        }


        /// <summary>
        /// Gets the extension feature URI from the specified operation URI.
        /// </summary>
        /// <param name="operationUri">
        ///   The operation URI.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI
        /// </param>
        /// <param name="error">
        ///   The reason for the failure, if the return value is <see langword="false"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if a valid extension feature URI could be extracted, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationUri"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetFeatureUriFromOperationUri(Uri operationUri, out Uri featureUri, out string? error) {
            if (operationUri == null) {
                throw new ArgumentNullException(nameof(operationUri));
            }
            
            error = null;

            if (!operationUri.IsAbsoluteUri) {
                featureUri = null!;
                error = SharedResources.Error_AbsoluteUriRequired;
                return false;
            }

            if (!operationUri.IsChildOf(WellKnownFeatures.Extensions.BaseUri)) {
                featureUri = null!;
                error = Resources.Error_InvalidExtensionFeatureOperationUri;
                return false;
            }

            // Operation URIs are in the format <feature_uri>/<operation_type>/<operation_name>
            featureUri = new Uri(operationUri, "../../");
            return true;
        }

    }
}
