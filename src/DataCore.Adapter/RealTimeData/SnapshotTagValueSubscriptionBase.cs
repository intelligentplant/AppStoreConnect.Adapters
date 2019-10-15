using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Base implementation of <see cref="ISnapshotTagValueSubscription"/>.
    /// </summary>
    public abstract class SnapshotTagValueSubscriptionBase : AdapterSubscription<TagValueQueryResult>, ISnapshotTagValueSubscription {

        /// <inheritdoc/>
        public abstract int Count { get; }

        /// <inheritdoc/>
        public abstract ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);
    
    }

}
