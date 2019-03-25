using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Features;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Utilities {

    /// <summary>
    /// A helper class that can add support for <see cref="IReadInterpolatedTagValues"/>, 
    /// <see cref="IReadPlotTagValues"/> and <see cref="IReadPlotTagValues"/> support to a driver that 
    /// only natively supports <see cref="IReadRawTagValues"/>.
    /// </summary>
    /// <remarks>
    ///   Interpolated, plot, and processed data queries are handled by querying for raw tag values, 
    ///   and then using utility classes in the <see cref="Utilities"/> namespace to perform additional 
    ///   calculation or aggregation. Native implementations of the data queries will almost always 
    ///   perform better, and should be used if available.
    /// </remarks>
    public class ReadHistoricalTagValuesHelper : IReadInterpolatedTagValues, IReadPlotTagValues, IReadProcessedTagValues, IReadTagValuesAtTimes {

        /// <summary>
        /// The tag search provider.
        /// </summary>
        private readonly ITagSearch _tagSearchProvider;

        /// <summary>
        /// The raw data provider.
        /// </summary>
        private readonly IReadRawTagValues _rawValuesProvider;


        /// <summary>
        /// Creates a new <see cref="ReadHistoricalTagValuesHelper"/> object.
        /// </summary>
        /// <param name="tagSearchProvider">
        ///   The <see cref="ITagSearch"/> instance that will provide the tag definitions for tags 
        ///   being queried.
        /// </param>
        /// <param name="rawValuesProvider">
        ///   The <see cref="IReadRawTagValues"/> instance that will provide raw tag values to the 
        ///   helper.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagSearchProvider"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="rawValuesProvider"/> is <see langword="null"/>.
        /// </exception>
        public ReadHistoricalTagValuesHelper(ITagSearch tagSearchProvider, IReadRawTagValues rawValuesProvider) {
            _tagSearchProvider = tagSearchProvider ?? throw new ArgumentNullException(nameof(tagSearchProvider));
            _rawValuesProvider = rawValuesProvider ?? throw new ArgumentNullException(nameof(rawValuesProvider));
        }


        /// <inheritdoc/>
        public async Task<IEnumerable<HistoricalTagValues>> ReadInterpolatedTagValues(IDataCoreContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            var tagDefinitions = await _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                Tags = request.Tags
            }, cancellationToken).ConfigureAwait(false);

            var rawValues = await _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                Tags = tagDefinitions.Select(x => x.Id).ToArray(),
                UtcStartTime = request.UtcStartTime.Subtract(request.SampleInterval),
                UtcEndTime = request.UtcEndTime,
                SampleCount = 0,
                BoundaryType = RawDataBoundaryType.Outside
            }, cancellationToken).ConfigureAwait(false);

            return rawValues
                .Select(x => new {
                    Tag = tagDefinitions.FirstOrDefault(t => t.Id.Equals(x.TagId, StringComparison.OrdinalIgnoreCase)),
                    Values = x
                })
                .Where(x => x.Tag != null)
                .Select(x => new HistoricalTagValues(
                    x.Tag.Id,
                    x.Tag.Name,
                    InterpolationHelper.GetInterpolatedValues(x.Tag, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, InterpolationCalculationType.Interpolate, x.Values.Values)
                )).ToArray();
        }


        /// <inheritdoc/>
        public async Task<IEnumerable<HistoricalTagValues>> ReadPlotTagValues(IDataCoreContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var tagDefinitions = await _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                Tags = request.Tags
            }, cancellationToken).ConfigureAwait(false);

            var bucketSize = PlotHelper.CalculateBucketSize(request.UtcStartTime, request.UtcEndTime, request.Intervals);

            var rawValues = await _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                Tags = tagDefinitions.Select(x => x.Id).ToArray(),
                UtcStartTime = request.UtcStartTime.Subtract(bucketSize),
                UtcEndTime = request.UtcEndTime,
                SampleCount = 0,
                BoundaryType = RawDataBoundaryType.Outside
            }, cancellationToken).ConfigureAwait(false);

            return rawValues
                .Select(x => new {
                    Tag = tagDefinitions.FirstOrDefault(t => t.Id.Equals(x.TagId, StringComparison.OrdinalIgnoreCase)),
                    Values = x
                })
                .Where(x => x.Tag != null)
                .Select(x => new HistoricalTagValues(
                    x.Tag.Id,
                    x.Tag.Name,
                    PlotHelper.GetPlotValues(x.Tag, request.UtcStartTime, request.UtcEndTime, request.Intervals, x.Values.Values)
                )).ToArray();
        }


        /// <inheritdoc/>
        public Task<IEnumerable<string>> GetSupportedDataFunctions(IDataCoreContext context, CancellationToken cancellationToken) {
            return Task.FromResult(AggregationHelper.GetSupportedDataFunctions());
        }


        /// <inheritdoc/>
        public async Task<IEnumerable<ProcessedHistoricalTagValues>> ReadProcessedTagValues(IDataCoreContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var tagDefinitions = await _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                Tags = request.Tags
            }, cancellationToken).ConfigureAwait(false);

            var rawValues = await _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                Tags = tagDefinitions.Select(x => x.Id).ToArray(),
                UtcStartTime = request.UtcStartTime.Subtract(request.SampleInterval),
                UtcEndTime = request.UtcEndTime,
                SampleCount = 0,
                BoundaryType = RawDataBoundaryType.Outside
            }, cancellationToken).ConfigureAwait(false);

            return rawValues
                .Select(x => new {
                    Tag = tagDefinitions.FirstOrDefault(t => t.Id.Equals(x.TagId, StringComparison.OrdinalIgnoreCase)),
                    Values = x
                })
                .Where(x => x.Tag != null)
                .Select(x => new ProcessedHistoricalTagValues(
                    x.Tag.Id,
                    x.Tag.Name,
                    request.DataFunctions.ToDictionary(
                        f => f,
                        f => AggregationHelper.GetAggregatedValues(x.Tag, f, request.UtcStartTime, request.UtcEndTime, request.SampleInterval, x.Values.Values)
                    )
                )).ToArray();
        }


        /// <inheritdoc/>
        public async Task<IEnumerable<HistoricalTagValues>> ReadTagValuesAtTimes(IDataCoreContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var tagDefinitions = await _tagSearchProvider.GetTags(context, new GetTagsRequest() {
                Tags = request.Tags
            }, cancellationToken).ConfigureAwait(false);

            var valuesByTag = tagDefinitions.ToDictionary(x => x, x => new List<TagValue>());

            foreach (var sampleTime in request.UtcSampleTimes.Distinct()) {
                var rawValues = await _rawValuesProvider.ReadRawTagValues(context, new ReadRawTagValuesRequest() {
                    Tags = tagDefinitions.Select(x => x.Id).ToArray(),
                    UtcStartTime = sampleTime.AddSeconds(-1),
                    UtcEndTime = sampleTime.AddSeconds(1),
                    SampleCount = 0,
                    BoundaryType = RawDataBoundaryType.Outside
                }, cancellationToken).ConfigureAwait(false);

                foreach (var item in rawValues) {
                    var tag = tagDefinitions.FirstOrDefault(x => x.Id.Equals(item.TagId));
                    if (tag == null || !valuesByTag.TryGetValue(tag, out var vals)) {
                        continue;
                    }
                    vals.Add(InterpolationHelper.GetValueAtTime(tag, sampleTime, item.Values, tag.DataType == TagDataType.Numeric ? InterpolationCalculationType.Interpolate : InterpolationCalculationType.UsePreviousValue));
                }
            }

            return valuesByTag.Select(x => new HistoricalTagValues(x.Key.Id, x.Key.Name, x.Value));
        }

    }
}
