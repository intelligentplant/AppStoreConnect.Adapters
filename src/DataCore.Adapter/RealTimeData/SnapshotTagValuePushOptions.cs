using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Options for <see cref="SnapshotTagValuePush"/>.
    /// </summary>
    public class SnapshotTagValuePushOptions : SubscriptionManagerOptions {

        /// <summary>
        /// A delegate that will receive tag names or IDs and will return the matching 
        /// <see cref="TagIdentifier"/>.
        /// </summary>
        /// <remarks>
        ///   <see cref="SnapshotTagValuePush.CreateTagResolverFromAdapter(IAdapter)"/> or 
        ///   <see cref="SnapshotTagValuePush.CreateTagResolverFromFeature(ITagInfo)"/> can be 
        ///   used to generate a compatible delegate using an existing adapter or 
        ///   <see cref="ITagInfo"/> implementation.
        /// </remarks>
        public Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, IAsyncEnumerable<TagIdentifier>>? TagResolver { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a tag changes from zero 
        /// to one.
        /// </summary>
        public Func<IEnumerable<TagIdentifier>, CancellationToken, Task>? OnTagSubscriptionsAdded { get; set; }

        /// <summary>
        /// A delegate that is invoked when the number of subscribers for a tag changes from one 
        /// to zero.
        /// </summary>
        public Func<IEnumerable<TagIdentifier>, CancellationToken, Task>? OnTagSubscriptionsRemoved { get; set; }

        /// <summary>
        /// A delegate that is invoked to determine if the topic for a subscription matches the 
        /// topic for a received tag value.
        /// </summary>
        /// <remarks>
        /// <para>
        ///   The first parameter passed to the delegate is the subscription topic, and the second 
        ///   parameter is the topic for the received tag value.
        /// </para>
        /// <para>
        ///   Note that specifying a value for this property overrides the default 
        ///   <see cref="SnapshotTagValuePushBase.IsTopicMatch(TagValueQueryResult, IEnumerable{TagIdentifier}, CancellationToken)"/> 
        ///   behaviour, which checks to see if the tag ID for the incoming value exactly matches 
        ///   the tag ID on a subscribed topic.
        /// </para>
        /// </remarks>
        public Func<TagIdentifier, TagIdentifier, CancellationToken, ValueTask<bool>>? IsTopicMatch { get; set; }

        /// <summary>
        /// A delegate that is invoked to determine if a given tag has subscribers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It is generally not required to specify a value for this property. An example of when 
        /// it would be desirable to provide a delegate would be if you wanted to allow wildcard 
        /// subscriptions, as the default implementation of <see cref="SnapshotTagValuePushBase.HasSubscribersAsync"/> 
        /// only checks to see if there is a subscription that exactly matches a given 
        /// <see cref="TagIdentifier"/>.
        /// </para>
        /// </remarks>
        public Func<TagIdentifier, bool>? HasSubscribers { get; set; }

    }

}
