using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using JsonSchema = Json.Schema;
using Json.Schema.Generation;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// <see cref="ICustomFunctions"/> implementation that allows custom functions to be 
    /// dynamically registered and unregistered at runtime.
    /// </summary>
    public sealed class CustomFunctions : ICustomFunctions {

        /// <summary>
        /// The base URI for custom functions registered using a relative URI.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// JSON options to use when serializing/deserializing.
        /// </summary>
        private readonly JsonSerializerOptions? _jsonOptions;

        /// <summary>
        /// The registered functions.
        /// </summary>
        private readonly Dictionary<Uri, CustomFunctionRegistration> _functions = new Dictionary<Uri, CustomFunctionRegistration>();

        /// <summary>
        /// Lock for accessing <see cref="_functions"/>.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _functionsLock = new Nito.AsyncEx.AsyncReaderWriterLock();

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }


        /// <summary>
        /// Creates a new <see cref="CustomFunctions"/> instance.
        /// </summary>
        /// <param name="baseUri">
        ///   The base URI for custom functions registered with a relative URI.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        public CustomFunctions(Uri baseUri, IBackgroundTaskService? backgroundTaskService = null, JsonSerializerOptions? jsonOptions = null, ILogger? logger = null) {
            if (baseUri == null) {
                throw new ArgumentNullException(nameof(baseUri));
            }
            if (!baseUri.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(baseUri), SharedResources.Error_AbsoluteUriRequired);
            }
            BaseUri = baseUri.EnsurePathHasTrailingSlash();
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            _jsonOptions = jsonOptions;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
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
        private static async ValueTask<bool> IsAuthorizedAsync(IAdapterCallContext context, CustomFunctionRegistration registration, CancellationToken cancellationToken) {
            return registration.Authorize == null || await registration.Authorize.Invoke(context, cancellationToken).ConfigureAwait(false);
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
                var lookupId = descriptor.Id.EnsurePathHasTrailingSlash();
                if (_functions.ContainsKey(lookupId)) {
                    return false;
                }

                var reg = new CustomFunctionRegistration(
                    new CustomFunctionDescriptorExtended() { 
                        Id = descriptor.Id,
                        Name = descriptor.Name,
                        Description = descriptor.Description,
                        RequestSchema = descriptor.RequestSchema,
                        ResponseSchema = descriptor.ResponseSchema
                    },
                    handler, 
                    authorizeHandler
                );

                _functions[lookupId] = reg;
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

            var descriptor = new CustomFunctionDescriptorExtended() {
                Id = id.IsAbsoluteUri 
                    ? id 
                    : new Uri(BaseUri, id),
                Name = name,
                Description = description,
                RequestSchema = CreateJsonSchema<TRequest>(),
                ResponseSchema = CreateJsonSchema<TResponse>()
            };

            return await RegisterFunctionAsync(
                descriptor,
                async (context, request, ct) => {
                    var result = await handler.Invoke(context, request.Body.Deserialize<TRequest>(_jsonOptions)!, ct).ConfigureAwait(false);
                    return new CustomFunctionInvocationResponse() {
                        Body = JsonSerializer.SerializeToElement(result, _jsonOptions)
                    };
                },
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
        ///   The registered function will be assigned an ID derived from the <see cref="BaseUri"/> 
        ///   and the function <paramref name="name"/>.
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
        public async Task<bool> UnregisterFunctionAsync(Uri id, CancellationToken cancellationToken) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            using (await _functionsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                return _functions.Remove(id.EnsurePathHasTrailingSlash());
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
                    funcs = funcs.Where(x => x.Descriptor.Id.ToString().Like(request.Id));
                }
                if (!string.IsNullOrEmpty(request.Name)) {
                    funcs = funcs.Where(x => x.Descriptor.Name.Like(request.Name));
                }
                if (!string.IsNullOrEmpty(request.Description)) {
                    funcs = funcs.Where(x => x.Descriptor.Description != null && x.Descriptor.Description.Like(request.Description));
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

                    result.Add(new CustomFunctionDescriptor() {
                        Id = item.Descriptor.Id,
                        Name = item.Descriptor.Name,
                        Description = item.Descriptor.Description
                    });

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

            using (await _functionsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_functions.TryGetValue(request.Id.EnsurePathHasTrailingSlash(), out var func) || !await IsAuthorizedAsync(context, func, cancellationToken).ConfigureAwait(false)) {
                    return null;
                }

                return new CustomFunctionDescriptorExtended() { 
                    Id = func.Descriptor.Id,
                    Name = func.Descriptor.Name,
                    Description = func.Descriptor.Description,
                    RequestSchema = func.Descriptor.RequestSchema,
                    ResponseSchema = func.Descriptor.ResponseSchema 
                };
            }
        }


        /// <inheritdoc/>
        async Task<CustomFunctionInvocationResponse> ICustomFunctions.InvokeFunctionAsync(IAdapterCallContext context, CustomFunctionInvocationRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);

            CustomFunctionHandler handler;

            using (await _functionsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_functions.TryGetValue(request.Id.EnsurePathHasTrailingSlash(), out var func)) {
                    throw new InvalidOperationException();
                }

                if (!await IsAuthorizedAsync(context, func, cancellationToken).ConfigureAwait(false)) {
                    throw new System.Security.SecurityException();
                }

                handler = func.Handler;
            }

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
        ///   Attributes can be used to customise the schema. In addition to the attributes in the 
        ///   <see cref="JsonSchema.Generation"/> namespace, attributes from the <see cref="System.ComponentModel.DataAnnotations"/> 
        ///   namespace can also be used.
        /// </para>
        /// 
        /// </remarks>
        public JsonElement CreateJsonSchema<T>() {
            Json.Schema.JsonSchemaUtility.RegisterExtensions();
            var builder = new JsonSchema.JsonSchemaBuilder().FromType<T>(new SchemaGeneratorConfiguration() { 
                PropertyNamingMethod = name => _jsonOptions?.PropertyNamingPolicy?.ConvertName(name) ?? name
            });

            return JsonSerializer.SerializeToElement(builder.Build(), _jsonOptions);
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
            var jsonSchema = JsonSchema.JsonSchema.FromText(JsonSerializer.Serialize(schema, jsonOptions));
            var result = jsonSchema.Validate(JsonSerializer.SerializeToNode(data, jsonOptions), new JsonSchema.ValidationOptions() {  
                OutputFormat = JsonSchema.OutputFormat.Detailed
            });
            validationResults = JsonSerializer.SerializeToElement(result, jsonOptions);
            return result.IsValid;
        }


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
