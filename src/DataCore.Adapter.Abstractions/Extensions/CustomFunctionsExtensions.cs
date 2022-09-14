using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Extensions for <see cref="ICustomFunctions"/>.
    /// </summary>
    public static class CustomFunctionsExtensions {

        /// <summary>
        /// Invokes a custom function.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request body type.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response body type.
        /// </typeparam>
        /// <param name="customFunctions">
        ///   The <see cref="ICustomFunctions"/> instance.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="functionId">
        ///   The ID of the custom function to invoke.
        /// </param>
        /// <param name="request">
        ///   The request body.
        /// </param>
        /// <param name="jsonOptions">
        ///   The JSON serializer options to use.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="customFunctions"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<TResponse?> InvokeFunctionAsync<TRequest, TResponse>(
            this ICustomFunctions customFunctions,
            IAdapterCallContext context,
            Uri functionId,
            TRequest request,
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default
        ) {
            if (customFunctions == null) {
                throw new ArgumentNullException(nameof(customFunctions));
            }

            var result = await customFunctions.InvokeFunctionAsync(
                context,
                CustomFunctionInvocationRequest.Create(functionId, request, jsonOptions),
                cancellationToken
            ).ConfigureAwait(false);

            return result.Body == null 
                ? default 
                : result.Body.Value.Deserialize<TResponse>(jsonOptions);
        }


        /// <summary>
        /// Invokes a custom function.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request body type.
        /// </typeparam>
        /// <typeparam name="TResponse">
        ///   The response body type.
        /// </typeparam>
        /// <param name="customFunctions">
        ///   The <see cref="ICustomFunctions"/> instance.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="functionId">
        ///   The ID of the custom function to invoke.
        /// </param>
        /// <param name="request">
        ///   The request body.
        /// </param>
        /// <param name="requestTypeInfo">
        ///   The JSON type information for <typeparamref name="TRequest"/>.
        /// </param>
        /// <param name="responseTypeInfo">
        ///   The JSON type information for <typeparamref name="TResponse"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="customFunctions"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<TResponse?> InvokeFunctionAsync<TRequest, TResponse>(
            this ICustomFunctions customFunctions,
            IAdapterCallContext context,
            Uri functionId,
            TRequest request,
            JsonTypeInfo<TRequest> requestTypeInfo,
            JsonTypeInfo<TResponse> responseTypeInfo,
            CancellationToken cancellationToken = default
        ) {
            if (customFunctions == null) {
                throw new ArgumentNullException(nameof(customFunctions));
            }

            var result = await customFunctions.InvokeFunctionAsync(
                context,
                CustomFunctionInvocationRequest.Create(functionId, request, requestTypeInfo),
                cancellationToken
            ).ConfigureAwait(false);

            return result.Body == null 
                ? default 
                : result.Body.Value.Deserialize(responseTypeInfo);
        }

    }
}
