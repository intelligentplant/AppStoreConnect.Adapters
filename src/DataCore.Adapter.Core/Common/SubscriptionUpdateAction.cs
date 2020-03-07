namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes an update to a push subscription.
    /// </summary>
    public enum SubscriptionUpdateAction {

        /// <summary>
        /// A subscription is being added.
        /// </summary>
        Subscribe,

        /// <summary>
        /// A subscription is being removed.
        /// </summary>
        Unsubscribe

    }
}
