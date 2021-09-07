using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Extensions for <see cref="IAdapterExtensionFeature"/>.
    /// </summary>
    public static class AdapterExtensionFeatureExtensions {

        #region [ GetDescriptor / GetOperations Overloads ]

        /// <summary>
        /// Gets the descriptor for the extension feature.
        /// </summary>
        /// <param name="feature">
        ///   The feature.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI that the descriptor is being requested for. This is used as a hint 
        ///   to the implementing type, in case the type implements multiple extension feature 
        ///   contracts.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The extension feature descriptor.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a valid absolute URI.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a child path of <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </exception>
        public static Task<FeatureDescriptor?> GetDescriptor(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            string uriString,
            CancellationToken cancellationToken
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            uri = uri.EnsurePathHasTrailingSlash();

            if (!uri.IsChildOf(WellKnownFeatures.Extensions.BaseUri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uriString));
            }

            return feature.GetDescriptor(context, uri, cancellationToken);
        }



        /// <summary>
        /// Gets the operations that are supported by the extension feature.
        /// </summary>
        /// <param name="feature">
        ///   The feature.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="uriString">
        ///   The feature URI that the operations are being requested for. This is used as a hint 
        ///   to the implementing type, in case the type implements multiple extension feature 
        ///   contracts.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation descriptors.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a valid absolute URI.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not a child path of <see cref="WellKnownFeatures.Extensions.BaseUri"/>.
        /// </exception>
        public static Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            string uriString,
            CancellationToken cancellationToken
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            uri = uri.EnsurePathHasTrailingSlash();

            if (!uri.IsChildOf(WellKnownFeatures.Extensions.BaseUri)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uriString));
            }

            return feature.GetOperations(context, uri, cancellationToken);
        }

        #endregion

        #region [ Invoke Overloads ]

        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T">
        ///   The operation return type.
        /// </typeparam>
        /// <param name="feature">
        ///   The <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the operation.
        /// </param>
        /// <param name="operationId">
        ///   The ID of the operation to call.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use when deserializing the operation 
        ///   result.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationId"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<T?> Invoke<T>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }

            var request = new InvocationRequest() {
                OperationId = operationId,
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results;

            if (result == null || result.Value.ValueKind == JsonValueKind.Undefined || result.Value.ValueKind == JsonValueKind.Null) {
                return default;
            }

            return result!.Value.Deserialize<T>(jsonOptions);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The operation return type.
        /// </typeparam>
        /// <param name="feature">
        ///   The <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the operation.
        /// </param>
        /// <param name="operationId">
        ///   The ID of the operation to call.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use when serializing and deserializing 
        ///   the <paramref name="argument"/> and the operation result.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T2"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationId"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<T2?> Invoke<T1, T2>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument,
            JsonSerializerOptions? jsonOptions = null,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }

            var request = new InvocationRequest() {
                OperationId = operationId,
                Arguments = argument.ToJsonElement(jsonOptions)
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results;

            if (result == null || result.Value.ValueKind == JsonValueKind.Undefined || result.Value.ValueKind == JsonValueKind.Null) {
                return default;
            }

            return result!.Value.Deserialize<T2>(jsonOptions);
        }

        #endregion

        #region [ Stream Overloads ]

        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T">
        ///   The operation return type.
        /// </typeparam>
        /// <param name="feature">
        ///   The <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the operation.
        /// </param>
        /// <param name="operationId">
        ///   The ID of the operation to call.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use when deserializing the operation 
        ///   results.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of each streamed <see cref="InvocationResponse"/> into an instance of 
        ///   <see cref="InvocationResponse"/> into an instance of <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationId"/> is <see langword="null"/>.
        /// </exception>
        public static async IAsyncEnumerable<T?> Stream<T>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            JsonSerializerOptions? jsonOptions = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }

            var request = new InvocationRequest() {
                OperationId = operationId
            };

            await foreach(var item in feature.Stream(context, request, cancellationToken).ConfigureAwait(false)) {
                var result = item?.Results;

                if (result == null || result.Value.ValueKind == JsonValueKind.Undefined || result.Value.ValueKind == JsonValueKind.Null) {
                    yield return default;
                }

                yield return result!.Value.Deserialize<T>(jsonOptions);
            }
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The operation return type.
        /// </typeparam>
        /// <param name="feature">
        ///   The <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the operation.
        /// </param>
        /// <param name="operationId">
        ///   The ID of the operation to call.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use when serializing and deserializing 
        ///   the <paramref name="argument"/> and the operation results.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of each streamed <see cref="InvocationResponse"/> into an instance of 
        ///   <see cref="InvocationResponse"/> into an instance of <typeparamref name="T2"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationId"/> is <see langword="null"/>.
        /// </exception>
        public static async IAsyncEnumerable<T2?> Stream<T1, T2>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument,
            JsonSerializerOptions? jsonOptions = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }

            var request = new InvocationRequest() {
                OperationId = operationId,
                Arguments = argument.ToJsonElement(jsonOptions)
            };

            await foreach (var item in feature.Stream(context, request, cancellationToken).ConfigureAwait(false)) {
                var result = item?.Results;

                if (result == null || result.Value.ValueKind == JsonValueKind.Undefined || result.Value.ValueKind == JsonValueKind.Null) {
                    yield return default;
                }

                yield return result!.Value.Deserialize<T2>(jsonOptions);
            }
        }

        #endregion

        #region [ DuplexStream Overloads ]

        /// <summary>
        /// Calls an extension feature duplex stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The operation return type.
        /// </typeparam>
        /// <param name="feature">
        ///   The <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the operation.
        /// </param>
        /// <param name="operationId">
        ///   The ID of the operation to call.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide arguments to stream to the operation.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use when serializing and deserializing 
        ///   the operations inputs and results.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of each streamed <see cref="InvocationResponse"/> into an instance of 
        ///   <typeparamref name="T2"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="operationId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public static async IAsyncEnumerable<T2?> DuplexStream<T1, T2>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            IAsyncEnumerable<T1?> channel,
            JsonSerializerOptions? jsonOptions = null,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (operationId == null) {
                throw new ArgumentNullException(nameof(operationId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var request = new DuplexStreamInvocationRequest() {
                OperationId = operationId
            };

            var response = feature.DuplexStream(context, request, channel.Transform(x => {
                return new InvocationStreamItem() {
                    Arguments = x.ToJsonElement(jsonOptions)
                };
            }, cancellationToken), cancellationToken);

            await foreach(var item in response.ConfigureAwait(false)) {
                var result = item?.Results;

                if (result == null || result.Value.ValueKind == JsonValueKind.Undefined || result.Value.ValueKind == JsonValueKind.Null) {
                    yield return default;
                }

                yield return result!.Value.Deserialize<T2>(jsonOptions);
            }
        }

        #endregion

    }
}
