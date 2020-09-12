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

        #region [ Private Helper Methods ]

        /// <summary>
        /// Invokes an extension method and converts input and output values to/from JSON.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="deserialize">
        ///   A delegate that will deserialize the JSON output to an instance of <typeparamref name="TOut"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        private static async Task<TOut> InvokeInternal<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TIn argument,
            Func<string, TOut> deserialize,
            CancellationToken cancellationToken
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            var result = await feature.Invoke(
                context,
                operationId,
                AdapterExtensionFeature.SerializeObject(argument),
                cancellationToken
            ).ConfigureAwait(false);

            return deserialize(result!);
        }


        /// <summary>
        /// Invokes a streaming method and converts the input and output values to/from JSON.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="deserialize">
        ///   A delegate that will deserialize the JSON output to an instance of <typeparamref name="TOut"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        private static async Task<ChannelReader<TOut>> StreamInternal<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TIn argument,
            Func<string, TOut> deserialize,
            CancellationToken cancellationToken
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            var result = await feature.Stream(
                context,
                operationId,
                AdapterExtensionFeature.SerializeObject(argument),
                cancellationToken
            ).ConfigureAwait(false);

            var channel = Channel.CreateUnbounded<TOut>();

            _ = Task.Run(async () => {
                try {
                    while (await result.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        while (result.TryRead(out var val)) {
                            if (val == null) {
                                continue;
                            }
                            channel.Writer.TryWrite(deserialize(val));
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception e) {
                    channel.Writer.TryComplete(e);
                }
                finally {
                    channel.Writer.TryComplete();
                }
            }, cancellationToken);

            return channel.Reader;
        }


        /// <summary>
        /// Invokes a duplex streaming method and converts the input and output values to/from JSON.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="channel">
        ///   A channel that will stream the input values for the operation.
        /// </param>
        /// <param name="deserialize">
        ///   A delegate that will deserialize the JSON output to an instance of <typeparamref name="TOut"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        private static async Task<ChannelReader<TOut>> DuplexStreamInternal<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            ChannelReader<TIn> channel,
            Func<string, TOut> deserialize,
            CancellationToken cancellationToken
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var inChannel = Channel.CreateUnbounded<string>();
            var outChannel = Channel.CreateUnbounded<TOut>();

            var result = await feature.DuplexStream(
                context,
                operationId,
                inChannel,
                cancellationToken
            ).ConfigureAwait(false);

            _ = Task.Run(async () => {
                // Process input channel
                try {
                    while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        while (channel.TryRead(out var val)) {
                            inChannel.Writer.TryWrite(AdapterExtensionFeature.SerializeObject(val));
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception e) {
                    inChannel.Writer.TryComplete(e);
                }
                finally {
                    inChannel.Writer.TryComplete();
                }
            }, cancellationToken);

            _ = Task.Run(async () => {
                // Process output channel
                try {
                    while (await result.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        while (result.TryRead(out var val)) {
                            if (val == null) {
                                continue;
                            }
                            outChannel.Writer.TryWrite(deserialize(val));
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception e) {
                    outChannel.Writer.TryComplete(e);
                }
                finally {
                    outChannel.Writer.TryComplete();
                }
            }, cancellationToken);

            return outChannel.Reader;
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
        ///   <paramref name="uriString"/> is not a child path of <see cref="WellKnownFeatures.Extensions.ExtensionFeatureBasePath"/>.
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

            uri = UriExtensions.EnsurePathHasTrailingSlash(uri);

            if (!uri.IsChildOf(WellKnownFeatures.Extensions.ExtensionFeatureBasePath)) {
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
        ///   <paramref name="uriString"/> is not a child path of <see cref="WellKnownFeatures.Extensions.ExtensionFeatureBasePath"/>.
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

            uri = UriExtensions.EnsurePathHasTrailingSlash(uri);

            if (!uri.IsChildOf(WellKnownFeatures.Extensions.ExtensionFeatureBasePath)) {
                throw new ArgumentException(SharedResources.Error_InvalidUri, nameof(uriString));
            }

            return feature.GetOperations(context, uri, cancellationToken);
        }

        #endregion

        #region [ Invoke Overloads ]

        /// <summary>
        /// Invokes an extension method and converts input and output values to/from JSON.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        public static Task<TOut> Invoke<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TIn argument,
            CancellationToken cancellationToken
        ) {
            return InvokeInternal(
                feature,
                context,
                operationId,
                argument,
                AdapterExtensionFeature.DeserializeObject<TOut>,
                cancellationToken
            );
        }


        /// <summary>
        /// Invokes an extension method and converts input and output values to/from JSON, using 
        /// an anonymous type to determine the shape of the output value.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="anonymousTypeDefinition">
        ///   The anonymous type instance used to convert output values.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="anonymousTypeDefinition"/> is <see langword="null"/>.
        /// </exception>
        public static Task<TOut> InvokeWithAnonymousResultType<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TIn argument,
            TOut anonymousTypeDefinition,
            CancellationToken cancellationToken
        ) {
            if (anonymousTypeDefinition == null) {
                throw new ArgumentNullException(nameof(anonymousTypeDefinition));
            }

            return InvokeInternal(
                feature,
                context,
                operationId,
                argument,
                json => AdapterExtensionFeature.DeserializeAnonymousType(json, anonymousTypeDefinition),
                cancellationToken
            );
        }


        /// <summary>
        /// Invokes an extension method and converts output values from JSON.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        public static Task<TOut> Invoke<TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            CancellationToken cancellationToken
        ) {
            return InvokeInternal(
                feature,
                context,
                operationId,
                (object) null!,
                AdapterExtensionFeature.DeserializeObject<TOut>,
                cancellationToken
            );
        }


        /// <summary>
        /// Invokes an extension method and converts output values from JSON, using an anonymous 
        /// type to determine the shape of the output value.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="anonymousTypeDefinition">
        ///   The anonymous type instance used to convert output values.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="anonymousTypeDefinition"/> is <see langword="null"/>.
        /// </exception>
        public static Task<TOut> InvokeWithAnonymousResultType<TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TOut anonymousTypeDefinition,
            CancellationToken cancellationToken
        ) {
            if (anonymousTypeDefinition == null) {
                throw new ArgumentNullException(nameof(anonymousTypeDefinition));
            }

            return InvokeInternal(
                feature,
                context,
                operationId,
                (object) null!,
                json => AdapterExtensionFeature.DeserializeAnonymousType(json, anonymousTypeDefinition),
                cancellationToken
            );
        }

        #endregion

        #region [ Stream Overloads ]

        /// <summary>
        /// Invokes a streaming method and converts the input and output values to/from JSON.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        public static Task<ChannelReader<TOut>> Stream<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TIn argument,
            CancellationToken cancellationToken
        ) {
            return StreamInternal(
                feature,
                context,
                operationId,
                argument,
                AdapterExtensionFeature.DeserializeObject<TOut>,
                cancellationToken
            );
        }


        /// <summary>
        /// Invokes a streaming method and converts the input and output values to/from JSON, using 
        /// an anonymous type to determine the shape of the output values.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="argument">
        ///   The operation argument.
        /// </param>
        /// <param name="anonymousTypeDefinition">
        ///   The anonymous type instance used to convert output values.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="anonymousTypeDefinition"/> is <see langword="null"/>.
        /// </exception>
        public static Task<ChannelReader<TOut>> StreamWithAnonymousResultType<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TIn argument,
            TOut anonymousTypeDefinition,
            CancellationToken cancellationToken
        ) {
            if (anonymousTypeDefinition == null) {
                throw new ArgumentNullException(nameof(anonymousTypeDefinition));
            }

            return StreamInternal(
                feature,
                context,
                operationId,
                argument,
                json => AdapterExtensionFeature.DeserializeAnonymousType(json, anonymousTypeDefinition),
                cancellationToken
            );
        }


        /// <summary>
        /// Invokes a streaming method and converts the output values from JSON.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        public static Task<ChannelReader<TOut>> Stream<TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            CancellationToken cancellationToken
        ) {
            return StreamInternal(
                feature,
                context,
                operationId,
                (object) null!,
                AdapterExtensionFeature.DeserializeObject<TOut>,
                cancellationToken
            );
        }


        /// <summary>
        /// Invokes a streaming method and converts the output values from JSON, using an anonymous 
        /// type to determine the shape of the output values.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="anonymousTypeDefinition">
        ///   The anonymous type instance used to convert output values.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="anonymousTypeDefinition"/> is <see langword="null"/>.
        /// </exception>
        public static Task<ChannelReader<TOut>> StreamWithAnonymousResultType<TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            TOut anonymousTypeDefinition,
            CancellationToken cancellationToken
        ) {
            if (anonymousTypeDefinition == null) {
                throw new ArgumentNullException(nameof(anonymousTypeDefinition));
            }

            return StreamInternal(
                feature,
                context,
                operationId,
                (object) null!,
                json => AdapterExtensionFeature.DeserializeAnonymousType(json, anonymousTypeDefinition),
                cancellationToken
            );
        }

        #endregion

        #region [ DuplexStream Overloads ]

        /// <summary>
        /// Invokes a duplex streaming method and converts the input and output values to/from JSON.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="channel">
        ///   A channel that will stream the inputs for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public static Task<ChannelReader<TOut>> DuplexStream<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            ChannelReader<TIn> channel,
            CancellationToken cancellationToken
        ) {
            return DuplexStreamInternal(
                feature,
                context,
                operationId,
                channel,
                AdapterExtensionFeature.DeserializeObject<TOut>,
                cancellationToken
            );
        }


        /// <summary>
        /// Invokes a duplex streaming method and converts the output values from JSON, using an 
        /// anonymous type to determine the shape of the output values.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature to call.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="operationId">
        ///   The URI for the operation.
        /// </param>
        /// <param name="channel">
        ///   A channel that will stream the inputs for the operation.
        /// </param>
        /// <param name="anonymousTypeDefinition">
        ///   The anonymous type instance used to convert output values.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will stream the results of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="anonymousTypeDefinition"/> is <see langword="null"/>.
        /// </exception>
        public static Task<ChannelReader<TOut>> DuplexStreamWithAnonymousResultType<TIn, TOut>(
            this IAdapterExtensionFeature feature,
            IAdapterCallContext context,
            Uri operationId,
            ChannelReader<TIn> channel,
            TOut anonymousTypeDefinition,
            CancellationToken cancellationToken
        ) {
            if (anonymousTypeDefinition == null) {
                throw new ArgumentNullException(nameof(anonymousTypeDefinition));
            }

            return DuplexStreamInternal(
                feature,
                context,
                operationId,
                channel,
                json => AdapterExtensionFeature.DeserializeAnonymousType(json, anonymousTypeDefinition),
                cancellationToken
            );
        }

        #endregion

    }
}
