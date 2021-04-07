using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Tags;
using DataCore.Adapter.Tags;

using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="TagSearchService.TagSearchServiceBase"/>.
    /// </summary>
    public class TagSearchServiceImpl : TagSearchService.TagSearchServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagSearchServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public TagSearchServiceImpl(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task GetTagProperties(GetTagPropertiesRequest request, IServerStreamWriter<AdapterProperty> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.Tags.GetTagPropertiesRequest() {
                PageSize = request.PageSize,
                Page = request.Page,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartGetTagPropertiesActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = await adapter.Feature.GetTagProperties(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

                long outputItems = 0;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var prop) || prop == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(prop.ToGrpcAdapterProperty()).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task FindTags(FindTagsRequest request, IServerStreamWriter<TagDefinition> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagSearch>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.Tags.FindTagsRequest() {
                Name = request.Name,
                Description = request.Description,
                Units = request.Units,
                Label = request.Label,
                Other = request.Other,
                PageSize = request.PageSize,
                Page = request.Page,
                Properties = new Dictionary<string, string>(request.Properties),
                ResultFields = (TagDefinitionFields) request.ResultFields
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartFindTagsActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = await adapter.Feature.FindTags(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

                var outputItems = 0;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(tag.ToGrpcTagDefinition()).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task GetTags(GetTagsRequest request, IServerStreamWriter<TagDefinition> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.Tags.GetTagsRequest() {
                Tags = request.Tags.ToArray(),
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartGetTagsActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                var reader = await adapter.Feature.GetTags(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

                long outputCount = 0;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var tag) || tag == null) {
                        continue;
                    }

                    await responseStream.WriteAsync(tag.ToGrpcTagDefinition()).ConfigureAwait(false);
                    activity.SetResponseItemCountTag(++outputCount);
                }
            }
        }

    }
}
