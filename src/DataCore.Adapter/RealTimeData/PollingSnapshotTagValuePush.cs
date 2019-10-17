using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extends <see cref="SnapshotTagValuePush"/> to provide a snapshot subscription 
    /// manager that periodically polls for the values of tags that are currently being 
    /// subscribed to. This can be used to provide push capabilities in an adapter that 
    /// e.g. only natively supports the <see cref="IReadSnapshotTagValues"/> feature.
    /// </summary>
    public abstract class PollingSnapshotTagValuePush : SnapshotTagValuePush {

        /// <summary>
        /// Default polling interval to use.
        /// </summary>
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The polling interval.
        /// </summary>
        private readonly TimeSpan _pollingInterval;


        /// <summary>
        /// Creates a new <see cref="PollingSnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="pollingInterval">
        ///   The interval between polling queries. If less than or equal to <see cref="TimeSpan.Zero"/>, 
        ///   <see cref="DefaultPollingInterval"/> will be used.
        /// </param>
        /// <param name="logger">
        ///   The logger for the subscription manager.
        /// </param>
        protected PollingSnapshotTagValuePush(TimeSpan pollingInterval, ILogger logger) : base(logger) {
            _pollingInterval = pollingInterval <= TimeSpan.Zero
                ? DefaultPollingInterval
                : pollingInterval;

            // Run the dedicated polling task.
            _ = Task.Factory.StartNew(
                () => RunSnapshotPollingLoop(DisposedToken),
                DisposedToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }


        /// <summary>
        /// Runs the dedicated polling task.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The system shutdown token.
        /// </param>
        /// <returns>
        ///   A task that will run the polling loop.
        /// </returns>
        private async Task RunSnapshotPollingLoop(CancellationToken cancellationToken) {
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    await Task.Delay(_pollingInterval, cancellationToken).ConfigureAwait(false);

                    try {
                        var tags = await GetSubscribedTags(cancellationToken).ConfigureAwait(false);
                        if (tags == null || !tags.Any()) {
                            return;
                        }

                        var channel = CreateChannel(5000, BoundedChannelFullMode.Wait);
                        channel.Writer.RunBackgroundOperation((ch, ct) => GetSnapshotTagValues(tags, ch, ct), true, cancellationToken);
                        
                        while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false) && channel.Reader.TryRead(out var val)) {
                            OnValueChanged(val);
                        }
                    }
                    catch (ChannelClosedException) {
                        // Channel was closed.
                    }
                    catch (OperationCanceledException) {
                        // Cancellation token fired.
                    }
                    catch (Exception e) {
                        Logger.LogError(e, Resources.Log_ErrorInSnapshotSubscriptionManagerPublishLoop);
                        OnPollingError(e);
                    }
                }
            }
            catch (OperationCanceledException) {
                // Cancellation token fired.
            }
        }


        /// <summary>
        /// Called if an error occurs while the subscription manager is polling for new tag values.
        /// </summary>
        /// <param name="error">
        ///   The error.
        /// </param>
        protected abstract void OnPollingError(Exception error);


        /// <summary>
        /// Gets the snapshot values for the specified tags.
        /// </summary>
        /// <param name="tagIds">
        ///   The tag IDs to query.
        /// </param>
        /// <param name="channel">
        ///   The channel to write the snapshot values to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The snapshot values for the tags.
        /// </returns>
        protected abstract Task GetSnapshotTagValues(IEnumerable<string> tagIds, ChannelWriter<TagValueQueryResult> channel, CancellationToken cancellationToken);


        /// <inheritdoc/>
        protected override async Task OnSubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            var channel = CreateChannel(5000, BoundedChannelFullMode.Wait);
            channel.Writer.RunBackgroundOperation((ch, ct) => GetSnapshotTagValues(tagIds, ch, ct), true, cancellationToken);
            
            while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!channel.Reader.TryRead(out var val) || val == null) {
                    continue;
                }

                OnValueChanged(val);
            }
        }


        /// <inheritdoc/>
        protected override Task OnUnsubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
