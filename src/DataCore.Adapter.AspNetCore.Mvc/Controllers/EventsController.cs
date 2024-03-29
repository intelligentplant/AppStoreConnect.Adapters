﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

using IntelligentPlant.BackgroundTasks;

using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for querying event messages on adapters.
    /// </summary>
    [ApiController]
    [Area("app-store-connect")]
    [Route("api/[area]/v2.0/events")]
    // Legacy route for compatibility with v1 of the toolkit
    [Route("api/data-core/v1.0/events")]
    [UseAdapterRequestValidation(false)]
    public class EventsController : ControllerBase {

        /// <summary>
        /// Holds channels for updating active topic-based event subscriptions.
        /// </summary>
        private static readonly ConcurrentDictionary<Guid, Channel<EventMessageSubscriptionUpdate>> s_activeSubscriptions = new ConcurrentDictionary<Guid, Channel<EventMessageSubscriptionUpdate>>();


        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// For registering background tasks.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;


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
        [Route("{adapterId:maxlength(200)}/by-time-range")]
        [ProducesResponseType(typeof(IAsyncEnumerable<EventMessage>), 200)]
        public async Task<IActionResult> ReadEventMessagesForTimeRange(string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadEventMessagesForTimeRange>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadEventMessagesForTimeRange))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;
            return Util.StreamResults(feature.ReadEventMessagesForTimeRange(callContext, request, cancellationToken));
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
        [Route("{adapterId:maxlength(200)}/by-cursor")]
        [ProducesResponseType(typeof(IAsyncEnumerable<EventMessageWithCursorPosition>), 200)]
        public async Task<IActionResult> ReadEventMessagesByCursor(string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadEventMessagesUsingCursor>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadEventMessagesUsingCursor))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.ReadEventMessagesUsingCursor(callContext, request, cancellationToken)
            );
        }


        /// <summary>
        /// Writes event messages to an adapter.
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
        [HttpPost]
        [Route("{adapterId:maxlength(200)}/write")]
        [ProducesResponseType(typeof(IAsyncEnumerable<WriteEventMessageResult>), 200)]
        public async Task<IActionResult> WriteEventMessages(string adapterId, WriteEventMessagesRequestExtended request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteEventMessages>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IWriteEventMessages))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            var channel = request.Events.PublishToChannel();

            return Util.StreamResults(
                feature.WriteEventMessages(callContext, request, channel.ReadAllAsync(cancellationToken), cancellationToken)
            );
        }

    }
}
