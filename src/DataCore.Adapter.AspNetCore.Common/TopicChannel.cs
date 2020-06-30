using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// <see cref="Channel{T}"/> implementation that is used to republish values emitted by a 
    /// <see cref="TopicSubscriptionWrapper{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The value type for the channel.
    /// </typeparam>
    public sealed class TopicChannel<T> : Channel<T>, IAsyncDisposable {

        /// <summary>
        /// The subscription that created this instance.
        /// </summary>
        private readonly TopicSubscriptionWrapper<T> _subscription;

        /// <summary>
        /// The channel used to republish values.
        /// </summary>
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions() {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

        /// <summary>
        /// The topic that this channel is observing.
        /// </summary>
        public string Topic { get; }


        /// <summary>
        /// Creates a new <see cref="TopicChannel{T}"/> instance.
        /// </summary>
        /// <param name="subscription">
        ///   The <see cref="TopicSubscriptionWrapper{T}"/> that is creating this instance.
        /// </param>
        /// <param name="topic">
        ///   The topic that this instance will be observing.
        /// </param>
        internal TopicChannel(TopicSubscriptionWrapper<T> subscription, string topic) {
            _subscription = subscription;
            Topic = topic;
            Reader = _channel.Reader;
            Writer = _channel.Writer;
        }


        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            _channel.Writer.TryComplete();
            await _subscription.OnTopicChannelDisposed(this).ConfigureAwait(false);
        }

    }
}
