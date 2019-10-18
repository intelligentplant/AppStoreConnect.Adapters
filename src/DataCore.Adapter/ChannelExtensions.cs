using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="Channel{T}"/>, <see cref="ChannelReader{T}"/>, and 
    /// <see cref="ChannelWriter{T}"/>.
    /// </summary>
    public static class ChannelExtensions {

        /// <summary>
        /// Capacity of channels created using <see cref="CreateTagDefinitionChannel"/>.
        /// </summary>
        public const int TagDefinitionChannelCapacity = 100;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateTagValueChannel{T}"/>.
        /// </summary>
        public const int TagValueChannelCapacity = 5000;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateTagValueAnnotationChannel"/>.
        /// </summary>
        public const int TagAnnotationChannelCapacity = 100;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateEventMessageChannel{T}(int)"/>.
        /// </summary>
        public const int EventChannelCapacity = 100;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateAssetModelNodeChannel"/>.
        /// </summary>
        public const int AssetModelNodeChannelCapacity = 100;


        /// <summary>
        /// Creates a bounded or unbounded channel that is optimised for a single reader/single writer 
        /// scenario and allows synchronous continuations.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel type.
        /// </typeparam>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="fullMode">
        ///   The action to take if a write is attempted on a full channel. Ignored if 
        ///   <paramref name="capacity"/> is less than one.
        /// </param>
        /// <returns>
        ///   The new channel.
        /// </returns>
        internal static Channel<T> CreateChannel<T>(int capacity, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait) {
            return capacity > 0
                ? Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
                    FullMode = fullMode,
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true
                })
                : Channel.CreateUnbounded<T>(new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true
                });
        }


        /// <summary>
        /// Converts the <see cref="IEnumerable{T}"/> to a <see cref="ChannelReader{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The item type.
        /// </typeparam>
        /// <param name="items">
        ///   The items to write to the channel.
        /// </param>
        /// <returns>
        ///   A <see cref="ChannelReader{T}"/> that can be used to read the emitted items.
        /// </returns>
        public static ChannelReader<T> ToChannel<T>(this IEnumerable<T> items) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            
            var result = CreateChannel<T>(-1);
            try {
                foreach (var item in items) {
                    result.Writer.TryWrite(item);
                }
            }
            catch (Exception e) {
                result.Writer.TryComplete(e);
            }
            finally {
                result.Writer.TryComplete();
            }

            return result;
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag search queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagDefinitionChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.TagDefinition> CreateTagDefinitionChannel(int capacity = TagDefinitionChannelCapacity) {
            return CreateChannel<RealTimeData.TagDefinition>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag identifier queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagDefinitionChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.TagIdentifier> CreateTagIdentifierChannel(int capacity = TagDefinitionChannelCapacity) {
            return CreateChannel<RealTimeData.TagIdentifier>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag value queries.
        /// </summary>
        /// <typeparam name="T">
        ///   The result type.
        /// </typeparam>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<T> CreateTagValueChannel<T>(int capacity = TagValueChannelCapacity) where T : RealTimeData.TagValueQueryResult {
            return CreateChannel<T>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to write tag values to an adapter.
        /// </summary>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.WriteTagValueItem> CreateTagValueWriteChannel(int capacity = TagValueChannelCapacity) {
            return CreateChannel<RealTimeData.WriteTagValueItem>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to return the results of tag value writes.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.WriteTagValueResult> CreateTagValueWriteResultChannel(int capacity = TagValueChannelCapacity) {
            return CreateChannel<RealTimeData.WriteTagValueResult>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag annotation queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagAnnotationChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.TagValueAnnotationQueryResult> CreateTagValueAnnotationChannel(int capacity = TagAnnotationChannelCapacity) {
            return CreateChannel<RealTimeData.TagValueAnnotationQueryResult>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to event message queries.
        /// </summary>
        /// <typeparam name="T">
        ///   The result type.
        /// </typeparam>
        /// <param name="fullMode">
        ///   The action to take when a write is attempted on a full channel.
        /// </param>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        internal static Channel<T> CreateEventMessageChannel<T>(BoundedChannelFullMode fullMode, int capacity = EventChannelCapacity) where T : Events.EventMessageBase {
            return CreateChannel<T>(capacity, fullMode);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to event message queries.
        /// </summary>
        /// <typeparam name="T">
        ///   The result type.
        /// </typeparam>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        public static Channel<T> CreateEventMessageChannel<T>(int capacity = EventChannelCapacity) where T : Events.EventMessageBase {
            return CreateEventMessageChannel<T>(BoundedChannelFullMode.Wait, capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to write event messages to an adapter.
        /// </summary>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        public static Channel<Events.WriteEventMessageItem> CreateEventMessageWriteChannel(int capacity = EventChannelCapacity) {
            return CreateChannel<Events.WriteEventMessageItem>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to return the results of event message writes.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        public static Channel<Events.WriteEventMessageResult> CreateEventMessageWriteResultChannel(int capacity = EventChannelCapacity) {
            return CreateChannel<Events.WriteEventMessageResult>(capacity);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to asset model node queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="AssetModelNodeChannelCapacity"/>.
        /// </remarks>
        public static Channel<AssetModel.AssetModelNode> CreateAssetModelNodeChannel(int capacity = AssetModelNodeChannelCapacity) {
            return CreateChannel<AssetModel.AssetModelNode>(capacity);
        }


        /// <summary>
        /// Republishes items read from the channel reader to a destination channel.
        /// </summary>
        /// <typeparam name="T">
        ///   The item type.
        /// </typeparam>
        /// <param name="source">
        ///   The source channel.
        /// </param>
        /// <param name="destination">
        ///   The destination channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will read and republish items from the source channel until it completes 
        ///   or the <paramref name="cancellationToken"/> fires.
        /// </returns>
        public static async Task Forward<T>(this ChannelReader<T> source, ChannelWriter<T> destination, CancellationToken cancellationToken = default) {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }
            if (destination == null) {
                throw new ArgumentNullException(nameof(destination));
            }

            // Republish from source to destination. We'll swallow OperationCanceledException and 
            // ChannelClosedException errors, as these indicate one way or another that one of the 
            // channels completed.

            try {
                while (!cancellationToken.IsCancellationRequested) {
                    var item = await source.ReadAsync(cancellationToken).ConfigureAwait(false);
                    await destination.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (ChannelClosedException) { }
        }


        /// <summary>
        /// Runs a background operation using the specified channel writer. Once the operation completes, the 
        /// channel will optionally be marked as completed.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel writer.
        /// </param>
        /// <param name="func">
        ///   The background operation to run.
        /// </param>
        /// <param name="complete">
        ///   Indicates if the channel should be marked as completed once the operation has finished. 
        ///   The channel will always be marked as completed if the operation throws an exception.
        /// </param>
        /// <param name="scheduler">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Func<ChannelWriter<T>, CancellationToken, Task> func, bool complete = true, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (scheduler == null) {
                scheduler = BackgroundTaskService.Default;
            }

            scheduler.QueueBackgroundWorkItem(async ct => { 
                using (var compositeToken = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellationToken)) {
                    try {
                        await func(channel, compositeToken.Token).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        channel.TryComplete(e);
                    }
                    finally {
                        if (complete) {
                            channel.TryComplete();
                        }
                    }
                }
            });
        }


        /// <summary>
        /// Runs a background operation using the specified channel writer. Once the operation completes, the 
        /// channel will optionally be marked as completed.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel writer.
        /// </param>
        /// <param name="func">
        ///   The background operation to run.
        /// </param>
        /// <param name="complete">
        ///   Indicates if the channel should be marked as completed once the operation has finished. 
        ///   The channel will always be marked as completed if the operation throws an exception.
        /// </param>
        /// <param name="scheduler">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Action<ChannelWriter<T>, CancellationToken> func, bool complete = true, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (scheduler == null) {
                scheduler = BackgroundTaskService.Default;
            }

            scheduler.QueueBackgroundWorkItem(ct => {
                using (var compositeToken = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellationToken)) {
                    try {
                        func(channel, compositeToken.Token);
                    }
                    catch (Exception e) {
                        channel.TryComplete(e);
                    }
                    finally {
                        if (complete) {
                            channel.TryComplete();
                        }
                    }
                }
            });
        }


        /// <summary>
        /// Runs a background operation using the specified channel reader.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel reader.
        /// </param>
        /// <param name="func">
        ///   The background operation to run.
        /// </param>
        /// <param name="scheduler">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Func<ChannelReader<T>, CancellationToken, Task> func, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (scheduler == null) {
                scheduler = BackgroundTaskService.Default;
            }

            scheduler.QueueBackgroundWorkItem(async ct => {
                using (var compositeToken = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellationToken)) {
                    try {
                        await func(channel, compositeToken.Token).ConfigureAwait(false);
                    }
                    catch (Exception e) {
                        // Swallow the exception; the work item should be handling this.
                    }

                }
            });
        }


        /// <summary>
        /// Runs a background operation using the specified channel reader.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel reader.
        /// </param>
        /// <param name="func">
        ///   The background operation to run.
        /// </param>
        /// <param name="scheduler">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Action<ChannelReader<T>, CancellationToken> func, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (scheduler == null) {
                scheduler = BackgroundTaskService.Default;
            }

            scheduler.QueueBackgroundWorkItem(ct => {
                using (var compositeToken = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellationToken)) {
                    try {
                        func(channel, compositeToken.Token);
                    }
                    catch {
                        // Swallow the exception; the work item should be handling this.
                    }
                }
            });
        }


        /// <summary>
        /// Reads items from the channel and returns a collection of the items that were read.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="maxItems">
        ///   The maximum number of items to read from the channel. Specify less than one to read 
        ///   all items from the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The items that were read.
        /// </returns>
        public static async Task<IEnumerable<T>> ReadItems<T>(this ChannelReader<T> channel, int maxItems = -1, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var result = maxItems > 0 
                ? new List<T>(maxItems) 
                : new List<T>(500);

            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (channel.TryRead(out var item)) {
                    result.Add(item);
                }

                if (maxItems > 0 && result.Count >= maxItems) {
                    break;
                }
            }

            return result;
        }


        /// <summary>
        /// Invokes a callback for each item emitted from the channel.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel.
        /// </param>
        /// <param name="callback">
        ///   The callback function to invoke.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will invoke the <paramref name="callback"/> for each emitted item.
        /// </returns>
        public static async Task ForEachAsync<T>(this ChannelReader<T> channel, Func<T, Task> callback, CancellationToken cancellationToken) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            
            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!channel.TryRead(out var item)) {
                    continue;
                }

                await callback.Invoke(item).WithCancellation(cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Creates a new channel that transforms items emitted from the current channel in a 
        /// background task.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The input channel item type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The output channel item type.
        /// </typeparam>
        /// <param name="channel">
        ///   The input channel.
        /// </param>
        /// <param name="callback">
        ///   The transform function to use.
        /// </param>
        /// <param name="scheduler">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A new <see cref="ChannelReader{T}"/> that will transform and emit items read from 
        ///   the original channel.
        /// </returns>
        public static ChannelReader<TOut> Transform<TIn, TOut>(this ChannelReader<TIn> channel, Func<TIn, TOut> callback, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }

            var result = Channel.CreateUnbounded<TOut>();

            channel.RunBackgroundOperation(async (ch, ct) => { 
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!ch.TryRead(out var item)) {
                            continue;
                        }

                        await result.Writer.WaitToWriteAsync(ct).ConfigureAwait(false);
                        result.Writer.TryWrite(callback(item));
                    }
                }
                catch (Exception e) {
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, scheduler, cancellationToken);

            return result;
        }

    }
}
