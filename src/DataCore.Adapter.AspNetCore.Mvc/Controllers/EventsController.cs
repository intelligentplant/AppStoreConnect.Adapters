using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using IntelligentPlant.BackgroundTasks;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for querying event messages on adapters.
    /// </summary>
    [ApiController]
    [Area("data-core")]
    [Route("api/[area]/v1.0/events")]
    public class EventsController : ControllerBase {

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// For registering background tasks.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;

        /// <summary>
        /// The maximum number of event messages that can be returned via an HTTP request.
        /// </summary>
        public const int MaxEventMessagesPerReadRequest = 1000;

        /// <summary>
        /// The maximum number of event messages that can be written in a single HTTP request.
        /// </summary>
        public const int MaxEventMessagesPerWriteRequest = 1000;


        /// <summary>
        /// Creates a new <see cref="EventsController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   Service for registering background tasks.
        /// </param>
        public EventsController(IAdapterAccessor adapterAccessor, IBackgroundTaskService backgroundTaskService) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
            _backgroundTaskService = backgroundTaskService ?? throw new ArgumentNullException(nameof(backgroundTaskService));
        }


        /// <summary>
        /// Reads historical event messages for the specified time range.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="EventMessage"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/by-time-range")]
        [ProducesResponseType(typeof(IEnumerable<EventMessage>), 200)]
        public async Task<IActionResult> ReadEventMessagesForTimeRange(string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadEventMessagesForTimeRange>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadEventMessagesForTimeRange))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;
            var reader = await feature.ReadEventMessages(callContext, request, cancellationToken).ConfigureAwait(false);
            var result = new List<EventMessage>();

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                if (result.Count > MaxEventMessagesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(callContext.CultureInfo, Resources.Warning_MaxResponseItemsReached, MaxEventMessagesPerReadRequest));
                    break;
                }

                result.Add(msg);
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Reads historical event messages starting at the specified cursor position.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="EventMessageWithCursorPosition"/> 
        ///   objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/by-cursor")]
        [ProducesResponseType(typeof(IEnumerable<EventMessageWithCursorPosition>), 200)]
        public async Task<IActionResult> ReadEventMessagesByCursor(string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadEventMessagesUsingCursor>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadEventMessagesUsingCursor))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;
            var reader = await feature.ReadEventMessages(callContext, request, cancellationToken).ConfigureAwait(false);
            var result = new List<EventMessageWithCursorPosition>();

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                if (result.Count > MaxEventMessagesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(callContext.CultureInfo, Resources.Warning_MaxResponseItemsReached, MaxEventMessagesPerReadRequest));
                    break;
                }

                result.Add(msg);
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Writes event messages to an adapter. Up to <see cref="MaxEventMessagesPerWriteRequest"/> 
        /// messages can be written in a single request.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to write to.
        /// </param>
        /// <param name="request">
        ///   The event messages to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="WriteEventMessageResult"/> 
        ///   objects (one per sample written).
        /// </returns>
        /// <remarks>
        ///   Up to <see cref="MaxEventMessagesPerWriteRequest"/> values can be written to the 
        ///   adapter in a single request. Subsequent values will be ignored. No corresponding 
        ///   <see cref="WriteEventMessageResult"/> object will be returned for these items.
        /// </remarks>
        [HttpPost]
        [Route("{adapterId}/write")]
        [ProducesResponseType(typeof(IEnumerable<WriteEventMessageResult>), 200)]
        public async Task<IActionResult> WriteEventMessages(string adapterId, WriteEventMessagesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteEventMessages>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IWriteEventMessages))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;

            var writeChannel = ChannelExtensions.CreateEventMessageWriteChannel(MaxEventMessagesPerWriteRequest);

            writeChannel.Writer.RunBackgroundOperation(async (ch, ct) => {
                var itemsWritten = 0;

                foreach (var evt in request.Events) {
                    ++itemsWritten;

                    if (evt == null) {
                        continue;
                    }

                    if (!await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        break;
                    }

                    ch.TryWrite(evt);

                    if (itemsWritten >= MaxEventMessagesPerWriteRequest) {
                        Util.AddIncompleteResponseHeader(Response, string.Format(callContext.CultureInfo, Resources.Warning_MaxResponseItemsReached, MaxEventMessagesPerWriteRequest));
                        break;
                    }
                }
            }, true, _backgroundTaskService, cancellationToken);

            var resultChannel = await feature.WriteEventMessages(callContext, writeChannel, cancellationToken).ConfigureAwait(false);
            var result = new List<WriteEventMessageResult>(MaxEventMessagesPerWriteRequest);

            while (await resultChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (resultChannel.TryRead(out var res) && res != null) {
                    result.Add(res);
                }
            }

            return Ok(result); // 200
        }

    }
}
