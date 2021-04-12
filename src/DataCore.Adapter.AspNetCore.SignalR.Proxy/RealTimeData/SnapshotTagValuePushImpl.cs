using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="ISnapshotTagValuePush"/>.
    /// </summary>
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public SnapshotTagValuePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<TagValueQueryResult> Subscribe(
            IAdapterCallContext context, 
            CreateSnapshotTagValueSubscriptionRequest request, 
            IAsyncEnumerable<TagValueSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in client.TagValues.CreateSnapshotTagValueChannelAsync(
                    AdapterId,
                    request,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }
        
    }
}
