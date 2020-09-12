using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// <remarks>
    ///   Extension features are expected to receive and send JSON-encoded values. Use the 
    ///   <see cref="DeserializeObject"/> and <see cref="SerializeObject"/> methods to deserialize input values 
    ///   from and serialize output values to JSON.
    /// </remarks>
    public abstract partial class AdapterExtensionFeature : IAdapterExtensionFeature {
#pragma warning restore CS0419 // Ambiguous reference in cref attribute

        /// <summary>
        /// Base URI for extension features.
        /// </summary>
        internal static Uri ExtensionUriBase { get; } = new Uri(WellKnownFeatures.Extensions.ExtensionFeatureBasePath);

        /// <summary>
        /// The <see cref="IBackgroundTaskService"/> that can be used to run background tasks.
        /// </summary>
        protected IBackgroundTaskService BackgroundTaskService { get; }


#pragma warning disable CS0419 // Ambiguous reference in cref attribute
        /// <summary>
        /// Operation descriptors created from calls to one of the <see cref="BindInvoke"/>, 
        /// <see cref="BindStream"/>, and <see cref="BindDuplexStream"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, ExtensionFeatureOperationDescriptor> _boundDescriptors = new ConcurrentDictionary<Uri, ExtensionFeatureOperationDescriptor>();

        /// <summary>
        /// Handlers for invocation methods created by a call to one of the <see cref="BindInvoke"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, Func<IAdapterCallContext, string, CancellationToken, Task<string>>> _boundInvokeMethods = new ConcurrentDictionary<Uri, Func<IAdapterCallContext, string, CancellationToken, Task<string>>>();

        /// <summary>
        /// Handlers for streaming methods created by a call to one of the <see cref="BindStream"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, Func<IAdapterCallContext, string, CancellationToken, Task<ChannelReader<string>>>> _boundStreamMethods = new ConcurrentDictionary<Uri, Func<IAdapterCallContext, string, CancellationToken, Task<ChannelReader<string>>>>();

        /// <summary>
        /// Handlers for duplex streaming methods created by a call to one of the <see cref="BindDuplexStream"/> overloads.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, Func<IAdapterCallContext, ChannelReader<string>, CancellationToken, Task<ChannelReader<string>>>> _boundDuplexStreamMethods = new ConcurrentDictionary<Uri, Func<IAdapterCallContext, ChannelReader<string>, CancellationToken, Task<ChannelReader<string>>>>();
#pragma warning restore CS0419 // Ambiguous reference in cref attribute


        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeature"/> object.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks. Can be 
        ///   <see langword="null"/>.
        /// </param>
        protected AdapterExtensionFeature(IBackgroundTaskService backgroundTaskService) {
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;

            foreach (var featureUri in GetType().GetAdapterFeatureUris().Where(x => !ExtensionUriBase.Equals(x) && ExtensionUriBase.IsBaseOf(x))) {
                // Create auto-binding for GetDescriptor method
                var opUri = GetOperationUri(
                    featureUri,
                    nameof(IAdapterExtensionFeature.GetDescriptor),
                    ExtensionFeatureOperationType.Invoke
                );
                _boundInvokeMethods[opUri] = async (ctx, arg, ct) => {
                    var desc = await ((IAdapterExtensionFeature) this).GetDescriptor(ctx, featureUri, ct).ConfigureAwait(false);
                    return SerializeObject(desc);
                };

                // Create auto-binding for GetOperations method
                opUri = GetOperationUri(
                    featureUri, 
                    nameof(IAdapterExtensionFeature.GetOperations), 
                    ExtensionFeatureOperationType.Invoke
                );
                _boundInvokeMethods[opUri] = async (ctx, arg, ct) => {
                    var ops = await ((IAdapterExtensionFeature) this).GetOperations(ctx, featureUri, ct).ConfigureAwait(false);
                    return SerializeObject(ops);
                };
            }
        }


        /// <inheritdoc/>
        Task<FeatureDescriptor> IAdapterExtensionFeature.GetDescriptor(
            IAdapterCallContext context, 
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            return GetDescriptor(
                context, 
                featureUri == null 
                    ? null 
                    : UriExtensions.EnsurePathHasTrailingSlash(featureUri), 
                cancellationToken
            );
        }


        /// <inheritdoc/>
        Task<IEnumerable<ExtensionFeatureOperationDescriptor>> IAdapterExtensionFeature.GetOperations(
            IAdapterCallContext context, 
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            return GetOperations(
                context,
                featureUri == null
                    ? null
                    : UriExtensions.EnsurePathHasTrailingSlash(featureUri),
                cancellationToken
            );
        }


        /// <inheritdoc/>
        Task<string> IAdapterExtensionFeature.Invoke(IAdapterCallContext context, Uri operationId, string json, CancellationToken cancellationToken) {
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }
            return Invoke(context, operationId, json, cancellationToken);
        }


        /// <inheritdoc/>
        Task<ChannelReader<string>> IAdapterExtensionFeature.Stream(IAdapterCallContext context, Uri operationId, string json, CancellationToken cancellationToken) {
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }
            return Stream(context, operationId, json, cancellationToken);
        }


        /// <inheritdoc/>
        Task<ChannelReader<string>> IAdapterExtensionFeature.DuplexStream(IAdapterCallContext context, Uri operationId, ChannelReader<string> channel, CancellationToken cancellationToken) {
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }
            return DuplexStream(context, operationId, channel, cancellationToken);
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
        protected virtual Task<FeatureDescriptor> GetDescriptor(
            IAdapterCallContext context,
            Uri featureUri,
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
        protected virtual Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context,
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            var result = _boundDescriptors
                .Values
                .Where(x => {
                    if (featureUri == null) {
                        return true;
                    }

                    return UriExtensions.IsChildOf(x.OperationId, featureUri);
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
        /// <remarks>
        /// 
        /// <para>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every invocation that has not been registered via a call to <see cref="BindInvoke"/>.
        /// </para>
        /// 
        /// <para>
        ///   When overriding this method, use the <see cref="DeserializeObject"/> and <see cref="SerializeObject"/> 
        ///   methods to deserialize input values and serialize output values from/to JSON.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task<string> Invoke(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context, 
            Uri operationId, 
            string json, 
            CancellationToken cancellationToken
        ) {
            if (_boundInvokeMethods.TryGetValue(operationId, out var handler)) {
                return handler(context, json, cancellationToken);
            }
            throw new MissingMethodException(operationId?.ToString());
        }



#pragma warning disable CS0419 // Ambiguous reference in cref attribute
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
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will stream the JSON-serialized results of the operation.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   The default behaviour of this method is to throw a <see cref="MissingMethodException"/> 
        ///   for every streaming invocation that has not been registered via a call to <see cref="BindStream"/>.
        /// </para>
        /// 
        /// <para>
        ///   When overriding this method, use the <see cref="DeserializeObject"/> and <see cref="SerializeObject"/> 
        ///   methods to deserialize input values and serialize output values from/to JSON.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task<ChannelReader<string>> Stream(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context, 
            Uri operationId, 
            string json, 
            CancellationToken cancellationToken
        ) {
            if (_boundStreamMethods.TryGetValue(operationId, out var handler)) {
                return handler(context, json, cancellationToken);
            }
            throw new MissingMethodException(operationId?.ToString());
        }



#pragma warning disable CS0419 // Ambiguous reference in cref attribute
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
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will stream the JSON-serialized results of the operation.
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
        ///   When overriding this method, use the <see cref="DeserializeObject"/> and <see cref="SerializeObject"/> 
        ///   methods to deserialize input values and serialize output values from/to JSON.
        /// </para>
        /// 
        /// </remarks>
        protected virtual Task<ChannelReader<string>> DuplexStream(
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
            IAdapterCallContext context, 
            Uri operationId, 
            ChannelReader<string> channel, 
            CancellationToken cancellationToken
        ) {
            if (_boundDuplexStreamMethods.TryGetValue(operationId, out var handler)) {
                return handler(context, channel, cancellationToken);
            }
            throw new MissingMethodException(operationId?.ToString());
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
            interfaceMethod = null;

            // Get all of the extension feature interface mappings defined by the implementing type 
            // for the method.
            var interfaceMappings = method
                .ReflectedType
                .GetTypeInfo()
                .ImplementedInterfaces
                .Where(x => x.IsExtensionAdapterFeature())
                .Select(x => method.ReflectedType.GetInterfaceMap(x));

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
        /// Tries to extract details about an extension feature operation from a <see cref="MethodInfo"/> 
        /// that represents the implementation of a method declared in an extension feature 
        /// interface.
        /// </summary>
        /// <param name="method">
        ///   The method implementation.
        /// </param>
        /// <param name="operationType">
        ///   The operation type for the method.
        /// </param>
        /// <param name="operationId">
        ///   The operation ID for the method.
        /// </param>
        /// <param name="displayName">
        ///   The display name for the method.
        /// </param>
        /// <param name="description">
        ///   The description for the method.
        /// </param>
        /// <param name="inputParameterDescription">
        ///   The description for the method's input parameter.
        /// </param>
        /// <param name="outputParameterDescription">
        ///   The description for the method's output parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation details could be extracted from the 
        ///   <paramref name="method"/>, or <see langword="false"/> otherwise.
        /// </returns>
        private static bool TryGetOperationDetailsFromMemberInfo(
            MethodInfo method, 
            ExtensionFeatureOperationType operationType,
            out Uri operationId, 
            out string displayName,
            out string description,
            out string inputParameterDescription,
            out string outputParameterDescription
        ) {
            operationId = null;
            displayName = null;
            description = null;
            inputParameterDescription = null;
            outputParameterDescription = null;

            if (method == null) {
                return false;
            }

            MethodInfo methodDeclaration;
            Type extensionFeatureType;

            if (TryGetInterfaceMethodDeclaration(method, out methodDeclaration)) {
                // We found the method declaration on an extension feature interface. We'll use 
                // this interface definition and the associated method declaration to retrieve 
                // metadata required to create the extension feature operation descriptor.
                extensionFeatureType = methodDeclaration.ReflectedType;
            }
            else if (method.ReflectedType.IsExtensionAdapterFeature()) {
                // The reflected type for the method is an extension feature, so we will look 
                // directly on the method and its reflected type for metadata required to create 
                // the extension feature operation descriptor.
                methodDeclaration = method;
                extensionFeatureType = method.ReflectedType;
            }
            else {
                // We are unable to determine the extension feature that this method is associated 
                // with, so we can't create a descriptor for it.
                methodDeclaration = null;
                extensionFeatureType = null;
            }

            if (methodDeclaration == null || extensionFeatureType == null) {
                return false;
            }

            operationId = GetOperationUri(extensionFeatureType, methodDeclaration.Name, operationType);
            var attr = methodDeclaration.GetCustomAttribute<ExtensionFeatureOperationAttribute>();

            displayName = attr?.GetName();
            description = attr?.GetDescription();
            inputParameterDescription = attr?.GetInputParameterDescription();
            outputParameterDescription = attr?.GetOutputParameterDescription();

            if (string.IsNullOrWhiteSpace(displayName)) {
                displayName = methodDeclaration.Name;
            }

            return true;
        }


        /// <summary>
        /// Creates an <see cref="ExtensionFeatureOperationDescriptor"/> for an operation.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The operation's input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The operation's output parameter type.
        /// </typeparam>
        /// <param name="operationType">
        ///   The operation type.
        /// </param>
        /// <param name="operationId">
        ///   The operation ID.
        /// </param>
        /// <param name="name">
        ///   The operation's display name.
        /// </param>
        /// <param name="description">
        ///   The operation description.
        /// </param>
        /// <param name="hasInputParameter">
        ///   Indicates if the operation uses an input parameter.
        /// </param>
        /// <param name="inputParameterDescription">
        ///   The description for the input parameter.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="hasReturnParameter">
        ///   Indicates if the operation has a return parameter.
        /// </param>
        /// <param name="returnParameterDescription">
        ///   The description for the return parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   A new <see cref="ExtensionFeatureOperationDescriptor"/> instance.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Binding should not fail just because we can't create an instance of a parameter type using Activator.CreateInstance")]
        private static ExtensionFeatureOperationDescriptor CreateDescriptor<TIn, TOut>(
            ExtensionFeatureOperationType operationType,
            Uri operationId,
            string name,
            string description,
            bool hasInputParameter,
            string inputParameterDescription,
            TIn inputParameterExample,
            bool hasReturnParameter,
            string returnParameterDescription,
            TOut returnParameterExample
        ) {
            if (hasInputParameter && !typeof(TIn).IsValueType && Equals(inputParameterExample, null)) {
                try {
                    inputParameterExample = Activator.CreateInstance<TIn>();
                }
                catch {
                    // Swallow the exception; this value is for display purposes only so we don't 
                    // want to make the binding fail!
                }
            }

            if (hasReturnParameter && !typeof(TOut).IsValueType && Equals(returnParameterExample, null)) {
                try {
                    returnParameterExample = Activator.CreateInstance<TOut>();
                }
                catch {
                    // Swallow the exception; this value is for display purposes only so we don't 
                    // want to make the binding fail!
                }
            }

            return new ExtensionFeatureOperationDescriptor() {
                OperationType = operationType,
                OperationId = operationId,
                Name = name,
                Description = description ?? "<UNKNOWN>",
                Input = new ExtensionFeatureOperationParameterDescriptor() {
                    Description = !hasInputParameter 
                        ? "<NOT USED>" 
                        : inputParameterDescription ?? "<UNKNOWN>",
                    ExampleValue = !hasInputParameter 
                        ? SerializeObject(new object()) 
                        : SerializeObject(inputParameterExample, true) ?? "<UNKNOWN>"
                },
                Output = new ExtensionFeatureOperationParameterDescriptor() {
                    Description = !hasReturnParameter
                        ? "<NOT USED>"
                        : returnParameterDescription ?? "<UNKNOWN>",
                    ExampleValue = !hasReturnParameter
                        ? SerializeObject(new object())
                        : SerializeObject(returnParameterExample, true) ?? "<UNKNOWN>"
                },
            };
        }


        /// <summary>
        /// Gets the operation URI for the specified unqualified extension operation name.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The extension feature type. This must be an interface derived from 
        ///   <see cref="IAdapterExtensionFeature"/> that is annotated with <see cref="AdapterFeatureAttribute"/>.
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
        ///   The extension feature type. This must be an interface derived from 
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
        ///   <paramref name="featureType"/> is not an interface derived from <see cref="IAdapterExtensionFeature"/> 
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
            return GetOperationUri(featureUri, unqualifiedName, operationType);
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
            return UriExtensions.EnsurePathHasTrailingSlash(new Uri(
                featureUri,
                unqualifiedName.EndsWith("/", StringComparison.Ordinal)
                    ? string.Concat(unqualifiedName, operationType.ToString())
                    : string.Concat(unqualifiedName, "/", operationType.ToString())
            ));
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
        public static bool TryGetFeatureUriFromOperationUri(Uri operationUri, out Uri featureUri, out string error) {
            if (operationUri == null) {
                throw new ArgumentNullException(nameof(operationUri));
            }

            error = null;

            if (!operationUri.IsAbsoluteUri) {
                featureUri = null;
                error = SharedResources.Error_AbsoluteUriRequired;
                return false;
            }

            if (!UriExtensions.IsChildOf(operationUri, WellKnownFeatures.Extensions.ExtensionFeatureBasePath)) {
                featureUri = null;
                error = Resources.Error_InvalidExtensionFeatureOperationUri;
                return false;
            }

            // Operation URIs are in the format <feature_uri>/<operation_type>/<operation_name>
            featureUri = new Uri(operationUri, "../../");
            return true;
        }


        /// <summary>
        /// Deserializes a value from JSON.
        /// </summary>
        /// <param name="type">
        ///   The value type.
        /// </param>
        /// <param name="json">
        ///   The JSON string.
        /// </param>
        /// <returns>
        ///   The deserialized value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="json"/> is <see langword="null"/>.
        /// </exception>
        public static object DeserializeObject(Type type, string json) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (json == null) {
                throw new ArgumentNullException(nameof(json));
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
        }


        /// <summary>
        /// Deserializes a value from JSON.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="json">
        ///   The JSON string.
        /// </param>
        /// <returns>
        ///   The deserialized value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="json"/> is <see langword="null"/>.
        /// </exception>
        public static T DeserializeObject<T>(string json) {
            if (json == null) {
                throw new ArgumentNullException(nameof(json));
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }


        /// <summary>
        /// Deserializes a value from JSON to an anonymous type.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="json">
        ///   The JSON string.
        /// </param>
        /// <param name="definition">
        ///   An instance of the anonymous type to deserialize the JSON to.
        /// </param>
        /// <returns>
        ///   The deserialized value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="definition"/> is <see langword="null"/>.
        /// </exception>
        public static T DeserializeAnonymousType<T>(string json, T definition) {
            if (json == null) {
                throw new ArgumentNullException(nameof(json));
            }
            if (definition == null) {
                throw new ArgumentNullException(nameof(definition));
            }

            return Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(json, definition);
        }


        /// <summary>
        /// Serializes a value to JSON.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <param name="indented">
        ///   When <see langword="true"/>, indented JSON formatting will be used.
        /// </param>
        /// <returns>
        ///   The serialized value.
        /// </returns>
        public static string SerializeObject(object value, bool indented = false) {
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                value, 
                indented 
                    ? Newtonsoft.Json.Formatting.Indented 
                    : Newtonsoft.Json.Formatting.None
            );
        }

    }
}
