using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Implements an in-memory event message store that implements push, read, and write operations.
    /// </summary>
    public sealed class InMemoryEventMessageStore : IEventMessagePush, IEventMessagePushWithTopics, IReadEventMessagesForTimeRange, IReadEventMessagesUsingCursor, IWriteEventMessages, IDisposable {

        /// <summary>
        /// Indicates if the object has been disposed;
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Fires when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Fires when the object is disposed.
        /// </summary>
        private readonly CancellationToken _disposedToken;

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// The <see cref="IBackgroundTaskService"/> to use.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;

        /// <summary>
        /// <see cref="IEventMessagePush"/> handler.
        /// </summary>
        private readonly EventMessagePush _push;

        /// <summary>
        /// <see cref="IEventMessagePushWithTopics"/> handler.
        /// </summary>
        private readonly EventMessagePushWithTopics _pushWithTopics;

        /// <summary>
        /// The event messages, sorted by cursor position.
        /// </summary>
        private readonly SortedList<CursorPosition, EventMessage> _eventMessages = new SortedList<CursorPosition, EventMessage>();

        /// <summary>
        /// Lock for accessing <see cref="_eventMessages"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _eventMessagesLock = new ReaderWriterLockSlim();

        /// <summary>
        /// The maximum number of event messages that can be stored.
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        /// A sequence ID that is assigned to incoming messages to differentiate between messages 
        /// with identical sample times.
        /// </summary>
        private long _sequenceId;

        /// <summary>
        /// Publishes all event messages written to the <see cref="InMemoryEventMessageStore"/>.
        /// </summary>
        public event Action<EventMessage>? Publish;


        /// <summary>
        /// Creates a new <see cref="InMemoryEventMessageStore"/> object.
        /// </summary>
        /// <param name="options">
        ///   The store options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background operations.
        /// </param>
        /// <param name="logger">
        ///   The logger for the <see cref="InMemoryEventMessageStore"/>.
        /// </param>
        public InMemoryEventMessageStore(
            InMemoryEventMessageStoreOptions options, 
            IBackgroundTaskService? backgroundTaskService, 
            ILogger? logger
        ) {
            Logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _disposedToken = _disposedTokenSource.Token;
            _backgroundTaskService = backgroundTaskService ?? BackgroundTaskService.Default;
            _push = new EventMessagePush(options, backgroundTaskService, Logger);
            _pushWithTopics = new EventMessagePushWithTopics(new EventMessagePushWithTopicsOptions() { 
                AdapterId = options.AdapterId
            }, backgroundTaskService, Logger);
            _capacity = options?.Capacity ?? -1;
        }


        /// <inheritdoc/>
        Task<ChannelReader<EventMessage>> IEventMessagePush.Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request, 
            CancellationToken cancellationToken
        ) {
            return ((IEventMessagePush) _push).Subscribe(context, request, cancellationToken);
        }


        /// <inheritdoc/>
        Task<ChannelReader<EventMessage>> IEventMessagePushWithTopics.Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageTopicSubscriptionRequest request,
            ChannelReader<EventMessageSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            return ((IEventMessagePushWithTopics) _pushWithTopics).Subscribe(context, request, channel, cancellationToken);
        }



        /// <summary>
        /// Writes an event message to the store.
        /// </summary>
        /// <param name="message">
        ///   The message.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The cursor position for the message.
        /// </returns>
        private async ValueTask<CursorPosition> WriteEventMessage(EventMessage message, CancellationToken cancellationToken) {
            var cursorPosition = CreateCursorPosition(message);

            _eventMessagesLock.EnterWriteLock();
            try {
                var msg = EventMessageBuilder.CreateFromExisting(message).Build();
                if (Logger.IsEnabled(LogLevel.Trace)) {
                    Logger.LogTrace(
                        Resources.Log_InMemoryEventMessageManager_WroteMessage, 
                        cursorPosition, 
                        msg.Id, 
                        msg.UtcEventTime
                    );
                }
                _eventMessages.Add(cursorPosition, msg);
                if (_capacity > 0 && _eventMessages.Count > _capacity) {

                    // Over capacity; remove earliest message.
                    if (Logger.IsEnabled(LogLevel.Trace)) {
                        var evicted = _eventMessages.First();
                        Logger.LogTrace(
                            Resources.Log_InMemoryEventMessageManager_EvictedMessage, 
                            evicted.Key, 
                            evicted.Value.Id, 
                            evicted.Value.UtcEventTime
                        );
                    }
                    
                    _eventMessages.RemoveAt(0);
                }
            }
            finally {
                _eventMessagesLock.ExitWriteLock();
            }

            Publish?.Invoke(message);
            await _push.ValueReceived(message, cancellationToken).ConfigureAwait(false);
            await _pushWithTopics.ValueReceived(message, cancellationToken).ConfigureAwait(false);

            return cursorPosition;
        }


        /// <summary>
        /// Writes event messages to the store.
        /// </summary>
        /// <param name="messages">
        ///   The messages.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will write the messages.
        /// </returns>
        public ValueTask WriteEventMessages(params EventMessage[] messages) {
            return WriteEventMessages((IEnumerable<EventMessage>) messages);
        }


        /// <summary>
        /// Writes event messages to the store.
        /// </summary>
        /// <param name="messages">
        ///   The messages.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will write the messages.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="messages"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask WriteEventMessages(IEnumerable<EventMessage> messages) {
            if (messages == null) {
                throw new ArgumentNullException(nameof(messages));
            }

            foreach (var message in messages) {
                if (message == null) {
                    continue;
                }
                await WriteEventMessage(message, _disposedToken).ConfigureAwait(false);
            }
        }



        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exceptions are emitted via the response channel")]
        public Task<ChannelReader<WriteEventMessageResult>> WriteEventMessages(IAdapterCallContext context, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel();

            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        while (ch.TryRead(out var item)) {
                            if (item?.EventMessage == null) {
                                continue;
                            }

                            var cursorPosition = await WriteEventMessage(item.EventMessage, ct).ConfigureAwait(false);
                            if (!await result.Writer.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                                break;
                            }

                            result.Writer.TryWrite(new WriteEventMessageResult(
                                item.CorrelationId,
                                Common.WriteStatus.Success,
                                null,
                                new[] {
                                new Common.AdapterProperty("Cursor Position", Common.Variant.FromValue(cursorPosition.ToString()))
                                }
                            ));
                        }
                    }
                }
                catch (Exception e) {
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, _backgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public Task<ChannelReader<EventMessage>> ReadEventMessagesForTimeRange(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            EventMessage[] messages;

            _eventMessagesLock.EnterReadLock();
            try {
                var selector = _eventMessages
                    .Values
                    .Where(x => x.UtcEventTime >= request.UtcStartTime)
                    .Where(x => x.UtcEventTime <= request.UtcEndTime);

                if (request.Topics != null && request.Topics.Any()) {
                    selector = selector.Where(x => request.Topics.Contains(x.Topic, StringComparer.OrdinalIgnoreCase));
                }

                if (request.Direction == EventReadDirection.Backwards) {
                    selector = selector.OrderByDescending(x => x.UtcEventTime);
                }

                messages = selector
                    .Skip(request.PageSize * (request.Page - 1))
                    .Take(request.PageSize)
                    .ToArray();
            }
            finally {
                _eventMessagesLock.ExitReadLock();
            }

            return Task.FromResult(messages.Select(x => EventMessageBuilder.CreateFromExisting(x).Build()).PublishToChannel());
        }


        /// <inheritdoc/>
        public Task<ChannelReader<EventMessageWithCursorPosition>> ReadEventMessagesUsingCursor(IAdapterCallContext context, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            KeyValuePair<CursorPosition, EventMessage>[] messages;

            _eventMessagesLock.EnterReadLock();
            try {
                IEnumerable<KeyValuePair<CursorPosition, EventMessage>> selector;

                if (string.IsNullOrWhiteSpace(request.CursorPosition)) {
                    selector = request.Direction == EventReadDirection.Forwards
                        ? _eventMessages
                        : _eventMessages.Reverse();
                }
                else if (!CursorPosition.TryParse(request.CursorPosition!, out var cursorPosition) || !_eventMessages.ContainsKey(cursorPosition)) {
                    return Task.FromResult(Array.Empty<EventMessageWithCursorPosition>().PublishToChannel());
                }
                else {
                    selector = request.Direction == EventReadDirection.Forwards
                        ? _eventMessages.Where(x => x.Key > cursorPosition)
                        : _eventMessages.Where(x => x.Key < cursorPosition).Reverse();
                }

                if (!string.IsNullOrWhiteSpace(request.Topic)) {
                    selector = selector.Where(x => string.Equals(x.Value.Topic, request.Topic, StringComparison.OrdinalIgnoreCase));
                }

                messages = selector
                    .Take(request.PageSize)
                    .ToArray();
            }
            finally {
                _eventMessagesLock.ExitReadLock();
            }

            return Task.FromResult(messages.Select(x => EventMessageBuilder.CreateFromExisting(x.Value).Build(x.Key.ToString())).PublishToChannel());
        }


        /// <summary>
        /// Creates a cursor position for the specified event message.
        /// </summary>
        /// <param name="message">
        ///   The message.
        /// </param>
        /// <returns>
        ///   The cursor position.
        /// </returns>
        private CursorPosition CreateCursorPosition(EventMessage message) {
            return new CursorPosition(message.UtcEventTime.Ticks, Interlocked.Increment(ref _sequenceId));
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            _isDisposed = true;

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();

            _push.Dispose();
            _pushWithTopics.Dispose();

            _eventMessagesLock.EnterWriteLock();
            try {
                _eventMessages.Clear();
            }
            finally {
                _eventMessagesLock.ExitWriteLock();
            }
            _eventMessagesLock.Dispose();
        }


        /// <summary>
        /// Describes a cursor position for an event message that is sortable via a <see cref="Primary"/> 
        /// index, and then by a <see cref="Secondary"/> index if two cursor positions have the same 
        /// primary index.
        /// </summary>
        private struct CursorPosition : IComparable<CursorPosition> {

            /// <summary>
            /// The primary index for the cursor. Cursors are sorted initially by <see cref="Primary"/> 
            /// and then by <see cref="Secondary"/>.
            /// </summary>
            public long Primary;

            /// <summary>
            /// The secondary index for the cursor, to allow messages with the same <see cref="Primary"/> 
            /// value to be sorted into order.
            /// </summary>
            public long Secondary;


            /// <summary>
            /// Creates a new <see cref="CursorPosition"/>.
            /// </summary>
            /// <param name="primary">
            ///   The primary index for the cursor.
            /// </param>
            /// <param name="secondary">
            ///   The secondary index for the cursor.
            /// </param>
            public CursorPosition(long primary, long secondary) {
                Primary = primary;
                Secondary = secondary;
            }


            /// <inheritdoc/>
            public override string ToString() {
                return string.Concat(Primary, '|', Secondary);
            }


            /// <inheritdoc/>
            public int CompareTo(CursorPosition other) {
                if (Primary < other.Primary) {
                    return -1;
                }
                if (Primary > other.Primary) {
                    return 1;
                }

                return Secondary < other.Secondary
                    ? -1
                    : Secondary > other.Secondary
                        ? 1
                        : 0;
            }


            /// <summary>
            /// Tries to parse the specified value into a <see cref="CursorPosition"/> instance.
            /// </summary>
            /// <param name="value">
            ///   The string to parse.
            /// </param>
            /// <param name="cursorPosition">
            ///   The parsed cursor position.
            /// </param>
            /// <returns>
            ///   <see langword="true"/> if the cursor position was successfully parsed, or 
            ///   <see langword="false"/> otherwise.
            /// </returns>
            public static bool TryParse(string value, out CursorPosition cursorPosition) {
                if (string.IsNullOrWhiteSpace(value)) {
                    cursorPosition = default;
                    return false;
                }

                var parts = value.Split('|');
                if (parts.Length != 2) {
                    cursorPosition = default;
                    return false;
                }

                if (!long.TryParse(parts[0], out var ticks) || !long.TryParse(parts[1], out var sequenceId)) {
                    cursorPosition = default;
                    return false;
                }

                cursorPosition = new CursorPosition(ticks, sequenceId);
                return true;
            }


            /// <inheritdoc/>
            public static bool operator < (CursorPosition x, CursorPosition y) {
                return x.CompareTo(y) < 0;
            }


            /// <inheritdoc/>
            public static bool operator > (CursorPosition x, CursorPosition y) {
                return x.CompareTo(y) > 0;
            }


            /// <inheritdoc/>
            public static bool operator <= (CursorPosition x, CursorPosition y) {
                return x.CompareTo(y) < 0;
            }


            /// <inheritdoc/>
            public static bool operator >= (CursorPosition x, CursorPosition y) {
                return x.CompareTo(y) > 0;
            }

        }

    }


    /// <summary>
    /// Options for <see cref="InMemoryEventMessageStore"/>.
    /// </summary>
    public class InMemoryEventMessageStoreOptions : EventMessagePushOptions {

        /// <summary>
        /// The capacity of the store. When the store reaches capacity, the messages with the
        /// earliest timestamp will be removed to create space for newer messages. Specify 
        /// less than one for no limit.
        /// </summary>
        public int Capacity { get; set; } = 5000;

    }
}
