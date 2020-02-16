using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Implements an in-memory event message store that implements push, read, and write operations.
    /// </summary>
    public class InMemoryEventMessageManager : EventMessagePush, IReadEventMessagesForTimeRange, IReadEventMessagesUsingCursor, IWriteEventMessages {

        /// <summary>
        /// The event messages, sorted by cursor position.
        /// </summary>
        private readonly SortedList<string, EventMessage> _eventMessages = new SortedList<string, EventMessage>(StringComparer.Ordinal);

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
        /// Creates a new <see cref="InMemoryEventMessageManager"/> object.
        /// </summary>
        /// <param name="options">
        ///   The store options.
        /// </param>
        /// <param name="logger">
        ///   The logger for the <see cref="InMemoryEventMessageManager"/>.
        /// </param>
        public InMemoryEventMessageManager(InMemoryEventMessageManagerOptions options, ILogger logger) 
            : base(logger) {
            _capacity = options?.Capacity ?? -1;
        }


        /// <inheritdoc/>
        protected override Task OnSubscriptionAdded(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <inheritdoc/>
        protected override Task OnSubscriptionRemoved(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        /// <summary>
        /// Writes an event message to the store.
        /// </summary>
        /// <param name="message">
        ///   The message.
        /// </param>
        /// <returns>
        ///   The cursor position for the message.
        /// </returns>
        private string WriteEventMessage(EventMessage message) {
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
            OnMessage(message);

            return cursorPosition;
        }


        /// <summary>
        /// Writes event messages to the store.
        /// </summary>
        /// <param name="messages">
        ///   The messages.
        /// </param>
        public void WriteEventMessages(params EventMessage[] messages) {
            WriteEventMessages((IEnumerable<EventMessage>) messages);
        }


        /// <summary>
        /// Writes event messages to the store.
        /// </summary>
        /// <param name="messages">
        ///   The messages.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="messages"/> is <see langword="null"/>.
        /// </exception>
        public void WriteEventMessages(IEnumerable<EventMessage> messages) {
            if (messages == null) {
                throw new ArgumentNullException(nameof(messages));
            }

            foreach (var message in messages) {
                if (message == null) {
                    continue;
                }
                WriteEventMessage(message);
            }
        }


        /// <inheritdoc/>
        public ChannelReader<WriteEventMessageResult> WriteEventMessages(IAdapterCallContext context, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateEventMessageWriteResultChannel();

            channel.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!ch.TryRead(out var item) || item?.EventMessage == null) {
                            continue;
                        }

                        var cursorPosition = WriteEventMessage(item.EventMessage);
                        if (!await result.Writer.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                            break;
                        }

                        result.Writer.TryWrite(new WriteEventMessageResult(
                            item.CorrelationId, 
                            Common.WriteStatus.Success, 
                            null, 
                            new [] {
                                new Common.AdapterProperty("Cursor Position", Common.Variant.FromValue(cursorPosition))
                            }
                        ));
                    }
                }
                catch (Exception e) {
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, null, cancellationToken);

            return result;
        }


        /// <inheritdoc/>
        public ChannelReader<EventMessage> ReadEventMessages(IAdapterCallContext context, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
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

            return messages.Select(x => EventMessageBuilder.CreateFromExisting(x).Build()).PublishToChannel();
        }


        /// <inheritdoc/>
        public ChannelReader<EventMessageWithCursorPosition> ReadEventMessages(IAdapterCallContext context, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            KeyValuePair<string, EventMessage>[] messages;

            _eventMessagesLock.EnterReadLock();
            try {
                IEnumerable<KeyValuePair<string, EventMessage>> selector;

                if (string.IsNullOrWhiteSpace(request.CursorPosition)) {
                    selector = request.Direction == EventReadDirection.Forwards
                        ? _eventMessages
                        : _eventMessages.Reverse();
                }
                else if (!_eventMessages.ContainsKey(request.CursorPosition)) {
                    return Array.Empty<EventMessageWithCursorPosition>().PublishToChannel();
                }
                else {
                    selector = request.Direction == EventReadDirection.Forwards
                        ? _eventMessages.Where(x => string.CompareOrdinal(request.CursorPosition, x.Key) > 0)
                        : _eventMessages.Where(x => string.CompareOrdinal(request.CursorPosition, x.Key) < 0).Reverse();
                }

                messages = selector
                    .Take(request.PageSize)
                    .ToArray();
            }
            finally {
                _eventMessagesLock.ExitReadLock();
            }

            return messages.Select(x => EventMessageBuilder.CreateFromExisting(x.Value).Build(x.Key)).PublishToChannel();
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
        private string CreateCursorPosition(EventMessage message) {
            return string.Concat(message.UtcEventTime.Ticks, '|', Interlocked.Increment(ref _sequenceId));
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _eventMessagesLock.EnterWriteLock();
                try {
                    _eventMessages.Clear();
                }
                finally {
                    _eventMessagesLock.ExitWriteLock();
                }
                _eventMessagesLock.Dispose();
            }
        }

    }


    /// <summary>
    /// Options for <see cref="InMemoryEventMessageManager"/>.
    /// </summary>
    public class InMemoryEventMessageManagerOptions {

        /// <summary>
        /// The capacity of the store. When the store reaches capacity, the messages with the
        /// earliest timestamp will be removed to create space for newer messages. Specify 
        /// less than one for no limit.
        /// </summary>
        public int Capacity { get; set; }

    }
}
