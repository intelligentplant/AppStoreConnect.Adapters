using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Subscriptions;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {
    public class SnapshotSubscriptionManagerOptions : Subscriptions.SubscriptionManagerOptions {

        /// <summary>
        /// A delegate that will receive tag names or IDs and will return the matching 
        /// <see cref="TagIdentifier"/>.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   If <see cref="TagResolver"/> is <see langword="null"/>, the subscription manager 
        ///   will assume that all tag names specified when subscribing are valid tag names.
        /// </para>
        /// 
        /// <para>
        ///   <see cref="SnapshotTagValuePushBase.CreateTagResolverFromAdapter(IAdapter)"/> or 
        ///   <see cref="SnapshotTagValuePushBase.CreateTagResolverFromFeature(ITagInfo)"/> can be 
        ///   used to generate a compatible delegate using an existing adapter or 
        ///   <see cref="ITagInfo"/> implementation.
        /// </para>
        ///   
        /// </remarks>
        public TagResolver? TagResolver { get; set; }

        public Func<TagValueQueryResult, string>? GetTopic { get; set; }

        public Func<IEnumerable<SubscriptionTopic>, CancellationToken, ValueTask>? OnFirstSubscriberAdded { get; set; }

        public Func<IEnumerable<SubscriptionTopic>, CancellationToken, ValueTask>? OnLastSubscriberRemoved { get; set; }

    }
}
