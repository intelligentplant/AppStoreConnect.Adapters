using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// <see cref="ISnapshotTagValuePush"/> implementation.
    /// </summary>
    public class SnapshotTagValuePush : SnapshotTagValuePushBase {

        /// <summary>
        /// Holds the current values for subscribed tags.
        /// </summary>
        private readonly ConcurrentDictionary<TagIdentifier, TagValueQueryResult> _currentValueCache = new ConcurrentDictionary<TagIdentifier, TagValueQueryResult>();


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        public SnapshotTagValuePush(SnapshotTagValuePushOptions? options, IBackgroundTaskService? backgroundTaskService, ILogger? logger) 
            : base(options, backgroundTaskService, logger) { }


        /// <summary>
        /// Creates a delegate compatible with <see cref="SnapshotTagValuePushOptions.TagResolver"/> using an 
        /// <see cref="ITagInfo"/> feature.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="ITagInfo"/> feature to use.
        /// </param>
        /// <returns>
        ///   A new delegate.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        public static Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, IAsyncEnumerable<TagIdentifier>> CreateTagResolverFromFeature(ITagInfo feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return (context, tags, cancellationToken) => feature.GetTags(context, new GetTagsRequest() { 
                Tags = tags?.ToArray()!
            }, cancellationToken);
        }


        /// <summary>
        /// Creates a delegate compatible with <see cref="SnapshotTagValuePushOptions.TagResolver"/> using an 
        /// <see cref="IAdapter"/> that implements the <see cref="ITagInfo"/> feature.
        /// </summary>
        /// <param name="adapter">
        ///   The <see cref="IAdapter"/> to use to resolve tags.
        /// </param>
        /// <returns>
        ///   A new delegate.
        /// </returns>
        /// <remarks>
        ///   The adapter's <see cref="ITagInfo"/> feature will be resolved every time the resulting 
        ///   delegate is invoked. If the feature cannot be resolved, the delegate will return an 
        ///   empty <see cref="IAsyncEnumerable{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static Func<IAdapterCallContext, IEnumerable<string>, CancellationToken, IAsyncEnumerable<TagIdentifier>> CreateTagResolverFromAdapter(IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            return (context, tags, cancellationToken) => adapter.TryGetFeature<ITagInfo>(out var feature)
                ? feature!.GetTags(context, new GetTagsRequest() { Tags = tags?.ToArray()! }, cancellationToken)
                : Array.Empty<TagIdentifier>().PublishToChannel().ReadAllAsync(cancellationToken);
        }


        /// <inheritdoc/>
        protected override ValueTask<bool> AddOrUpdateCachedValueAsync(TagIdentifier tag, TagValueQueryResult value, CancellationToken cancellationToken) {
            // Add the value
            var latestValue = _currentValueCache.AddOrUpdate(
                tag,
                value,
                (key, prev) => prev.Value.UtcSampleTime > value.Value.UtcSampleTime
                    ? prev
                    : value
            );

            if (latestValue != value) {
                // There was already a later value sent for this tag.
                return new ValueTask<bool>(false);
            }

            return new ValueTask<bool>(true);
        }


        /// <inheritdoc/>
        protected override ValueTask<TagValueQueryResult?> GetCachedValueAsync(TagIdentifier tag, CancellationToken cancellationToken) {
            return new ValueTask<TagValueQueryResult?>(_currentValueCache.TryGetValue(tag, out var value) ? value : null);
        }


        /// <inheritdoc/>
        protected override ValueTask<bool> RemoveCachedValueAsync(TagIdentifier tag, CancellationToken cancellationToken) {
            return new ValueTask<bool>(_currentValueCache.TryRemove(tag, out _));
        }


        /// <inheritdoc/>
        protected override async Task OnTagsRemoved(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            await base.OnTagsRemoved(tags, cancellationToken).ConfigureAwait(false);
            await RemoveCachedValuesIfNoSubscribersAsync(tags, cancellationToken).ConfigureAwait(false);
        }

    }

}
