using System;
using System.Collections.Generic;
using System.Text;
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
        /// Capacity of channels created using <see cref="CreateBoundedTagDefinitionChannel"/>.
        /// </summary>
        public const int TagDefinitionChannelCapacity = 100;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateBoundedTagValueChannel{T}"/>.
        /// </summary>
        public const int TagValueChannelCapacity = 1000;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateBoundedTagValueAnnotationChannel"/>.
        /// </summary>
        public const int TagAnnotationChannelCapacity = 100;

        /// <summary>
        /// Capacity of channels created using <see cref="CreateBoundedEventMessageChannel{T}()"/>.
        /// </summary>
        public const int EventChannelCapacity = 100;


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
        /// Creates a bounded channel that can be used to return results to tag search queries.
        /// </summary>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The capacity of the created channel is set to <see cref="TagDefinitionChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.Models.TagDefinition> CreateBoundedTagDefinitionChannel() {
            return CreateChannel<RealTimeData.Models.TagDefinition>(TagDefinitionChannelCapacity);
        }


        /// <summary>
        /// Creates a bounded channel that can be used to return results to tag value queries.
        /// </summary>
        /// <typeparam name="T">
        ///   The result type.
        /// </typeparam>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The capacity of the created channel is set to <see cref="TagValueChannelCapacity"/>.
        /// </remarks>
        public static Channel<T> CreateBoundedTagValueChannel<T>() where T : RealTimeData.Models.TagValueQueryResult {
            return CreateChannel<T>(TagValueChannelCapacity);
        }


        /// <summary>
        /// Creates a bounded channel that can be used to return results to tag annotation queries.
        /// </summary>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The capacity of the created channel is set to <see cref="TagAnnotationChannelCapacity"/>.
        /// </remarks>
        public static Channel<RealTimeData.Models.TagValueAnnotationQueryResult> CreateBoundedTagValueAnnotationChannel() {
            return CreateChannel<RealTimeData.Models.TagValueAnnotationQueryResult>(TagAnnotationChannelCapacity);
        }


        /// <summary>
        /// Creates a bounded channel that can be used to return results to event message queries.
        /// </summary>
        /// <typeparam name="T">
        ///   The result type.
        /// </typeparam>
        /// <param name="fullMode">
        ///   The action to take when a write is attempted on a full channel.
        /// </param>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        internal static Channel<T> CreateBoundedEventMessageChannel<T>(BoundedChannelFullMode fullMode) where T : Events.Models.EventMessageBase {
            return CreateChannel<T>(EventChannelCapacity, fullMode);
        }


        /// <summary>
        /// Creates a bounded channel that can be used to return results to event message queries.
        /// </summary>
        /// <typeparam name="T">
        ///   The result type.
        /// </typeparam>
        /// <returns>
        ///   The channel.
        /// </returns>
        /// <remarks>
        ///   The capacity of the created channel is set to <see cref="EventChannelCapacity"/>.
        /// </remarks>
        public static Channel<T> CreateBoundedEventMessageChannel<T>() where T : Events.Models.EventMessageBase {
            return CreateBoundedEventMessageChannel<T>(BoundedChannelFullMode.Wait);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Func<ChannelWriter<T>, CancellationToken, Task> func, bool complete = true, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            _ = Task.Run(async () => {
                try {
                    await func(channel, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) {
                    channel.TryComplete(e);
                }
                finally {
                    if (complete) {
                        channel.TryComplete();
                    }
                }
            }, cancellationToken);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelWriter<T> channel, Action<ChannelWriter<T>, CancellationToken> func, bool complete = true, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            _ = Task.Run(() => {
                try {
                    func(channel, cancellationToken);
                }
                catch (Exception e) {
                    channel.TryComplete(e);
                }
                finally {
                    if (complete) {
                        channel.TryComplete();
                    }
                }
            }, cancellationToken);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Func<ChannelReader<T>, CancellationToken, Task> func, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            _ = Task.Run(async () => {
                try {
                    await func(channel, cancellationToken).ConfigureAwait(false);
                }
                catch {
                    // Swallow the exception; the background operation should handle these.
                }
            }, cancellationToken);
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="func"/> is <see langword="null"/>.
        /// </exception>
        public static void RunBackgroundOperation<T>(this ChannelReader<T> channel, Action<ChannelReader<T>, CancellationToken> func, CancellationToken cancellationToken = default) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }
            if (func == null) {
                throw new ArgumentNullException(nameof(func));
            }

            _ = Task.Run(() => {
                try {
                    func(channel, cancellationToken);
                }
                catch {
                    // Swallow the exception; the background operation should handle these.
                }
            }, cancellationToken);
        }

    }
}
