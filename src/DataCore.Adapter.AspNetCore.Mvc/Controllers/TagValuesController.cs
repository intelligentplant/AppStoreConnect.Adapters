using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting tag data.
    /// </summary>
    [ApiController]
    [Area("data-core")]
    [Route("api/[area]/v1.0/tag-values")]
    public class TagValuesController: ControllerBase {

        /// <summary>
        /// The Data Core context for the caller.
        /// </summary>
        private readonly IAdapterCallContext _callContext;

        /// <summary>
        /// The service for accessing the running adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The maximum number of samples that can be requested overall per request.
        /// </summary>
        public const int MaxSamplesPerReadRequest = 20000;

        /// <summary>
        /// The maximum number of samples that can be written per request.
        /// </summary>
        public const int MaxSamplesPerWriteRequest = 5000;


        /// <summary>
        /// Creates a new <see cref="TagValuesController"/> object.
        /// </summary>
        /// <param name="callContext">
        ///   The Data Core context for the caller.
        /// </param>
        /// <param name="adapterAccessor">
        ///   The service for accessing running adapters.
        /// </param>
        public TagValuesController(IAdapterCallContext callContext, IAdapterAccessor adapterAccessor) {
            _callContext = callContext ?? throw new ArgumentNullException(nameof(callContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
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
        [Route("{adapterId}/snapshot")]
        [ProducesResponseType(typeof(IEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadSnapshotValues(string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadSnapshotTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadSnapshotTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var reader = feature.ReadSnapshotTagValues(_callContext, request, cancellationToken);

            var result = new List<TagValueQueryResult>(request.Tags.Length);
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                if (result.Count > MaxSamplesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerReadRequest));
                    break;
                }

                result.Add(value);
            }

            return Ok(result); // 200
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
        [Route("{adapterId}/raw")]
        [ProducesResponseType(typeof(IEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadRawValues(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadRawTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadRawTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;
            var reader = feature.ReadRawTagValues(_callContext, request, cancellationToken);

            var result = new List<TagValueQueryResult>();
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                if (result.Count > MaxSamplesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerReadRequest));
                    break;
                }

                result.Add(value);
            }

            return Ok(result); // 200
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
        [Route("{adapterId}/plot")]
        [ProducesResponseType(typeof(IEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadPlotValues(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadPlotTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadPlotTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;

            var reader = feature.ReadPlotTagValues(_callContext, request, cancellationToken);

            var result = new List<TagValueQueryResult>();
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                if (result.Count > MaxSamplesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerReadRequest));
                    break;
                }

                result.Add(value);
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Requests interpolated tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
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
        [ProducesResponseType(typeof(IEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadInterpolatedValues(string adapterId, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadInterpolatedTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadInterpolatedTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var reader = feature.ReadInterpolatedTagValues(_callContext, request, cancellationToken);

            var result = new List<TagValueQueryResult>();
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                if (result.Count > MaxSamplesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerReadRequest));
                    break;
                }

                result.Add(value);
            }

            return Ok(result); // 200
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
        [Route("{adapterId}/values-at-times")]
        [ProducesResponseType(typeof(IEnumerable<TagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadValuesAtTimes(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadTagValuesAtTimes>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadTagValuesAtTimes))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var reader = feature.ReadTagValuesAtTimes(_callContext, request, cancellationToken);

            var result = new List<TagValueQueryResult>();
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                if (result.Count > MaxSamplesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerReadRequest));
                    break;
                }

                result.Add(value);
            }

            return Ok(result); // 200
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
        [Route("{adapterId}/processed")]
        [ProducesResponseType(typeof(IEnumerable<ProcessedTagValueQueryResult>), 200)]
        public async Task<IActionResult> ReadProcessedValues(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadProcessedTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadProcessedTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }

            var feature = resolvedFeature.Feature;

            var reader = feature.ReadProcessedTagValues(_callContext, request, cancellationToken);

            var result = new List<ProcessedTagValueQueryResult>();
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                if (result.Count > MaxSamplesPerReadRequest) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerReadRequest));
                    break;
                }

                result.Add(value);
            }

            return Ok(result); // 200
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
        public async Task<IActionResult> GetSupportedAggregateFunctions(string adapterId, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadProcessedTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadProcessedTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var result = await feature.GetSupportedDataFunctions(_callContext, cancellationToken).ConfigureAwait(false);
            return Ok(result); // 200
        }


        /// <summary>
        /// Writes values to an adapter's snapshot. Up to <see cref="MaxSamplesPerWriteRequest"/> 
        /// values can be written in a single request.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to write to.
        /// </param>
        /// <param name="values">
        ///   The values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="WriteTagValueResult"/> 
        ///   objects (one per sample written).
        /// </returns>
        /// <remarks>
        ///   Up to <see cref="MaxSamplesPerWriteRequest"/> values can be written to the adapter 
        ///   in a single request. Subsequent values will be ignored. No corresponding 
        ///   <see cref="WriteTagValueResult"/> object will be returned for these items.
        /// </remarks>
        [HttpPost]
        [Route("{adapterId}/write/snapshot")]
        [ProducesResponseType(typeof(IEnumerable<WriteTagValueResult>), 200)]
        public async Task<IActionResult> WriteSnapshotValues(string adapterId, IEnumerable<WriteTagValueItem> values, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteSnapshotTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IWriteSnapshotTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var writeChannel = ChannelExtensions.CreateTagValueWriteChannel(MaxSamplesPerWriteRequest);

            writeChannel.Writer.RunBackgroundOperation(async (ch, ct) => {
                var itemsWritten = 0;

                foreach (var value in values) {
                    ++itemsWritten;

                    if (value == null) {
                        continue;
                    }

                    if (!await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        break;
                    }

                    ch.TryWrite(value);

                    if (itemsWritten >= MaxSamplesPerWriteRequest) {
                        Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerWriteRequest));
                        break;
                    }
                }
            }, true, cancellationToken);

            var resultChannel = feature.WriteSnapshotTagValues(_callContext, writeChannel, cancellationToken);

            var result = new List<WriteTagValueResult>(MaxSamplesPerWriteRequest);

            while (await resultChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (resultChannel.TryRead(out var res) && res != null) {
                    result.Add(res);
                }
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Writes values to an adapter's historical archive. Up to <see cref="MaxSamplesPerWriteRequest"/>
        /// values can be written in a single request.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to write to.
        /// </param>
        /// <param name="values">
        ///   The values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="WriteTagValueResult"/> 
        ///   objects (one per sample written).
        /// </returns>
        /// <remarks>
        ///   Up to <see cref="MaxSamplesPerWriteRequest"/> values can be written to the adapter 
        ///   in a single request. Subsequent values will be ignored. No corresponding 
        ///   <see cref="WriteTagValueResult"/> object will be returned for these items.
        /// </remarks>
        [HttpPost]
        [Route("{adapterId}/write/history")]
        [ProducesResponseType(typeof(IEnumerable<WriteTagValueResult>), 200)]
        public async Task<IActionResult> WriteHistoricalValues(string adapterId, IEnumerable<WriteTagValueItem> values, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteHistoricalTagValues>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IWriteHistoricalTagValues))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var writeChannel = ChannelExtensions.CreateTagValueWriteChannel(MaxSamplesPerWriteRequest);

            writeChannel.Writer.RunBackgroundOperation(async (ch, ct) => {
                var itemsWritten = 0;

                foreach (var value in values) {
                    ++itemsWritten;

                    if (value == null) {
                        continue;
                    }

                    if (!await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        break;
                    }

                    ch.TryWrite(value);

                    if (itemsWritten >= MaxSamplesPerWriteRequest) {
                        Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxSamplesPerWriteRequest));
                        break;
                    }
                }
            }, true, cancellationToken);

            var resultChannel = feature.WriteHistoricalTagValues(_callContext, writeChannel, cancellationToken);

            var result = new List<WriteTagValueResult>(MaxSamplesPerWriteRequest);

            while (await resultChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (resultChannel.TryRead(out var res) && res != null) {
                    result.Add(res);
                }
            }

            return Ok(result); // 200
        }

    }
}
