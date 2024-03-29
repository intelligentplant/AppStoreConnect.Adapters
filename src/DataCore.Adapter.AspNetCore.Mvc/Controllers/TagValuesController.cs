﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting tag data.
    /// </summary>
    [ApiController]
    [Area("app-store-connect")]
    [Route("api/[area]/v2.0/tag-values")]
    // Legacy route for compatibility with v1 of the toolkit
    [Route("api/data-core/v1.0/tag-values")]
    [UseAdapterRequestValidation(false)]
    public class TagValuesController: ControllerBase {

        /// <summary>
        /// Holds channels for updating active snapshot subscriptions.
        /// </summary>
        private static readonly ConcurrentDictionary<Guid, Channel<TagValueSubscriptionUpdate>> s_activeSubscriptions = new ConcurrentDictionary<Guid, Channel<TagValueSubscriptionUpdate>>();

        /// <summary>
        /// The service for accessing the running adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The service for registering background tasks.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;

        /// <summary>
        /// Default query time range to use in a historical query if a start or end time is not 
        /// specified on a route that accepts the time range as query string parameters.
        /// </summary>
        public static TimeSpan DefaultHistoricalQueryDuration { get; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Default number of samples or intervals to request in a historical query if this is not 
        /// specified on a route that accepts this value as a query string parameter.
        /// </summary>
        public const int DefaultSampleOrIntervalCount = 100;


        /// <summary>
        /// Creates a new <see cref="TagValuesController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for accessing running adapters.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   Service for registering background tasks.
        /// </param>
        public TagValuesController(IAdapterAccessor adapterAccessor, IBackgroundTaskService backgroundTaskService) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
            _backgroundTaskService = backgroundTaskService ?? throw new ArgumentNullException(nameof(backgroundTaskService));
        }


        /// <summary>
        /// Requests snapshot (current) tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="tag">
        ///   The tag IDs or names to poll.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the snapshot values for the requested tags.
        /// </returns>
        [HttpGet]
        [Route("{adapterId:maxlength(200)}/snapshot")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public Task<IActionResult> ReadSnapshotValues(string adapterId, [FromQuery] string[] tag = null!, CancellationToken cancellationToken = default) {
            var request = new ReadSnapshotTagValuesRequest() {
                Tags = tag ?? Array.Empty<string>()
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return ReadSnapshotValues(adapterId, request, cancellationToken);
        }


        /// <summary>
        /// Requests snapshot (current) tag values.
        /// </summary>
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
        [Route("{adapterId:maxlength(200)}/snapshot")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadSnapshotValues(string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadSnapshotTagValues>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadSnapshotTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            
            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.ReadSnapshotTagValues(callContext, request, cancellationToken)
            );
        }


        /// <summary>
        /// Requests raw (archived) tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="tag">
        ///   The tag IDs or names to poll.
        /// </param>
        /// <param name="start">
        ///   The UTC start time for the query.
        /// </param>
        /// <param name="end">
        ///   The UTC end time for the query.
        /// </param>
        /// <param name="count">
        ///   The maximum number of samples to retrieve per tag.
        /// </param>
        /// <param name="boundary">
        ///   The boundary type for the query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the raw values for the requested tags.
        /// </returns>
        [HttpGet]
        [Route("{adapterId:maxlength(200)}/raw")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public Task<IActionResult> ReadRawValues(
            string adapterId, 
            [FromQuery] string[] tag = null!, 
            DateTime? start = null, 
            DateTime? end = null, 
            int count = DefaultSampleOrIntervalCount, 
            RawDataBoundaryType boundary = RawDataBoundaryType.Inside, 
            CancellationToken cancellationToken = default
        ) {
            var now = DateTime.UtcNow;
            if (start == null && end == null) {
                end = now;
                start = now.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (start == null) {
                start = end!.Value.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (end == null) {
                end = start.Value.Add(DefaultHistoricalQueryDuration);
            }

            var request = new ReadRawTagValuesRequest() {
                Tags = tag ?? Array.Empty<string>(),
                UtcStartTime = Util.ConvertToUniversalTime(start.Value),
                UtcEndTime = Util.ConvertToUniversalTime(end.Value),
                SampleCount = count,
                BoundaryType = boundary
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return ReadRawValues(adapterId, request, cancellationToken);
        }


        /// <summary>
        /// Requests raw (archived) tag values.
        /// </summary>
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
        [Route("{adapterId:maxlength(200)}/raw")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadRawValues(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadRawTagValues>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadRawTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.ReadRawTagValues(callContext, request, cancellationToken)
            );
        }


        /// <summary>
        /// Requests plot (vizualization-friendly) tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="tag">
        ///   The tag IDs or names to poll.
        /// </param>
        /// <param name="start">
        ///   The UTC start time for the query.
        /// </param>
        /// <param name="end">
        ///   The UTC end time for the query.
        /// </param>
        /// <param name="count">
        ///   The number of intervals for the query (typically the pixel width of the chart that 
        ///   the data will be displayed on).
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
        [HttpGet]
        [Route("{adapterId:maxlength(200)}/plot")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public Task<IActionResult> ReadPlotValues(
            string adapterId, 
            [FromQuery] string[] tag = null!, 
            DateTime? start = null,
            DateTime? end = null,
            int count = DefaultSampleOrIntervalCount,
            CancellationToken cancellationToken = default
        ) {
            var now = DateTime.UtcNow;
            if (start == null && end == null) {
                end = now;
                start = now.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (start == null) {
                start = end!.Value.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (end == null) {
                end = start.Value.Add(DefaultHistoricalQueryDuration);
            }

            var request = new ReadPlotTagValuesRequest() {
                Tags = tag ?? Array.Empty<string>(),
                UtcStartTime = Util.ConvertToUniversalTime(start.Value),
                UtcEndTime = Util.ConvertToUniversalTime(end.Value),
                Intervals = count
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return ReadPlotValues(adapterId, request, cancellationToken);
        }


        /// <summary>
        /// Requests plot (vizualization-friendly) tag values.
        /// </summary>
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
        [Route("{adapterId:maxlength(200)}/plot")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadPlotValues(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadPlotTagValues>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadPlotTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.ReadPlotTagValues(callContext, request, cancellationToken)
            );
        }


        /// <summary>
        /// Requests tag values at specific timestamps.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="tag">
        ///   The tag IDs or names to poll.
        /// </param>
        /// <param name="time">
        ///   The UTC sample times to request values at.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the values for the requested tags at the requested times.
        /// </returns>
        [HttpGet]
        [Route("{adapterId:maxlength(200)}/values-at-times")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public Task<IActionResult> ReadValuesAtTimes(
            string adapterId, 
            [FromQuery] string[] tag = null!,
            [FromQuery] DateTime[] time = null!,
            CancellationToken cancellationToken = default
        ) {
            var request = new ReadTagValuesAtTimesRequest() {
                Tags = tag ?? Array.Empty<string>(),
                UtcSampleTimes = time?.Select(Util.ConvertToUniversalTime)?.ToArray() ?? Array.Empty<DateTime>()
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return ReadValuesAtTimes(adapterId, request, cancellationToken);
        }


        /// <summary>
        /// Requests tag values at specific timestamps.
        /// </summary>
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
        [Route("{adapterId:maxlength(200)}/values-at-times")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadValuesAtTimes(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadTagValuesAtTimes>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadTagValuesAtTimes))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.ReadTagValuesAtTimes(callContext, request, cancellationToken)
            );
        }


        /// <summary>
        /// Requests processed (aggregated) tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="tag">
        ///   The tag IDs or names to poll.
        /// </param>
        /// <param name="start">
        ///   The UTC start time for the query.
        /// </param>
        /// <param name="end">
        ///   The UTC end time for the query.
        /// </param>
        /// <param name="count">
        ///   The number of samples to request per tag. The sample interval for the query will be 
        ///   derived from this value.
        /// </param>
        /// <param name="function">
        ///   The data function IDs for the query.
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
        [HttpGet]
        [Route("{adapterId:maxlength(200)}/processed")]
        [ProducesResponseType(typeof(IAsyncEnumerable<ProcessedTagValueQueryResult>), 200)]
        public Task<IActionResult> ReadProcessedValues(
            string adapterId,
            [FromQuery] string[] tag = null!,
            DateTime? start = null,
            DateTime? end = null,
            int count = DefaultSampleOrIntervalCount,
            [FromQuery] string[] function = null!,
            CancellationToken cancellationToken = default
        ) {
            var now = DateTime.UtcNow;
            if (start == null && end == null) {
                end = now;
                start = now.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (start == null) {
                start = end!.Value.Subtract(DefaultHistoricalQueryDuration);
            }
            else if (end == null) {
                end = start.Value.Add(DefaultHistoricalQueryDuration);
            }

            if (count < 1) {
                count = 1;
            }

            var interval = TimeSpan.FromSeconds((end.Value - start.Value).TotalSeconds / count);

            var request = new ReadProcessedTagValuesRequest() {
                Tags = tag ?? Array.Empty<string>(),
                UtcStartTime = Util.ConvertToUniversalTime(start.Value),
                UtcEndTime = Util.ConvertToUniversalTime(end.Value),
                SampleInterval = interval,
                DataFunctions = function ?? Array.Empty<string>()
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);

            return ReadProcessedValues(adapterId, request, cancellationToken);
        }


        /// <summary>
        /// Requests processed (aggregated) tag values.
        /// </summary>
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
        [Route("{adapterId:maxlength(200)}/processed")]
        [ProducesResponseType(typeof(IAsyncEnumerable<ProcessedTagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadProcessedValues(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadProcessedTagValues>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadProcessedTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.ReadProcessedTagValues(callContext, request, cancellationToken)
            );
        }


        /// <summary>
        /// Requests the aggregate functions that can be specified when requesting processed data.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain descriptors for the aggregated function names that can 
        ///   be specified.
        /// </returns>
        /// <remarks>
        ///   Processed data queries are used to request aggregated values for tags. The functions 
        ///   supported vary by data source. The <see cref="DefaultDataFunctions"/> class defines
        ///   constants for commonly-supported aggregate functions.
        /// </remarks>
        /// <seealso cref="DefaultDataFunctions"/>
        [HttpGet]
        [Route("{adapterId:maxlength(200)}/supported-aggregations")]
        [ProducesResponseType(typeof(IAsyncEnumerable<DataFunctionDescriptor>), 200)]
        public Task<IActionResult> GetSupportedDataFunctions(string adapterId, CancellationToken cancellationToken) {
            return GetSupportedDataFunctions(adapterId, new GetSupportedDataFunctionsRequest(), cancellationToken);
        }


        /// <summary>
        /// Requests the aggregate functions that can be specified when requesting processed data.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain descriptors for the aggregated function names that can 
        ///   be specified.
        /// </returns>
        /// <remarks>
        ///   Processed data queries are used to request aggregated values for tags. The functions 
        ///   supported vary by data source. The <see cref="DefaultDataFunctions"/> class defines
        ///   constants for commonly-supported aggregate functions.
        /// </remarks>
        /// <seealso cref="DefaultDataFunctions"/>
        [HttpPost]
        [Route("{adapterId:maxlength(200)}/supported-aggregations")]
        [ProducesResponseType(typeof(IAsyncEnumerable<DataFunctionDescriptor>), 200)]
        public async Task<IActionResult> GetSupportedDataFunctions(string adapterId, GetSupportedDataFunctionsRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadProcessedTagValues>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadProcessedTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.GetSupportedDataFunctions(callContext, request, cancellationToken)
            );
        }


        /// <summary>
        /// Writes values to an adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to write to.
        /// </param>
        /// <param name="request">
        ///   The values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="WriteTagValueResult"/> 
        ///   objects (one per sample written).
        /// </returns>
        [HttpPost]
        [Route("{adapterId:maxlength(200)}/write/snapshot")]
        [ProducesResponseType(typeof(IAsyncEnumerable<WriteTagValueResult>), 200)]
        public async Task<IActionResult> WriteSnapshotValues(string adapterId, WriteTagValuesRequestExtended request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteSnapshotTagValues>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IWriteSnapshotTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            var channel = request.Values.PublishToChannel();

            return Util.StreamResults(
                feature.WriteSnapshotTagValues(callContext, request, channel.ReadAllAsync(cancellationToken), cancellationToken)
            );
        }


        /// <summary>
        /// Writes values to an adapter's historical archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to write to.
        /// </param>
        /// <param name="request">
        ///   The values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="WriteTagValueResult"/> 
        ///   objects (one per sample written).
        /// </returns>
        [HttpPost]
        [Route("{adapterId:maxlength(200)}/write/history")]
        [ProducesResponseType(typeof(IAsyncEnumerable<WriteTagValueResult>), 200)]
        public async Task<IActionResult> WriteHistoricalValues(string adapterId, WriteTagValuesRequestExtended request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteHistoricalTagValues>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IWriteHistoricalTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            
            var feature = resolvedFeature.Feature;

            var channel = request.Values.PublishToChannel();

            return Util.StreamResults(
                feature.WriteHistoricalTagValues(callContext, request, channel.ReadAllAsync(cancellationToken), cancellationToken)
            );
        } 

    }
}
