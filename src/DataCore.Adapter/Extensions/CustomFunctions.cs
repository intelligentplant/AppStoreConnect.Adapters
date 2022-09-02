using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

using JsonSchema = Json.Schema;
using JsonSchemaGeneration = Json.Schema.Generation;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// <see cref="ICustomFunctions"/> implementation that allows custom functions to be 
    /// dynamically registered and unregistered at runtime.
    /// </summary>
    public sealed class CustomFunctions : ICustomFunctions {

        private readonly ILogger _logger;

        private readonly Dictionary<Uri, CustomFunctionRegistration> _functions = new Dictionary<Uri, CustomFunctionRegistration>();

        private readonly Nito.AsyncEx.AsyncReaderWriterLock _functionsLock = new Nito.AsyncEx.AsyncReaderWriterLock();

        public IBackgroundTaskService BackgroundTaskService { get; }


        public CustomFunctions(IBackgroundTaskService? backgroundTaskService = null, ILogger<CustomFunctions>? logger = null) {
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            _logger = logger ?? (ILogger) Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }


        private static async ValueTask<bool> IsAuthorizedAsync(IAdapterCallContext context, CustomFunctionRegistration registration, CancellationToken cancellationToken) {
            return registration.Authorize == null || await registration.Authorize.Invoke(context, cancellationToken).ConfigureAwait(false);
        }


        public async Task AddFunctionAsync(
            CustomFunctionDescriptorExtended descriptor,
            CustomFunctionHandler handler, 
            CustomFunctionAuthorizeHandler? authorizeHandler = null, 
            CancellationToken cancellationToken = default
        ) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(handler));
            }
            ValidationExtensions.ValidateObject(descriptor);
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            using (await _functionsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (_functions.ContainsKey(descriptor.Id)) {
                    throw new ArgumentException($"A function with ID '{descriptor.Id}' has already been registered.", nameof(descriptor));
                }

                var reg = new CustomFunctionRegistration(new CustomFunctionDescriptorExtended() { 
                    Id = descriptor.Id,
                    Name = descriptor.Name,
                    Description = descriptor.Description,
                    RequestSchema = descriptor.RequestSchema,
                    ResponseSchema = descriptor.ResponseSchema 
                }, handler, authorizeHandler);
                _functions[reg.Id] = reg;
            }
        }


        public async Task<bool> RemoveFunctionAsync(Uri id, CancellationToken cancellationToken) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            using (await _functionsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                return _functions.Remove(id);
            }
        }


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
                    funcs = funcs.Where(x => x.Id.ToString().Like(request.Id));
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
                if (!_functions.TryGetValue(request.Id, out var func) || !await IsAuthorizedAsync(context, func, cancellationToken).ConfigureAwait(false)) {
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


        async Task<CustomFunctionInvocationResponse> ICustomFunctions.InvokeFunctionAsync(IAdapterCallContext context, CustomFunctionInvocationRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);

            CustomFunctionHandler handler;

            using (await _functionsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_functions.TryGetValue(request.Id, out var func)) {
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
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <remarks>
        /// 
        /// <para>
        ///   <see cref="CreateJsonSchema{T}"/> can be used to simplify generation of request and 
        ///   response schemas for <see cref="CustomFunctionDescriptorExtended"/> instances.
        /// </para>
        /// 
        /// <para>
        ///   Schema generation is performed using <see cref="JsonSchema"/>.
        /// </para>
        /// 
        /// </remarks>
        public static JsonElement CreateJsonSchema<T>() {
            return default;
        }


        private readonly struct CustomFunctionRegistration {

            public Uri Id => Descriptor!.Id;

            public CustomFunctionDescriptorExtended Descriptor { get; }

            public CustomFunctionHandler Handler { get; }

            public CustomFunctionAuthorizeHandler? Authorize { get; }


            public CustomFunctionRegistration(CustomFunctionDescriptorExtended descriptor, CustomFunctionHandler handler, CustomFunctionAuthorizeHandler? authorize) {
                Descriptor = descriptor;
                Handler = handler;
                Authorize = authorize;
            }

        }

    }
}
