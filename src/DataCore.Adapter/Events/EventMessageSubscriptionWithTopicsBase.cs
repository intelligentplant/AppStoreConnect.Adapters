using System.Threading.Tasks;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Base implementation of <see cref="IEventMessageSubscriptionWithTopics"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The type that topic names will be resolved to when added to the subscription.
    /// </typeparam>
    public abstract class EventMessageSubscriptionWithTopicsBase<T> : AdapterSubscriptionWithTopics<EventMessage, T>, IEventMessageSubscriptionWithTopics where T : class {

        /// <summary>
        /// The subscription type.
        /// </summary>
        public EventMessageSubscriptionType SubscriptionType { get; }


        /// <summary>
        /// Creates a new <see cref="EventMessageSubscriptionWithTopicsBase{T}"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        /// <param name="subscriptionType">
        ///   The subscription type (active or passive).
        /// </param>
        protected EventMessageSubscriptionWithTopicsBase(
            IAdapterCallContext context,
            string id,
            EventMessageSubscriptionType subscriptionType
        ) : base(
            context,
            id
        ) {
            SubscriptionType = subscriptionType;
        }


        /// <inheritdoc/>
        protected override string GetTopicNameForValue(EventMessage value) {
            return value?.Topic;
        }


        /// <inheritdoc/>
        protected override void OnCancelled() {
            // Do nothing.
        }

    }


    /// <summary>
    /// Base implementation of <see cref="IEventMessageSubscriptionWithTopics"/> that does not 
    /// need to parse a topic name into another type.
    /// </summary>
    public abstract class EventMessageSubscriptionWithTopicsBase : EventMessageSubscriptionWithTopicsBase<string> {

        /// <summary>
        /// Creates a new <see cref="EventMessageSubscriptionWithTopicsBase"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        /// <param name="subscriptionType">
        ///   The subscription type (active or passive).
        /// </param>
        protected EventMessageSubscriptionWithTopicsBase(
            IAdapterCallContext context,
            string id,
            EventMessageSubscriptionType subscriptionType
        ) : base(context, id, subscriptionType) { }


        /// <inheritdoc/>
        protected sealed override ValueTask<string> ResolveTopic(IAdapterCallContext context, string topic) {
            return new ValueTask<string>(topic);
        }

    }

}
