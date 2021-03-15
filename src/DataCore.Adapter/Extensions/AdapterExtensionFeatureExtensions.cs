using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Extensions for <see cref="IAdapterExtensionFeature"/>.
    /// </summary>
    public static class AdapterExtensionFeatureExtensions {

        #region [ Helper Methods ]

        /// <summary>
        /// Converts the specified value to a <see cref="Common.Variant"/>, encoding it as an <see cref="EncodedObject"/> 
        /// if it does not map to a standard <see cref="VariantType"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the object to convert.
        /// </typeparam>
        /// <param name="feature">
        ///   The <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <param name="value">
        ///   The value to convert.
        /// </param>
        /// <returns>
        ///   The converted value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <typeparamref name="T"/> is not directly assignable to <see cref="Common.Variant"/>, and the 
        ///   <paramref name="feature"/> does not define an <see cref="IObjectEncoder"/> that is 
        ///   capable of encoding the object.
        /// </exception>
        public static Variant ConvertToVariant<T>(this IAdapterExtensionFeature feature, T? value) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            var targetType = typeof(T);
            if (targetType == typeof(Variant) || Variant.TryGetVariantType(targetType, out var _)) {
                return Variant.FromValue(value);
            }

            if (targetType.IsArray) {
                var elementType = targetType.GetElementType();
                if (elementType == typeof(Variant) || Variant.TryGetVariantType(elementType, out var _)) {
                    return Variant.FromValue(value);
                }

                return new Variant(feature.Encoders.Encode((Array?) (object?) value));

            }
            else {
                return new Variant(feature.Encoders.Encode(value));
            }
        }


        /// <summary>
        /// Converts the specified <see cref="Common.Variant"/> to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to convert the <see cref="Common.Variant"/> to.
        /// </typeparam>
        /// <param name="feature">
        ///   The <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <param name="value">
        ///   The object to convert.
        /// </param>
        /// <returns>
        ///   The converted value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/> contains an <see cref="EncodedObject"/> as its value, and 
        ///   the <paramref name="feature"/> does not contain an <see cref="IObjectEncoder"/> that 
        ///   is capable of decoding the value.
        /// </exception>
        public static T? ConvertFromVariant<T>(this IAdapterExtensionFeature feature, Variant value) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return feature.Encoders.Decode<T>(value);
        }

        #endregion

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
            CancellationToken cancellationToken
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
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T>(result.Value);
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
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T2>(result.Value);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T3"/>.
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
        public static async Task<T3?> Invoke<T1, T2, T3>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T3>(result.Value);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T4"/>.
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
        public static async Task<T4?> Invoke<T1, T2, T3, T4>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T4>(result.Value);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T5"/>.
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
        public static async Task<T5?> Invoke<T1, T2, T3, T4, T5>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T5>(result.Value);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T6"/>.
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
        public static async Task<T6?> Invoke<T1, T2, T3, T4, T5, T6>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T6>(result.Value);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation argument type.
        /// </typeparam>
        /// <typeparam name="T7">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="argument6">
        ///   The sixth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T7"/>.
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
        public static async Task<T7?> Invoke<T1, T2, T3, T4, T5, T6, T7>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            T6? argument6,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5),
                    feature.ConvertToVariant(argument6)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T7>(result.Value);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation argument type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation argument type.
        /// </typeparam>
        /// <typeparam name="T8">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="argument6">
        ///   The sixth operation argument.
        /// </param>
        /// <param name="argument7">
        ///   The seventh operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T8"/>.
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
        public static async Task<T8?> Invoke<T1, T2, T3, T4, T5, T6, T7, T8>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            T6? argument6,
            T7? argument7,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5),
                    feature.ConvertToVariant(argument6),
                    feature.ConvertToVariant(argument7)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T8>(result.Value);
        }


        /// <summary>
        /// Calls an extension feature invoke operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation argument type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation argument type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The eighth operation argument type.
        /// </typeparam>
        /// <typeparam name="T9">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="argument6">
        ///   The sixth operation argument.
        /// </param>
        /// <param name="argument7">
        ///   The seventh operation argument.
        /// </param>
        /// <param name="argument8">
        ///   The eighth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will convert the first <see cref="InvocationResponse.Results"/> 
        ///   entry of the <see cref="InvocationResponse"/> into an instance of <typeparamref name="T9"/>.
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
        public static async Task<T9?> Invoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            T6? argument6,
            T7? argument7,
            T8? argument8,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5),
                    feature.ConvertToVariant(argument6),
                    feature.ConvertToVariant(argument7),
                    feature.ConvertToVariant(argument8)
                }
            };

            var response = await feature.Invoke(context, request, cancellationToken).ConfigureAwait(false);
            var result = response?.Results?.FirstOrDefault();

            if (!result.HasValue || result.Value.IsNull()) {
                return default;
            }

            return feature.ConvertFromVariant<T9>(result.Value);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T"/>.
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
        public static async Task<ChannelReader<T?>> Stream<T>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            CancellationToken cancellationToken
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

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T2"/>.
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
        public static async Task<ChannelReader<T2?>> Stream<T1, T2>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T2>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T3"/>.
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
        public static async Task<ChannelReader<T3?>> Stream<T1, T2, T3>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T3>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T4"/>.
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
        public static async Task<ChannelReader<T4?>> Stream<T1, T2, T3, T4>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T4>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T5"/>.
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
        public static async Task<ChannelReader<T5?>> Stream<T1, T2, T3, T4, T5>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T5>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T6"/>.
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
        public static async Task<ChannelReader<T6?>> Stream<T1, T2, T3, T4, T5, T6>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T6>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation argument type.
        /// </typeparam>
        /// <typeparam name="T7">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="argument6">
        ///   The sixth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T7"/>.
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
        public static async Task<ChannelReader<T7?>> Stream<T1, T2, T3, T4, T5, T6, T7>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            T6? argument6,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5),
                    feature.ConvertToVariant(argument6)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T7>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation argument type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation argument type.
        /// </typeparam>
        /// <typeparam name="T8">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="argument6">
        ///   The sixth operation argument.
        /// </param>
        /// <param name="argument7">
        ///   The seventh operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T8"/>.
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
        public static async Task<ChannelReader<T8?>> Stream<T1, T2, T3, T4, T5, T6, T7, T8>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            T6? argument6,
            T7? argument7,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5),
                    feature.ConvertToVariant(argument6),
                    feature.ConvertToVariant(argument7)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T8>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


        /// <summary>
        /// Calls an extension feature stream operation.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first operation argument type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second operation argument type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third operation argument type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth operation argument type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth operation argument type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth operation argument type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh operation argument type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The eighth operation argument type.
        /// </typeparam>
        /// <typeparam name="T9">
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
        /// <param name="argument1">
        ///   The first operation argument.
        /// </param>
        /// <param name="argument2">
        ///   The second operation argument.
        /// </param>
        /// <param name="argument3">
        ///   The third operation argument.
        /// </param>
        /// <param name="argument4">
        ///   The fourth operation argument.
        /// </param>
        /// <param name="argument5">
        ///   The fifth operation argument.
        /// </param>
        /// <param name="argument6">
        ///   The sixth operation argument.
        /// </param>
        /// <param name="argument7">
        ///   The seventh operation argument.
        /// </param>
        /// <param name="argument8">
        ///   The eighth operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T9"/>.
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
        public static async Task<ChannelReader<T9?>> Stream<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument1,
            T2? argument2,
            T3? argument3,
            T4? argument4,
            T5? argument5,
            T6? argument6,
            T7? argument7,
            T8? argument8,
            CancellationToken cancellationToken
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
                Arguments = new[] {
                    feature.ConvertToVariant(argument1),
                    feature.ConvertToVariant(argument2),
                    feature.ConvertToVariant(argument3),
                    feature.ConvertToVariant(argument4),
                    feature.ConvertToVariant(argument5),
                    feature.ConvertToVariant(argument6),
                    feature.ConvertToVariant(argument7),
                    feature.ConvertToVariant(argument8)
                }
            };

            var response = await feature.Stream(context, request, cancellationToken).ConfigureAwait(false);

            return response.Transform(x => { 
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T9>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T2"/>.
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
        public static async Task<ChannelReader<T2?>> DuplexStream<T1, T2>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            ChannelReader<T1?> channel,
            CancellationToken cancellationToken
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

            var request = new InvocationRequest() {
                OperationId = operationId
            };

            var response = await feature.DuplexStream(context, request, channel.Transform(x => {
                return new InvocationStreamItem() {
                    Arguments = new[] {
                        feature.ConvertToVariant(x),
                    }
                };
            }, feature.BackgroundTaskService, cancellationToken), cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T2>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }


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
        /// <param name="argument">
        ///   The initial operation argument.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide additional arguments to stream to the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="ChannelReader{T}"/> that 
        ///   will convert the first <see cref="InvocationResponse.Results"/> entry of each 
        ///   streamed <see cref="InvocationResponse"/> into an instance of <typeparamref name="T2"/>.
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
        public static async Task<ChannelReader<T2?>> DuplexStream<T1, T2>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            T1? argument,
            ChannelReader<T1?> channel,
            CancellationToken cancellationToken
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

            var request = new InvocationRequest() {
                OperationId = operationId,
                Arguments = new[] {
                    feature.ConvertToVariant(argument),
                }
            };

            var response = await feature.DuplexStream(context, request, channel.Transform(x => {
                return new InvocationStreamItem() {
                    Arguments = new[] {
                        feature.ConvertToVariant(x),
                    }
                };
            }, feature.BackgroundTaskService, cancellationToken), cancellationToken).ConfigureAwait(false);

            return response.Transform(x => {
                var result = x.Results?.FirstOrDefault();
                if (!result.HasValue || result.Value.IsNull()) {
                    return default;
                }

                return feature.ConvertFromVariant<T2>(result.Value);
            }, feature.BackgroundTaskService, cancellationToken);
        }

        #endregion

    }
}
