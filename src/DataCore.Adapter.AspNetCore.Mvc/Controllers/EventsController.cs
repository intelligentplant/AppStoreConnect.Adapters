using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for querying event messages on adapters.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/events")]
    public class EventsController : ControllerBase {

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the calling user.
        /// </summary>
        private readonly IAdapterCallContext _callContext;

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The maximum number of event messages that can be returned via an HTTP request.
        /// </summary>
        public const int MaxEventMessagesPerRequest = 1000;


        /// <summary>
        /// Creates a new <see cref="EventsController"/> object.
        /// </summary>
        /// <param name="callContext">
        ///   The <see cref="IAdapterCallContext"/> for the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public EventsController(IAdapterCallContext callContext, IAdapterAccessor adapterAccessor) {
            _callContext = callContext ?? throw new ArgumentNullException(nameof(callContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Reads historical event messages for the specified time range.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
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
        public async Task<IActionResult> ReadEventMessagesForTimeRange(ApiVersion apiVersion, string adapterId, ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadEventMessagesForTimeRange>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadEventMessagesForTimeRange))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;
            var reader = feature.ReadEventMessages(_callContext, request, cancellationToken);
            var result = new List<EventMessage>();

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                if (result.Count > MaxEventMessagesPerRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxEventMessagesPerRequest));
                    break;
                }

                result.Add(msg);
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Reads historical event messages starting at the specified cursor position.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
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
        public async Task<IActionResult> ReadEventMessagesByCursor(ApiVersion apiVersion, string adapterId, ReadEventMessagesUsingCursorRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadEventMessagesUsingCursor>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadEventMessagesUsingCursor))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;
            var reader = feature.ReadEventMessages(_callContext, request, cancellationToken);
            var result = new List<EventMessageWithCursorPosition>();

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var msg) || msg == null) {
                    continue;
                }

                if (result.Count > MaxEventMessagesPerRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxEventMessagesPerRequest));
                    break;
                }

                result.Add(msg);
            }

            return Ok(result); // 200
        }

    }
}
