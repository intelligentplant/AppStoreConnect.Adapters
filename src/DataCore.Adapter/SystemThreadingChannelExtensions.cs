#if NETSTANDARD2_1 == false

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Channels {

    /// <summary>
    /// Extension methods for <see cref="Channel{T}"/>, <see cref="ChannelReader{T}"/>, and 
    /// <see cref="ChannelWriter{T}"/>.
    /// </summary>
    public static class SystemThreadingChannelExtensions {

        /// <summary>
        /// Creates an <see cref="IAsyncEnumerable{T}"/> that enables reading all of the data 
        /// from the channel.
        /// </summary>
        /// <typeparam name="T">
        ///   The item type.
        /// </typeparam>
        /// <param name="channel">
        ///   The <see cref="ChannelReader{T}"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to use to cancel the enumeration.
        /// </param>
        /// <returns>
        ///   The created async enumerable.
        /// </returns>
        public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> channel, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (channel.TryRead(out var item)) {
                    yield return item;
                }
            }
        }

    }
}

#endif
