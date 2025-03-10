using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using JsonSchema = Json.Schema;

using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// <see cref="ICustomFunctions"/> implementation that allows custom functions to be 
    /// dynamically registered and unregistered at runtime.
    /// </summary>
    public sealed partial class CustomFunctions : ICustomFunctions {

        /// <summary>
        /// The base URI for custom functions registered using a relative URI.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<CustomFunctions> _logger;

        /// <summary>
        /// JSON options to use when serializing/deserializing custom function request and 
        /// response bodies.
        /// </summary>
        public JsonSerializerOptions? JsonOptions { get; set; }

        /// <summary>
        /// The registered functions.
        /// </summary>
        private readonly Dictionary<Uri, CustomFunctionRegistration> _functions = new Dictionary<Uri, CustomFunctionRegistration>();

        /// <summary>
        /// Lock for accessing <see cref="_functions"/>.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _functionsLock = new Nito.AsyncEx.AsyncReaderWriterLock();

        /// <summary>
        /// The default authorisation handler to use if a custom function registration does not 
        /// specify its own authorisation handler.
        /// </summary>
        public CustomFunctionAuthorizeHandler? DefaultAuthorizeHandler { get; set; }


        /// <summary>
        /// Creates a new <see cref="CustomFunctions"/> instance.
        /// </summary>
        /// <param name="baseUri">
        ///   The base URI for custom functions registered with a relative URI. This would usually 
        ///   be the type URI for the adapter associated with the <see cref="CustomFunctions"/> 
        ///   instance.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="baseUri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="baseUri"/> is not an absolute URI.
        /// </exception>
        public CustomFunctions(Uri baseUri, JsonSerializerOptions? jsonOptions = null, ILogger<CustomFunctions>? logger = null) {
            if (baseUri == null) {
                throw new ArgumentNullException(nameof(baseUri));
            }
            if (!baseUri.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(baseUri), SharedResources.Error_AbsoluteUriRequired);
            }
            BaseUri = new Uri(baseUri.EnsurePathHasTrailingSlash(), "custom-functions/");
            JsonOptions = jsonOptions;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomFunctions>.Instance;
        }


        /// <summary>
        /// Checks if the caller is authorised to call the specified custom function.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="registration">
        ///   The custom function registration.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return <see langword="true"/> if the 
        ///   caller is authorised or <see langword="false"/> otherwise.
        /// </returns>
        private async ValueTask<bool> IsAuthorizedAsync(IAdapterCallContext context, CustomFunctionRegistration registration, CancellationToken cancellationToken) {
            var handler = registration.Authorize ?? DefaultAuthorizeHandler;
            return handler == null || await handler.Invoke(context, registration.Descriptor.Id, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Registers a custom function.
        /// </summary>
        /// <param name="descriptor">
        ///   The function descriptor.
        /// </param>
        /// <param name="handler">
        ///   The function handler.
        /// </param>
        /// <param name="authorizeHandler">
        ///   The function's authorisation handler.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the custom function was registered, or <see langword="false"/> 
        ///   if a function with the same ID is already registered.
        /// </returns>
        private bool RegisterFunctionCore(
            CustomFunctionDescriptorExtended descriptor,
            CustomFunctionHandler handler,
            CustomFunctionAuthorizeHandler? authorizeHandler
        ) {
            var lookupId = descriptor.Id.EnsurePathHasTrailingSlash();
            if (_functions.ContainsKey(lookupId)) {
                return false;
            }

            var reg = new CustomFunctionRegistration(
                descriptor,
                handler,
                authorizeHandler
            );

            _functions[lookupId] = reg;
            LogFunctionRegistered(lookupId);
            return true;
        }


        /// <summary>
        /// Registers a custom function.
        /// </summary>
        /// <param name="descriptor">
        ///   The function descriptor.
        /// </param>
        /// <param name="handler">
        ///   The function handler.
        /// </param>
        /// <param name="authorizeHandler">
        ///   The function's authorisation handler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return <see langword="true"/> if the custom 
        ///   function was registered, or <see langword="false"/> if a function with the same ID is 
        ///   already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="descriptor"/> is not valid.
        /// </exception>
        public async Task<bool> RegisterFunctionAsync(
            CustomFunctionDescriptorExtended descriptor,
            CustomFunctionHandler handler, 
            CustomFunctionAuthorizeHandler? authorizeHandler = null, 
            CancellationToken cancellationToken = default
        ) {
            ValidationExtensions.ValidateObject(descriptor);
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            using (await _functionsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                RegisterFunctionCore(descriptor, handler, authorizeHandler);
            }

            return true;
        }


        /// <summary>
        /// Registers a custom function.
        /// </summary>
        /// <param name="descriptor">
        ///   The function descriptor.
        /// </param>
        /// <param name="handler">
        ///   The function handler.
        /// </param>
        /// <param name="authorizeHandler">
        ///   The function's authorisation handler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the custom function was registered, or <see langword="false"/> 
        ///   if a function with the same ID is already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="descriptor"/> is not valid.
        /// </exception>
        /// <remarks>
        ///   This method blocks the calling thread. Use <see cref="RegisterFunctionAsync(CustomFunctionDescriptorExtended, CustomFunctionHandler, CustomFunctionAuthorizeHandler?, CancellationToken)"/> 
        ///   to register a function asynchronously.
        /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public bool RegisterFunction(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            CustomFunctionDescriptorExtended descriptor,
            CustomFunctionHandler handler,
            CustomFunctionAuthorizeHandler? authorizeHandler = null,
            CancellationToken cancellationToken = default
        ) {
            ValidationExtensions.ValidateObject(descriptor);
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            using (_functionsLock.WriterLock(cancellationToken)) {
                RegisterFunctionCore(descriptor, handler, authorizeHandler);
            }

            return true;
        }


        /// <summary>
        /// Registers a custom function.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request type of the custom function.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response type of the custom function.
        /// </typeparam>
        /// <param name="id">
        ///   The function ID. If a relative URI is specified, it will be made absolute using the 
        ///   <see cref="BaseUri"/>.
        /// </param>
        /// <param name="name">
        ///   The name of the function.
        /// </param>
        /// <param name="description">
        ///   The function's description.
        /// </param>
        /// <param name="handler">
        ///   The function handler.
        /// </param>
        /// <param name="authorizeHandler">
        ///   The function's authorisation handler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return <see langword="true"/> if the custom 
        ///   function was registered, or <see langword="false"/> if a function with the same ID is 
        ///   already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="name"/> is not valid.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="description"/> is not valid.
        /// </exception>
        /// <remarks>
        ///   A JSON schema will be generated for the request and response types. See 
        ///   <see cref="CreateJsonSchema{T}"/> for details about how the schema is generated.
        /// </remarks>
        public async Task<bool> RegisterFunctionAsync<TRequest, TResponse>(
            Uri id,
            string name,
            string? description,
            CustomFunctionHandler<TRequest, TResponse> handler,
            CustomFunctionAuthorizeHandler? authorizeHandler = null,
            CancellationToken cancellationToken = default
        ) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var descriptor = new CustomFunctionDescriptorExtended(
                id.IsAbsoluteUri
                    ? id
                    : new Uri(BaseUri, id),
                name,
                description, 
                CreateJsonSchema<TRequest>(),
                CreateJsonSchema<TResponse>()
            );

            return await RegisterFunctionAsync(
                descriptor,
                CreateHandler(handler, JsonOptions),
                authorizeHandler,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Registers a custom function.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request type of the custom function.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response type of the custom function.
        /// </typeparam>
        /// <param name="id">
        ///   The function ID. If a relative URI is specified, it will be made absolute using the 
        ///   <see cref="BaseUri"/>.
        /// </param>
        /// <param name="name">
        ///   The name of the function.
        /// </param>
        /// <param name="description">
        ///   The function's description.
        /// </param>
        /// <param name="handler">
        ///   The function handler.
        /// </param>
        /// <param name="authorizeHandler">
        ///   The function's authorisation handler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the custom function was registered, or <see langword="false"/> 
        ///   if a function with the same ID is already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="name"/> is not valid.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="description"/> is not valid.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   This method blocks the calling thread. Use <see cref="RegisterFunctionAsync{TRequest, TResponse}(Uri, string, string?, CustomFunctionHandler{TRequest, TResponse}, CustomFunctionAuthorizeHandler?, CancellationToken)"/> 
        ///   to register a function asynchronously.
        /// </para>
        /// 
        /// <para>
        ///   A JSON schema will be generated for the request and response types. See 
        ///   <see cref="CreateJsonSchema{T}"/> for details about how the schema is generated.
        /// </para>
        /// 
        /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public bool RegisterFunction<TRequest, TResponse>(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            Uri id,
            string name,
            string? description,
            CustomFunctionHandler<TRequest, TResponse> handler,
            CustomFunctionAuthorizeHandler? authorizeHandler = null,
            CancellationToken cancellationToken = default
        ) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var descriptor = new CustomFunctionDescriptorExtended(
                id.IsAbsoluteUri
                    ? id
                    : new Uri(BaseUri, id),
                name,
                description,
                CreateJsonSchema<TRequest>(),
                CreateJsonSchema<TResponse>()
            );

            return RegisterFunction(
                descriptor,
                CreateHandler(handler, JsonOptions),
                authorizeHandler,
                cancellationToken
            );
        }


        /// <summary>
        /// Registers a custom function.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request type of the custom function.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response type of the custom function.
        /// </typeparam>
        /// <param name="name">
        ///   The name of the function.
        /// </param>
        /// <param name="description">
        ///   The function's description.
        /// </param>
        /// <param name="handler">
        ///   The function handler.
        /// </param>
        /// <param name="authorizeHandler">
        ///   The function's authorisation handler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return <see langword="true"/> if the custom 
        ///   function was registered, or <see langword="false"/> if a function with the same ID is 
        ///   already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="description"/> is not valid.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   The registered function will be assigned an ID derived from the <see cref="BaseUri"/> 
        ///   and the function <paramref name="name"/>.
        /// </para>
        /// 
        /// <para>
        ///   A JSON schema will be generated for the request and response types. See 
        ///   <see cref="CreateJsonSchema{T}"/> for details about how the schema is generated.
        /// </para>
        /// 
        /// </remarks>
        public async Task<bool> RegisterFunctionAsync<TRequest, TResponse>(
            string name,
            string? description,
            CustomFunctionHandler<TRequest, TResponse> handler,
            CustomFunctionAuthorizeHandler? authorizeHandler = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return await RegisterFunctionAsync(new Uri(BaseUri, name.ToLowerInvariant()), name, description, handler, authorizeHandler, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Registers a custom function.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request type of the custom function.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response type of the custom function.
        /// </typeparam>
        /// <param name="name">
        ///   The name of the function.
        /// </param>
        /// <param name="description">
        ///   The function's description.
        /// </param>
        /// <param name="handler">
        ///   The function handler.
        /// </param>
        /// <param name="authorizeHandler">
        ///   The function's authorisation handler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the custom function was registered, or <see langword="false"/> 
        ///   if a function with the same ID is already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="description"/> is not valid.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   This method blocks the calling thread. Use <see cref="RegisterFunctionAsync{TRequest, TResponse}(string, string?, CustomFunctionHandler{TRequest, TResponse}, CustomFunctionAuthorizeHandler?, CancellationToken)"/> 
        ///   to register a function asynchronously.
        /// </para>
        /// 
        /// <para>
        ///   The registered function will be assigned an ID derived from the <see cref="BaseUri"/> 
        ///   and the function <paramref name="name"/>.
        /// </para>
        /// 
        /// <para>
        ///   A JSON schema will be generated for the request and response types. See 
        ///   <see cref="CreateJsonSchema{T}"/> for details about how the schema is generated.
        /// </para>
        /// 
        /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public bool RegisterFunction<TRequest, TResponse>(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            string name,
            string? description,
            CustomFunctionHandler<TRequest, TResponse> handler,
            CustomFunctionAuthorizeHandler? authorizeHandler = null,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return RegisterFunction(new Uri(BaseUri, name.ToLowerInvariant()), name, description, handler, authorizeHandler, cancellationToken);
        }


        /// <summary>
        /// Unregisters a custom function.
        /// </summary>
        /// <param name="id">
        ///   The custom function ID.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the function was unregistered, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        private bool UnregisterFunctionCore(Uri id) {
            var lookupId = id.EnsurePathHasTrailingSlash();
            if (_functions.Remove(lookupId)) {
                LogFunctionUnregistered(lookupId);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Unregisters a custom function.
        /// </summary>
        /// <param name="id">
        ///   The custom function ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return <see langword="true"/> if the 
        ///   function was unregistered, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public async Task<bool> UnregisterFunctionAsync(Uri id, CancellationToken cancellationToken = default) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            using (await _functionsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                return UnregisterFunctionCore(id);
            }
        }


        /// <summary>
        /// Unregisters a custom function.
        /// </summary>
        /// <param name="id">
        ///   The custom function ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the function was unregistered, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   This method blocks the calling thread. Use <see cref="UnregisterFunctionAsync(Uri, CancellationToken)"/> 
        ///   to unregister a function asynchronously
        /// </remarks>
        public bool UnregisterFunction(Uri id, CancellationToken cancellationToken = default) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            using (_functionsLock.WriterLock(cancellationToken)) {
                return UnregisterFunctionCore(id);
            }
        }


        /// <inheritdoc/>
        async Task<IEnumerable<CustomFunctionDescriptor>> ICustomFunctions.GetFunctionsAsync(
            IAdapterCallContext context, 
            GetCustomFunctionsRequest request,
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);

            using (await _functionsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                var result = new List<CustomFunctionDescriptor>(request.PageSize);

                IEnumerable<CustomFunctionRegistration> funcs = _functions.Values;

                if (!string.IsNullOrEmpty(request.Id)) {
                    funcs = funcs.Where(x => x.Descriptor.Id.ToString().Like(request.Id!));
                }
                if (!string.IsNullOrEmpty(request.Name)) {
                    funcs = funcs.Where(x => x.Descriptor.Name.Like(request.Name!));
                }
                if (!string.IsNullOrEmpty(request.Description)) {
                    funcs = funcs.Where(x => x.Descriptor.Description != null && x.Descriptor.Description.Like(request.Description!));
                }

                var skipCount = (request.Page - 1) * request.PageSize;
                var takeCount = request.PageSize;

                foreach (var item in funcs.OrderBy(x => x.Descriptor.Name, StringComparer.OrdinalIgnoreCase)) {
                    if (takeCount < 1) {
                        break;
                    }

                    if (!await IsAuthorizedAsync(context, item, cancellationToken).ConfigureAwait(false)) {
                        continue;
                    }

                    if (skipCount > 0) {
                        --skipCount;
                        continue;
                    }

                    result.Add(new CustomFunctionDescriptor(item.Descriptor.Id, item.Descriptor.Name, item.Descriptor.Description));

                    --takeCount;
                }

                return result;
            }
        }


        /// <inheritdoc/>
        async Task<CustomFunctionDescriptorExtended?> ICustomFunctions.GetFunctionAsync(
            IAdapterCallContext context, 
            GetCustomFunctionRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);

            var lookupId = request.Id.IsAbsoluteUri
                ? request.Id.EnsurePathHasTrailingSlash()
                : new Uri(BaseUri, request.Id).EnsurePathHasTrailingSlash();

            using (await _functionsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_functions.TryGetValue(lookupId, out var func) || !await IsAuthorizedAsync(context, func, cancellationToken).ConfigureAwait(false)) {
                    return null;
                }

                return func.Descriptor;
            }
        }


        /// <inheritdoc/>
        async Task<CustomFunctionInvocationResponse> ICustomFunctions.InvokeFunctionAsync(IAdapterCallContext context, CustomFunctionInvocationRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);

            var lookupId = request.Id.IsAbsoluteUri
                ? request.Id.EnsurePathHasTrailingSlash()
                : new Uri(BaseUri, request.Id).EnsurePathHasTrailingSlash();

            CustomFunctionHandler handler;

            using (await _functionsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_functions.TryGetValue(lookupId, out var func)) {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_UnknownCustomFunctionId, request.Id));
                }

                if (!await IsAuthorizedAsync(context, func, cancellationToken).ConfigureAwait(false)) {
                    throw new System.Security.SecurityException(string.Format(CultureInfo.CurrentCulture, Resources.Error_NotAuthorisedToInvokeFunction, request.Id));
                }

                handler = func.Handler;
            }

            LogFunctionInvoked(lookupId);
            return await handler.Invoke(context, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a JSON schema for the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to generate a JSON schema for.
        /// </typeparam>
        /// <returns>
        ///   The JSON schema, represented as a <see cref="JsonElement"/>.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   <see cref="CreateJsonSchema{T}"/> can be used to simplify generation of request and 
        ///   response schemas for <see cref="CustomFunctionDescriptorExtended"/> instances.
        /// </para>
        /// 
        /// <para>
        ///   Schema generation is performed using <see cref="JsonSchema.JsonSchemaBuilder"/>. 
        ///   Attributes can be specified on properties to customise the generated schema. In 
        ///   addition to the attributes in the <see cref="JsonSchema.Generation"/> namespace, 
        ///   the following attributes from the <see cref="System.ComponentModel.DataAnnotations"/> 
        ///   namespace can also be used:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.DataTypeAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DescriptionAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DisplayNameAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.MaxLengthAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.MinLengthAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.RangeAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.RegularExpressionAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.RequiredAttribute"/>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public JsonElement CreateJsonSchema<T>() {
            return Json.Schema.JsonSchemaUtility.CreateJsonSchema<T>(JsonOptions);
        }


        /// <summary>
        /// Tries to validate the specified JSON document against a schema.
        /// </summary>
        /// <param name="data">
        ///   The JSON data to validate.
        /// </param>
        /// <param name="schema">
        ///   The schema to validate the <paramref name="data"/> against.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <param name="validationResults">
        ///   The validation results.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="data"/> was successfully validated 
        ///   against the <paramref name="schema"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryValidate(JsonElement data, JsonElement schema, JsonSerializerOptions? jsonOptions, out JsonElement validationResults) {
            return Json.Schema.JsonSchemaUtility.TryValidate(data, schema, jsonOptions, out validationResults);
        }


        /// <summary>
        /// Creates a <see cref="CustomFunctionHandler"/> that deserializes the body of a 
        /// <see cref="CustomFunctionInvocationRequest"/> to a <typeparamref name="TRequest"/>, 
        /// invokes the specified <paramref name="innerHandler"/>, and then serializes the resulting 
        /// <typeparamref name="TResponse"/> to a <see cref="CustomFunctionInvocationResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request type.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response type.
        /// </typeparam>
        /// <param name="innerHandler">
        ///   The inner <see cref="CustomFunctionHandler{TRequest, TResponse}"/>.
        /// </param>
        /// <param name="jsonOptions">
        ///   The JSON serializer options to use when deserializing the request and serializing 
        ///   the response.
        /// </param>
        /// <returns>
        ///   A new <see cref="CustomFunctionHandler"/> delegate.
        /// </returns>
        public static CustomFunctionHandler CreateHandler<TRequest, TResponse>(CustomFunctionHandler<TRequest, TResponse> innerHandler, JsonSerializerOptions? jsonOptions = null) {
            return async (context, request, ct) => {
                var result = await innerHandler.Invoke(context, request.Body == null ? default! : request.Body.Value.Deserialize<TRequest>(jsonOptions)!, ct).ConfigureAwait(false);
                return new CustomFunctionInvocationResponse() {
                    Body = JsonSerializer.SerializeToElement(result, jsonOptions)
                };
            };
        }


        /// <summary>
        /// Creates a <see cref="CustomFunctionHandler"/> that invokes the specified <paramref name="innerHandler"/>, 
        /// and then serializes the resulting <typeparamref name="TResponse"/> to a 
        /// <see cref="CustomFunctionInvocationResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">
        ///   The response type.
        /// </typeparam>
        /// <param name="innerHandler">
        ///   The inner <see cref="CustomFunctionHandler{TRequest, TResponse}"/>.
        /// </param>
        /// <param name="jsonOptions">
        ///   The JSON serializer options to use when deserializing the request and serializing 
        ///   the response.
        /// </param>
        /// <returns>
        ///   A new <see cref="CustomFunctionHandler"/> delegate.
        /// </returns>
        public static CustomFunctionHandler CreateHandler<TResponse>(CustomFunctionHandler<TResponse> innerHandler, JsonSerializerOptions? jsonOptions = null) {
            return async (context, request, ct) => {
                var result = await innerHandler.Invoke(context, ct).ConfigureAwait(false);
                return new CustomFunctionInvocationResponse() {
                    Body = JsonSerializer.SerializeToElement(result, jsonOptions)
                };
            };
        }


        [LoggerMessage(1, LogLevel.Debug, "Registered custom function '{id}'.")]
        partial void LogFunctionRegistered(Uri id);

        [LoggerMessage(2, LogLevel.Debug, "Unregistered custom function '{id}'.")]
        partial void LogFunctionUnregistered(Uri id);

        [LoggerMessage(3, LogLevel.Trace, "Invoked custom function '{id}'.")]
        partial void LogFunctionInvoked(Uri id);


        /// <summary>
        /// Describes a registered custom function.
        /// </summary>
        private readonly struct CustomFunctionRegistration {

            internal readonly CustomFunctionDescriptorExtended Descriptor;
            
            internal readonly CustomFunctionHandler Handler;

            internal readonly CustomFunctionAuthorizeHandler? Authorize;

            public CustomFunctionRegistration(
                CustomFunctionDescriptorExtended descriptor,
                CustomFunctionHandler handler, 
                CustomFunctionAuthorizeHandler? authorize
            ) {
                Descriptor = descriptor;
                Handler = handler;
                Authorize = authorize;
            }

        }

    }
}
