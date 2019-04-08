using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting tag data.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/tag-values")]
    public class TagValuesController: ControllerBase {

        /// <summary>
        /// The adapter API authorization service to use.
        /// </summary>
        private readonly AdapterApiAuthorizationService _authorizationService;

        /// <summary>
        /// The Data Core context for the caller.
        /// </summary>
        private readonly IAdapterCallContext _callContext;

        /// <summary>
        /// The service for accessing the running adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagValuesController"/> object.
        /// </summary>
        /// <param name="authorizationService">
        ///   The adapter API authorization service to use.
        /// </param>
        /// <param name="callContext">
        ///   The Data Core context for the caller.
        /// </param>
        /// <param name="adapterAccessor">
        ///   The service for accessing running adapters.
        /// </param>
        public TagValuesController(AdapterApiAuthorizationService authorizationService, IAdapterCallContext callContext, IAdapterAccessor adapterAccessor) {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _callContext = callContext ?? throw new ArgumentNullException(nameof(callContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Requests snapshot (current) tag values.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The snapshot data request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the snapshot values for the requested tags.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/snapshot")]
        [ProducesResponseType(typeof(IEnumerable<SnapshotTagValue>), 200)]
        public async Task<IActionResult> ReadSnapshotValues(ApiVersion apiVersion, string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadSnapshotTagValues>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadSnapshotTagValues))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IReadSnapshotTagValues>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var result = await feature.ReadSnapshotTagValues(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }


        /// <summary>
        /// Requests raw (archived) tag values.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The raw data request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the raw values for the requested tags.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/raw")]
        [ProducesResponseType(typeof(IEnumerable<HistoricalTagValues>), 200)]
        public async Task<IActionResult> ReadRawValues(ApiVersion apiVersion, string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadRawTagValues>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadRawTagValues))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IReadRawTagValues>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var result = await feature.ReadRawTagValues(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }


        /// <summary>
        /// Requests plot (vizualization-friendly) tag values.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The plot data request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the plot values for the requested tags.
        /// </returns>
        /// <remarks>
        ///   Plot data is intended to provide visualization-friendly data sets for display in e.g. 
        ///   charts.
        /// </remarks>
        [HttpPost]
        [Route("{adapterId}/plot")]
        [ProducesResponseType(typeof(IEnumerable<HistoricalTagValues>), 200)]
        public async Task<IActionResult> ReadPlotValues(ApiVersion apiVersion, string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadPlotTagValues>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadPlotTagValues))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IReadPlotTagValues>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var result = await feature.ReadPlotTagValues(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }


        /// <summary>
        /// Requests interpolated tag values.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="request">
        ///   The interpolated data request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the interpolated values for the requested tags.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/interpolated")]
        [ProducesResponseType(typeof(IEnumerable<HistoricalTagValues>), 200)]
        public async Task<IActionResult> ReadInterpolatedValues(ApiVersion apiVersion, string adapterId, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadInterpolatedTagValues>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadInterpolatedTagValues))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IReadInterpolatedTagValues>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var result = await feature.ReadInterpolatedTagValues(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }


        /// <summary>
        /// Requests tag values at specific timestamps.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The values-at-times data request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the values for the requested tags at the requested times.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/values-at-times")]
        [ProducesResponseType(typeof(IEnumerable<HistoricalTagValues>), 200)]
        public async Task<IActionResult> ReadValuesAtTimes(ApiVersion apiVersion, string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadTagValuesAtTimes>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadTagValuesAtTimes))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IReadTagValuesAtTimes>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var result = await feature.ReadTagValuesAtTimes(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }


        /// <summary>
        /// Requests processed (aggregated) tag values.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The processed data request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the aggregated values for the requested tags and data 
        ///   functions.
        /// </returns>
        /// <remarks>
        ///   Processed data queries are used to request aggregated values for tags. The functions 
        ///   supported vary by data source. The <see cref="DefaultDataFunctions"/> class defines
        ///   constants for commonly-supported aggregate functions.
        /// </remarks>
        /// <seealso cref="DefaultDataFunctions"/>
        [HttpPost]
        [Route("{adapterId}/processed")]
        [ProducesResponseType(typeof(IEnumerable<ProcessedHistoricalTagValues>), 200)]
        public async Task<IActionResult> ReadProcessedValues(ApiVersion apiVersion, string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadProcessedTagValues>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadProcessedTagValues))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IReadProcessedTagValues>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var result = await feature.ReadProcessedTagValues(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }


        /// <summary>
        /// Requests the aggregate functions that can be specified when requesting processed data.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the aggregated function names that can be specified.
        /// </returns>
        /// <remarks>
        ///   Processed data queries are used to request aggregated values for tags. The functions 
        ///   supported vary by data source. The <see cref="DefaultDataFunctions"/> class defines
        ///   constants for commonly-supported aggregate functions.
        /// </remarks>
        /// <seealso cref="DefaultDataFunctions"/>
        [HttpGet]
        [Route("{adapterId}/supported-aggregations")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        public async Task<IActionResult> GetSupportedAggregateFunctions(ApiVersion apiVersion, string adapterId, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<IReadProcessedTagValues>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadProcessedTagValues))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<IReadProcessedTagValues>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var result = await feature.GetSupportedDataFunctions(_callContext, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }

    }
}
