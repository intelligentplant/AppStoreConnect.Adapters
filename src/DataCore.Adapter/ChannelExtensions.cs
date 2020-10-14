using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IntelligentPlant.BackgroundTasks;

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


#pragma warning disable CS0419 // Ambiguous reference in cref attribute
        /// <summary>
        /// Capacity of channels created using <see cref="CreateTagValueChannel{T}"/>.
        /// </summary>
        public const int TagValueChannelCapacity = 100;
#pragma warning restore CS0419 // Ambiguous reference in cref attribute

        /// <summary>
        /// Capacity of channels created using <see cref="CreateTagValueAnnotationChannel"/>.
        /// </summary>
        public const int TagAnnotationChannelCapacity = 100;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateEventMessageChannel{T}(int, bool, bool)"/>.
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
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <param name="fullMode">
        ///   The action to take if a write is attempted on a full channel. Ignored if 
        ///   <paramref name="capacity"/> is less than one.
        /// </param>
        /// <returns>
        ///   The new channel.
        /// </returns>
        public static Channel<T> CreateChannel<T>(
            int capacity,
            bool singleReader = true,
            bool singleWriter = true,
            BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait
        ) {
            return capacity > 0
                ? Channel.CreateBounded<T>(new BoundedChannelOptions(capacity) {
                    FullMode = fullMode,
                    AllowSynchronousContinuations = false,
                    SingleReader = singleReader,
                    SingleWriter = singleWriter
                })
                : Channel.CreateUnbounded<T>(new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = false,
                    SingleReader = singleReader,
                    SingleWriter = singleWriter
                });
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag search queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagDefinitionChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.TagDefinition> CreateTagDefinitionChannel(
            int capacity = TagDefinitionChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<RealTimeData.TagDefinition>(capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag identifier queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagDefinitionChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.TagIdentifier> CreateTagIdentifierChannel(
            int capacity = TagDefinitionChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<RealTimeData.TagIdentifier>(capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag value queries.
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
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<T> CreateTagValueChannel<T>(
            BoundedChannelFullMode fullMode,
            int capacity = TagValueChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) where T : RealTimeData.TagValueQueryResult {
            return CreateChannel<T>(capacity, singleReader, singleWriter, fullMode);
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
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<T> CreateTagValueChannel<T>(
            int capacity = TagValueChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) where T : RealTimeData.TagValueQueryResult {
            return CreateChannel<T>(capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to write tag values to an adapter.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.WriteTagValueItem> CreateTagValueWriteChannel(
            int capacity = TagValueChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<RealTimeData.WriteTagValueItem>(capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to return the results of tag value writes.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.WriteTagValueResult> CreateTagValueWriteResultChannel(
            int capacity = TagValueChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<RealTimeData.WriteTagValueResult>(capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to tag annotation queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="TagAnnotationChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.TagValueAnnotationQueryResult> CreateTagValueAnnotationChannel(
            int capacity = TagAnnotationChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<RealTimeData.TagValueAnnotationQueryResult>(capacity, singleReader, singleWriter);
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
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        internal static Channel<T> CreateEventMessageChannel<T>(
            BoundedChannelFullMode fullMode, 
            int capacity = EventChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) where T : Events.EventMessageBase {
            return CreateChannel<T>(capacity, singleReader, singleWriter, fullMode);
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
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        public static Channel<T> CreateEventMessageChannel<T>(
            int capacity = EventChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) where T : Events.EventMessageBase {
            return CreateEventMessageChannel<T>(BoundedChannelFullMode.Wait, capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to write event messages to an adapter.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        public static Channel<Events.WriteEventMessageItem> CreateEventMessageWriteChannel(
            int capacity = EventChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<Events.WriteEventMessageItem>(capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to return the results of event message writes.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        public static Channel<Events.WriteEventMessageResult> CreateEventMessageWriteResultChannel(
            int capacity = EventChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<Events.WriteEventMessageResult>(capacity, singleReader, singleWriter);
        }


        /// <summary>
        /// Creates a channel that can be used to return results to asset model node queries.
        /// </summary>
        /// <param name="capacity">
        ///   The capacity of the channel. An unbounded channel will be created if the capacity is 
        ///   less than or equal to zero.
        /// </param>
        /// <param name="singleReader">
        ///   Indicates if the channel should be optimised for a single reader.
        /// </param>
        /// <param name="singleWriter">
        ///   Indicates if the channel should be optimised for a single writer.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The default capacity of the created channel is set to <see cref="AssetModelNodeChannelCapacity"/>.
        /// </remarks>
        public static Channel<AssetModel.AssetModelNode> CreateAssetModelNodeChannel(
            int capacity = AssetModelNodeChannelCapacity,
            bool singleReader = true,
            bool singleWriter = true
        ) {
            return CreateChannel<AssetModel.AssetModelNode>(capacity, singleReader, singleWriter);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Error is written to channel")]
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
                while (await source.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    while (source.TryRead(out var item)) {
                        await destination.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ChannelClosedException) { }
            catch (Exception e) {
                destination.TryComplete(e);
            }
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelWriter{T}"/>, 
        /// optionally completing the channel once the operation has completed.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="complete">
        ///   <see langword="true"/> to complete the channel once the operation completes, or 
        ///   <see langword="false"/> to leave the channel open. Exceptions thrown by 
        ///   <paramref name="workItem"/> will always cause the channel to complete.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService, 
            Func<ChannelWriter<T>, CancellationToken, Task> workItem, 
            ChannelWriter<T> channel, 
            bool complete, 
            params CancellationToken[] cancellationTokens
        ) {
            backgroundTaskService.QueueBackgroundChannelOperation(workItem, channel, complete, (IEnumerable<CancellationToken>) cancellationTokens);
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelWriter{T}"/>, 
        /// optionally completing the channel once the operation has completed.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="complete">
        ///   <see langword="true"/> to complete the channel once the operation completes, or 
        ///   <see langword="false"/> to leave the channel open. Exceptions thrown by 
        ///   <paramref name="workItem"/> will always cause the channel to complete.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService,
            Func<ChannelWriter<T>, CancellationToken, Task> workItem,
            ChannelWriter<T> channel,
            bool complete = true,
            IEnumerable<CancellationToken>? cancellationTokens = null
        ) {
            if (backgroundTaskService == null) {
                throw new ArgumentNullException(nameof(backgroundTaskService));
            }
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            backgroundTaskService.QueueBackgroundWorkItem(async ct => {
                try {
                    await workItem(channel, ct).ConfigureAwait(false);
                }
                catch (Exception e) {
                    channel.TryComplete(e);
                    throw;
                }
                finally {
                    if (complete) {
                        channel.TryComplete();
                    }
                }
            }, null, cancellationTokens ?? Array.Empty<CancellationToken>());
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelWriter{T}"/>, 
        /// optionally completing the channel once the operation has completed.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="complete">
        ///   <see langword="true"/> to complete the channel once the operation completes, or 
        ///   <see langword="false"/> to leave the channel open. Exceptions thrown by 
        ///   <paramref name="workItem"/> will always cause the channel to complete.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService,
            Action<ChannelWriter<T>, CancellationToken> workItem,
            ChannelWriter<T> channel,
            bool complete,
            params CancellationToken[] cancellationTokens
        ) {
            backgroundTaskService.QueueBackgroundChannelOperation(workItem, channel, complete, (IEnumerable<CancellationToken>) cancellationTokens);
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelWriter{T}"/>, 
        /// optionally completing the channel once the operation has completed.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="complete">
        ///   <see langword="true"/> to complete the channel once the operation completes, or 
        ///   <see langword="false"/> to leave the channel open. Exceptions thrown by 
        ///   <paramref name="workItem"/> will always cause the channel to complete.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService,
            Action<ChannelWriter<T>, CancellationToken> workItem,
            ChannelWriter<T> channel,
            bool complete = true,
            IEnumerable<CancellationToken>? cancellationTokens = null
        ) {
            if (backgroundTaskService == null) {
                throw new ArgumentNullException(nameof(backgroundTaskService));
            }
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            backgroundTaskService.QueueBackgroundWorkItem(ct => {
                try {
                    workItem(channel, ct);
                }
                catch (Exception e) {
                    channel.TryComplete(e);
                    throw;
                }
                finally {
                    if (complete) {
                        channel.TryComplete();
                    }
                }
            }, null, cancellationTokens ?? Array.Empty<CancellationToken>());
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelReader{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService,
            Func<ChannelReader<T>, CancellationToken, Task> workItem,
            ChannelReader<T> channel,
            params CancellationToken[] cancellationTokens
        ) {
            backgroundTaskService.QueueBackgroundChannelOperation(workItem, channel, (IEnumerable<CancellationToken>) cancellationTokens);
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelReader{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService,
            Func<ChannelReader<T>, CancellationToken, Task> workItem,
            ChannelReader<T> channel,
            IEnumerable<CancellationToken>? cancellationTokens = null
        ) {
            if (backgroundTaskService == null) {
                throw new ArgumentNullException(nameof(backgroundTaskService));
            }
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            backgroundTaskService.QueueBackgroundWorkItem(async ct => {
                await workItem(channel, ct).ConfigureAwait(false);
            }, null, cancellationTokens ?? Array.Empty<CancellationToken>());
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelReader{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService,
            Action<ChannelReader<T>, CancellationToken> workItem,
            ChannelReader<T> channel,
            params CancellationToken[] cancellationTokens
        ) {
            backgroundTaskService.QueueBackgroundChannelOperation(workItem, channel, (IEnumerable<CancellationToken>) cancellationTokens);
        }


        /// <summary>
        /// Queues a background work item that operates on a <see cref="ChannelReader{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that will queue the operation in the 
        ///   background.
        /// </param>
        /// <param name="workItem">
        ///   The operation to perform.
        /// </param>
        /// <param name="channel">
        ///   The channel to perform the operation on.
        /// </param>
        /// <param name="cancellationTokens">
        ///   A cancellation tokens for the operation. A composite token consisting of these tokens 
        ///   and the lifetime token of the <see cref="IBackgroundTaskService"/> will be passed to
        ///   <paramref name="workItem"/>.
        /// </param>
        public static void QueueBackgroundChannelOperation<T>(
            this IBackgroundTaskService backgroundTaskService,
            Action<ChannelReader<T>, CancellationToken> workItem,
            ChannelReader<T> channel,
            IEnumerable<CancellationToken>? cancellationTokens = null
        ) {
            if (backgroundTaskService == null) {
                throw new ArgumentNullException(nameof(backgroundTaskService));
            }
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            backgroundTaskService.QueueBackgroundWorkItem(ct => {
                workItem(channel, ct);
            }, null, cancellationTokens ?? Array.Empty<CancellationToken>());
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
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationTokens">
        ///   The cancellation tokens for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Func<ChannelWriter<T>, CancellationToken, Task> func, bool complete = true, IBackgroundTaskService? backgroundTaskService = null, IEnumerable<CancellationToken>? cancellationTokens = null) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (backgroundTaskService == null) {
                backgroundTaskService = BackgroundTaskService.Default;
            }

            backgroundTaskService.QueueBackgroundChannelOperation(async (ch, ct) => {
                await func(ch, ct).ConfigureAwait(false);
            }, channel, complete, cancellationTokens);
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
        /// <param name="backgroundTaskService">
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
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Func<ChannelWriter<T>, CancellationToken, Task> func, bool complete = true, IBackgroundTaskService? backgroundTaskService = null, CancellationToken cancellationToken = default) {
            channel.RunBackgroundOperation(
                func, 
                complete, 
                backgroundTaskService, 
                cancellationToken.Equals(default) 
                    ? null 
                    : new[] { cancellationToken }
            );
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
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationTokens">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Action<ChannelWriter<T>, CancellationToken> func, bool complete = true, IBackgroundTaskService? backgroundTaskService = null, IEnumerable<CancellationToken>? cancellationTokens = null) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (backgroundTaskService == null) {
                backgroundTaskService = BackgroundTaskService.Default;
            }

            backgroundTaskService.QueueBackgroundChannelOperation((ch, ct) => {
                func(ch, ct);
            }, channel, complete, cancellationTokens);
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
        /// <param name="backgroundTaskService">
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
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Action<ChannelWriter<T>, CancellationToken> func, bool complete = true, IBackgroundTaskService? backgroundTaskService = null, CancellationToken cancellationToken = default) {
            channel.RunBackgroundOperation(
                func, 
                complete, 
                backgroundTaskService, 
                cancellationToken.Equals(default) 
                    ? null 
                    : new[] { cancellationToken }
            );
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
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationTokens">
        ///   The cancellation tokens for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Func<ChannelReader<T>, CancellationToken, Task> func, IBackgroundTaskService? backgroundTaskService = null, IEnumerable<CancellationToken>? cancellationTokens = null) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (backgroundTaskService == null) {
                backgroundTaskService = BackgroundTaskService.Default;
            }

            backgroundTaskService.QueueBackgroundChannelOperation(async (ch, ct) => {
                await func(ch, ct).ConfigureAwait(false);
            }, channel, cancellationTokens);
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
        /// <param name="backgroundTaskService">
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
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Func<ChannelReader<T>, CancellationToken, Task> func, IBackgroundTaskService? backgroundTaskService = null, CancellationToken cancellationToken = default) {
            channel.RunBackgroundOperation(
                func, 
                backgroundTaskService, 
                cancellationToken.Equals(default) 
                    ? null 
                    : new[] { cancellationToken }
            );
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
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationTokens">
        ///   The cancellation tokens for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Action<ChannelReader<T>, CancellationToken> func, IBackgroundTaskService? backgroundTaskService = null, IEnumerable<CancellationToken>? cancellationTokens = null) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            if (backgroundTaskService == null) {
                backgroundTaskService = BackgroundTaskService.Default;
            }

            backgroundTaskService.QueueBackgroundChannelOperation((ch, ct) => {
                func(ch, ct);
            }, channel, cancellationTokens);
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
        /// <param name="backgroundTaskService">
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
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Action<ChannelReader<T>, CancellationToken> func, IBackgroundTaskService? backgroundTaskService = null, CancellationToken cancellationToken = default) {
            channel.RunBackgroundOperation(
                func,
                backgroundTaskService,
                cancellationToken.Equals(default)
                    ? null
                    : new[] { cancellationToken }
            );
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
        public static async Task<IEnumerable<T>> ToEnumerable<T>(this ChannelReader<T> channel, int maxItems = -1, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var result = maxItems > 0 
                ? new List<T>(maxItems) 
                : new List<T>(500);

            var shouldBreak = false;

            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (channel.TryRead(out var item)) {
                    result.Add(item);

                    if (maxItems > 0 && result.Count >= maxItems) {
                        shouldBreak = true;
                        break;
                    }
                }

                if (shouldBreak) {
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
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            
            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (channel.TryRead(out var item)) {
                    await callback.Invoke(item).WithCancellation(cancellationToken).ConfigureAwait(false);
                }
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
        /// <param name="backgroundTaskService">
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
        public static ChannelReader<TOut> Transform<TIn, TOut>(this ChannelReader<TIn> channel, Func<TIn, TOut> callback, IBackgroundTaskService? backgroundTaskService = null, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }

            var result = Channel.CreateUnbounded<TOut>();

            if (backgroundTaskService == null) {
                backgroundTaskService = BackgroundTaskService.Default;
            }

            backgroundTaskService.QueueBackgroundChannelOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        while (ch.TryRead(out var item)) {
                            await result.Writer.WriteAsync(callback(item), ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) {
                    result.Writer.TryComplete();
                }
                catch (ChannelClosedException) {
                    result.Writer.TryComplete();
                }
                catch (Exception e) {
                    result.Writer.TryComplete(e);
                    throw;
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, channel, cancellationToken);

            return result;
        }


        /// <summary>
        /// Creates a new channel that will emit the items and then complete.
        /// </summary>
        /// <typeparam name="T">
        ///   The item type.
        /// </typeparam>
        /// <param name="items">
        ///   The items to publish.
        /// </param>
        /// <returns>
        ///   A new <see cref="ChannelReader{T}"/> that will emit the items.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="items"/> is <see langword="null"/>.
        /// </exception>
        public static ChannelReader<T> PublishToChannel<T>(this IEnumerable<T> items) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }

            var result = Channel.CreateUnbounded<T>();

            items.PublishToChannel(result);

            result.Writer.TryComplete();
            return result;
        }


        /// <summary>
        /// Publishes the items to the specified channel.
        /// </summary>
        /// <typeparam name="T">
        ///   The item type.
        /// </typeparam>
        /// <param name="items">
        ///   The items to publish.
        /// </param>
        /// <param name="channel">
        ///   The channel to publish the items to.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="items"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public static void PublishToChannel<T>(this IEnumerable<T> items, ChannelWriter<T> channel) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            foreach (var item in items) {
                if (!channel.TryWrite(item)) {
                    break;
                }
            }
        }


        /// <summary>
        /// Publishes the items to the specified channel.
        /// </summary>
        /// <typeparam name="T">
        ///   The item type.
        /// </typeparam>
        /// <param name="items">
        ///   The items to publish.
        /// </param>
        /// <param name="channel">
        ///   The channel to publish the items to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="items"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public static async Task PublishToChannelAsync<T>(this IEnumerable<T> items, ChannelWriter<T> channel, CancellationToken cancellationToken = default) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            foreach (var item in items) {
                if (!await channel.WaitToWriteAsync(cancellationToken).ConfigureAwait(false) || !channel.TryWrite(item)) {
                    break;
                }
            }
        }

    }
}
