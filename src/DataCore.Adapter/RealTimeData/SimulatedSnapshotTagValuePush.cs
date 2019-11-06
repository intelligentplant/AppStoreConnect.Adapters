using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Allows an adapter that implements <see cref="IReadSnapshotTagValues"/> and 
    /// <see cref="ITagInfo"/> to simulate <see cref="ISnapshotTagValuePush"/> by periodically 
    /// polling for new values for subscribed tags.
    /// </summary>
    public sealed class SimulatedSnapshotTagValuePush : PollingSnapshotTagValuePush {

        /// <summary>
        /// The owning adapter.
        /// </summary>
        private readonly IAdapter _adapter;


        /// <summary>
        /// Creates a new <see cref="SimulatedSnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="adapter">
        ///   The owning adapter.
        /// </param>
        /// <param name="pollingInterval">
        ///   The polling interval to use.
        /// </param>
        /// <param name="taskScheduler">
        ///   The task scheduler to use when registering background operations.
        /// </param>
        /// <param name="logger">
        ///   The logger to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapter"/> does not implement the features required to simulate 
        ///   <see cref="ISnapshotTagValuePush"/>.
        /// </exception>
        private SimulatedSnapshotTagValuePush(IAdapter adapter, TimeSpan pollingInterval, IBackgroundTaskService taskScheduler, ILogger logger)
            : base(pollingInterval, taskScheduler, logger) {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

            if (!IsCompatible(adapter)) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterDoesNotSupportSimulatedPush, adapter.Descriptor.Id, nameof(SimulatedSnapshotTagValuePush)));
            }
        }


        /// <summary>
        /// Tests if the specified adapter is compatible with the <see cref="SimulatedSnapshotTagValuePush"/> 
        /// class. 
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the adapter is compatible with <see cref="SimulatedSnapshotTagValuePush"/>, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsCompatible(IAdapter adapter) {
            if (adapter == null) {
                return false;
            }

            return adapter.Features.Contains<IReadSnapshotTagValues>() && adapter.Features.Contains<ITagInfo>();
        }


        /// <summary>
        /// Adds a new <see cref="SimulatedSnapshotTagValuePush"/> to the features collection on 
        /// the provided <see cref="AdapterBase{TAdapterOptions}"/>.
        /// </summary>
        /// <typeparam name="TAdapterOptions">
        ///   The type of the adapter options.
        /// </typeparam>
        /// <param name="adapter">
        ///   The owning adapter.
        /// </param>
        /// <param name="pollingInterval">
        ///   The polling interval to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapter"/> does not implement the features required to simulate 
        ///   <see cref="ISnapshotTagValuePush"/>.
        /// </exception>
        public static void Register<TAdapterOptions>(
            AdapterBase<TAdapterOptions> adapter,
            TimeSpan pollingInterval
        ) where TAdapterOptions : AdapterOptions, new() {
            adapter.AddFeature(
                typeof(ISnapshotTagValuePush),
                new SimulatedSnapshotTagValuePush(adapter, pollingInterval, adapter?.TaskScheduler, adapter?.Logger)
            );
        }


        /// <inheritdoc/>
        protected override ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
            var feature = _adapter.Features.Get<ITagInfo>();
            if (feature == null) {
                var result = Channel.CreateUnbounded<TagIdentifier>();
                result.Writer.TryComplete();
                return result;
            }

            var tags = feature.GetTags(context, new GetTagsRequest() {
                Tags = tagNamesOrIds.ToArray()
            }, cancellationToken);

            return tags.Transform(tag => TagIdentifier.Create(tag?.Id, tag?.Name), TaskScheduler, cancellationToken);
        }


        /// <inheritdoc/>
        protected override async Task GetSnapshotTagValues(IEnumerable<string> tagIds, ChannelWriter<TagValueQueryResult> channel, CancellationToken cancellationToken) {
            var feature = _adapter.Features.Get<IReadSnapshotTagValues>();
            if (feature == null) {
                channel.TryComplete();
                return;
            }

            var values = feature.ReadSnapshotTagValues(null, new ReadSnapshotTagValuesRequest() {
                Tags = tagIds.ToArray()
            }, cancellationToken);

            await values.Forward(channel, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override void OnPollingError(Exception error) {
            Logger.LogError(error, Resources.Log_ErrorDuringSnapshotPushRefresh);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            // No action required.
        }

    }
}
