using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {
    public partial class AdapterExtensionFeature {

        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The return type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindInvoke<TIn, TOut>(
            Func<IAdapterCallContext, TIn, CancellationToken, Task<TOut>> handler,
            TIn inputParameterExample = default,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Invoke;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundInvokeMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
                operationType,
                operationId,
                name,
                description,
                true,
                inputParameterDescription,
                inputParameterExample,
                true,
                returnParameterDescription,
                returnParameterExample
            );

            _boundInvokeMethods[operationId] = async (ctx, json, ct) => {
                var inArg = DeserializeObject<TIn>(json);
                var result = await handler(ctx, inArg, ct).ConfigureAwait(false);
                return SerializeObject(result);
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The return type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindInvoke<TIn, TOut>(
            Func<IAdapterCallContext, TIn, TOut> handler,
            TIn inputParameterExample = default,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Invoke;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundInvokeMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
                operationType,
                operationId,
                name,
                description,
                true,
                inputParameterDescription,
                inputParameterExample,
                true,
                returnParameterDescription,
                returnParameterExample
            );

            _boundInvokeMethods[operationId] = (ctx, json, ct) => {
                var inArg = DeserializeObject<TIn>(json);
                var result = handler(ctx, inArg);
                return Task.FromResult(SerializeObject(result));
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The return type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindInvoke<TIn, TOut>(
            Func<TIn, TOut> handler,
            TIn inputParameterExample = default,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Invoke;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundInvokeMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
                operationType,
                operationId,
                name,
                description,
                true,
                inputParameterDescription,
                inputParameterExample,
                true,
                returnParameterDescription,
                returnParameterExample
            );

            _boundInvokeMethods[operationId] = (ctx, json, ct) => {
                var inArg = DeserializeObject<TIn>(json);
                var result = handler(inArg);
                return Task.FromResult(SerializeObject(result));
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The return type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindInvoke<TOut>(
            Func<IAdapterCallContext, CancellationToken, Task<TOut>> handler,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Invoke;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundInvokeMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
                operationType,
                operationId,
                name,
                description,
                false,
                null!,
                new object(),
                true,
                returnParameterDescription,
                returnParameterExample
            );

            _boundInvokeMethods[operationId] = async (ctx, json, ct) => {
                var result = await handler(ctx, ct).ConfigureAwait(false);
                return SerializeObject(result);
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The return type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindInvoke<TOut>(
            Func<IAdapterCallContext, TOut> handler,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Invoke;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundInvokeMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
                operationType,
                operationId,
                name,
                description,
                false,
                null!,
                (object) null!,
                true,
                returnParameterDescription,
                returnParameterExample
            );

            _boundInvokeMethods[operationId] = (ctx, json, ct) => {
                var result = handler(ctx);
                return Task.FromResult(SerializeObject(result));
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Invoke"/>.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The return type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindInvoke<TOut>(
            Func<TOut> handler,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Invoke;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundInvokeMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
                operationType,
                operationId,
                name,
                description,
                false,
                null!,
                (object) null!,
                true,
                returnParameterDescription,
                returnParameterExample
            );

            _boundInvokeMethods[operationId] = (ctx, json, ct) => {
                var result = handler();
                return Task.FromResult(SerializeObject(result));
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output stream type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindStream<TIn, TOut>(
            Func<IAdapterCallContext, TIn, CancellationToken, Task<ChannelReader<TOut>>> handler,
            TIn inputParameterExample = default,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Stream;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundStreamMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
               operationType,
               operationId,
               name,
               description,
               true,
               inputParameterDescription,
               inputParameterExample,
               true,
               returnParameterDescription,
               returnParameterExample
           );

            _boundStreamMethods[operationId] = async (ctx, json, ct) => {
                var inArg = DeserializeObject<TIn>(json);
                var outChannel = await handler(ctx, inArg, ct).ConfigureAwait(false);
                var result = Channel.CreateUnbounded<string>();

                outChannel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await result.Writer.WriteAsync(SerializeObject(val), ct2).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                    catch (Exception e) {
                        result.Writer.TryComplete(e);
                    }
                    finally {
                        result.Writer.TryComplete();
                    }
                }, BackgroundTaskService, ct);

                return result.Reader;
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output stream type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindStream<TIn, TOut>(
            Func<TIn, CancellationToken, Task<ChannelReader<TOut>>> handler,
            TIn inputParameterExample = default,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Stream;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundStreamMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
               operationType,
               operationId,
               name,
               description,
               true,
               inputParameterDescription,
               inputParameterExample,
               true,
               returnParameterDescription,
               returnParameterExample
           );

            _boundStreamMethods[operationId] = async (ctx, json, ct) => {
                var inArg = DeserializeObject<TIn>(json);
                var outChannel = await handler(inArg, ct).ConfigureAwait(false);
                var result = Channel.CreateUnbounded<string>();

                outChannel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await result.Writer.WriteAsync(SerializeObject(val), ct2).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                    catch (Exception e) {
                        result.Writer.TryComplete(e);
                    }
                    finally {
                        result.Writer.TryComplete();
                    }
                }, BackgroundTaskService, ct);

                return result.Reader;
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The output stream type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindStream<TOut>(
            Func<IAdapterCallContext, CancellationToken, Task<ChannelReader<TOut>>> handler,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Stream;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundStreamMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
               operationType,
               operationId,
               name,
               description,
               false,
               null!,
               (object) null!,
               true,
               returnParameterDescription,
               returnParameterExample
           );

            _boundStreamMethods[operationId] = async (ctx, json, ct) => {
                var outChannel = await handler(ctx, ct).ConfigureAwait(false);
                var result = Channel.CreateUnbounded<string>();

                outChannel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await result.Writer.WriteAsync(SerializeObject(val), ct2).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                    catch (Exception e) {
                        result.Writer.TryComplete(e);
                    }
                    finally {
                        result.Writer.TryComplete();
                    }
                }, BackgroundTaskService, ct);

                return result.Reader;
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.Stream"/>.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The output stream type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindStream<TOut>(
            Func<CancellationToken, Task<ChannelReader<TOut>>> handler,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.Stream;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundStreamMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
               operationType,
               operationId,
               name,
               description,
               false,
               null!,
               (object) null!,
               true,
               returnParameterDescription,
               returnParameterExample
           );

            _boundStreamMethods[operationId] = async (ctx, json, ct) => {
                var outChannel = await handler(ct).ConfigureAwait(false);
                var result = Channel.CreateUnbounded<string>();

                outChannel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await result.Writer.WriteAsync(SerializeObject(val), ct2).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                    catch (Exception e) {
                        result.Writer.TryComplete(e);
                    }
                    finally {
                        result.Writer.TryComplete();
                    }
                }, BackgroundTaskService, ct);

                return result.Reader;
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.DuplexStream"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output parameter type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindDuplexStream<TIn, TOut>(
            Func<IAdapterCallContext, ChannelReader<TIn>, CancellationToken, Task<ChannelReader<TOut>>> handler,
            TIn inputParameterExample = default,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.DuplexStream;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundDuplexStreamMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
               operationType,
               operationId,
               name,
               description,
               true,
               inputParameterDescription,
               inputParameterExample,
               true,
               returnParameterDescription,
               returnParameterExample
           );

            _boundDuplexStreamMethods[operationId] = async (ctx, channel, ct) => {
                var inChannel = Channel.CreateUnbounded<TIn>();
                var outChannel = await handler(ctx, inChannel, ct).ConfigureAwait(false);
                var result = Channel.CreateUnbounded<string>();

                channel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await inChannel.Writer.WriteAsync(DeserializeObject<TIn>(val), ct2).ConfigureAwait(false);
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
                }, BackgroundTaskService, ct);

                outChannel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await result.Writer.WriteAsync(SerializeObject(val), ct2).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                    catch (Exception e) {
                        result.Writer.TryComplete(e);
                    }
                    finally {
                        result.Writer.TryComplete();
                    }
                }, BackgroundTaskService, ct);

                return result.Reader;
            };

            return true;
        }


        /// <summary>
        /// Binds an extension feature operation with a type of <see cref="ExtensionFeatureOperationType.DuplexStream"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input parameter type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output parameter type.
        /// </typeparam>
        /// <param name="handler">
        ///   The handler for the operation.
        /// </param>
        /// <param name="inputParameterExample">
        ///   An example value for the input parameter.
        /// </param>
        /// <param name="returnParameterExample">
        ///   An example value for the return parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the operation was successfully bound, or <see langword="false"/>
        ///   if an operation with the same URI has already been bound.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="handler"/> is <see langword="null"/>.
        /// </exception>
        protected bool BindDuplexStream<TIn, TOut>(
            Func<ChannelReader<TIn>, CancellationToken, Task<ChannelReader<TOut>>> handler,
            TIn inputParameterExample = default,
            TOut returnParameterExample = default
        ) {
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }

            var operationType = ExtensionFeatureOperationType.DuplexStream;

            if (!TryGetOperationDetailsFromMemberInfo(
                handler.Method,
                operationType,
                out var operationId,
                out var name,
                out var description,
                out var inputParameterDescription,
                out var returnParameterDescription
            )) {
                return false;
            }

            if (_boundDescriptors.ContainsKey(operationId) || _boundDuplexStreamMethods.ContainsKey(operationId)) {
                return false;
            }

            _boundDescriptors[operationId] = CreateDescriptor(
               operationType,
               operationId,
               name,
               description,
               true,
               inputParameterDescription,
               inputParameterExample,
               true,
               returnParameterDescription,
               returnParameterExample
           );

            _boundDuplexStreamMethods[operationId] = async (ctx, channel, ct) => {
                var inChannel = Channel.CreateUnbounded<TIn>();
                var outChannel = await handler(inChannel, ct).ConfigureAwait(false);
                var result = Channel.CreateUnbounded<string>();

                channel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await inChannel.Writer.WriteAsync(DeserializeObject<TIn>(val), ct2).ConfigureAwait(false);
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
                }, BackgroundTaskService, ct);

                outChannel.RunBackgroundOperation(async (ch, ct2) => {
                    try {
                        while (!ct2.IsCancellationRequested) {
                            var val = await ch.ReadAsync(ct2).ConfigureAwait(false);
                            await result.Writer.WriteAsync(SerializeObject(val), ct2).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (ChannelClosedException) { }
                    catch (Exception e) {
                        result.Writer.TryComplete(e);
                    }
                    finally {
                        result.Writer.TryComplete();
                    }
                }, BackgroundTaskService, ct);

                return result.Reader;
            };

            return true;
        }

    }
}
