using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Events {
    internal class EventSubscriptionChannel<TIdentifier, TTopic, TValue> : SubscriptionChannel<TIdentifier, TTopic, TValue> {

        public EventMessageSubscriptionType SubscriptionType { get; }


        public EventSubscriptionChannel(
            TIdentifier id,
            IAdapterCallContext context,
            IBackgroundTaskService scheduler,
            TTopic topic,
            EventMessageSubscriptionType subscriptionType,
            TimeSpan publishInterval,
            CancellationToken[] cancellationTokens,
            Action cleanup,
            int channelCapacity = 0
        ) : base(
            id,
            context,
            scheduler,
            topic,
            publishInterval,
            cancellationTokens,
            cleanup,
            channelCapacity
        ) {
            SubscriptionType = subscriptionType;
        } 

    }
}
