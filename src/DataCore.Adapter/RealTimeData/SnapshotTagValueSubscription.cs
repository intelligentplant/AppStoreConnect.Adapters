using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Base implementation of <see cref="ISnapshotTagValueSubscription"/>.
    /// </summary>
    public abstract class SnapshotTagValueSubscription : AdapterSubscription<TagValueQueryResult>, ISnapshotTagValueSubscription {

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
