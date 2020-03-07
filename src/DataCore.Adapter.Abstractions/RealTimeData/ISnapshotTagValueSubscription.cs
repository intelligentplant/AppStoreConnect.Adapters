using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a snapshot tag value subscription on an adapter.
    /// </summary>
    public interface ISnapshotTagValueSubscription : IAdapterSubscription<TagValueQueryResult> {

        /// <summary>
        /// Adds a tag to the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag ID or name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        ValueTask<bool> AddTagToSubscription(string tag);


        /// <summary>
        /// Removes a tag from the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag ID or name.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        ValueTask<bool> RemoveTagFromSubscription(string tag);

    }
}
