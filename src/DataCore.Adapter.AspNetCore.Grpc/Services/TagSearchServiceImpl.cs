using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Tags;
using DataCore.Adapter.Tags;

using Grpc.Core;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

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
        /// The JSON serialization options to use.
        /// </summary>
        private readonly JsonSerializerOptions? _jsonOptions;


        /// <summary>
        /// Creates a new <see cref="TagSearchServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        /// <param name="jsonOptions">
        ///   The configured JSON options.
        /// </param>
        public TagSearchServiceImpl(IAdapterAccessor adapterAccessor, IOptions<JsonOptions> jsonOptions) {
            _adapterAccessor = adapterAccessor;
            _jsonOptions = jsonOptions?.Value?.SerializerOptions;
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
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.GetTagProperties(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcAdapterProperty()).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
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
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.FindTags(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcTagDefinition()).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
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
                long outputItems = 0;
                try {
                    await foreach (var item in adapter.Feature.GetTags(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }

                        ++outputItems;
                        await responseStream.WriteAsync(item.ToGrpcTagDefinition()).ConfigureAwait(false);
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(outputItems);
                }
            }
        }


        /// <inheritdoc/>
        public override async Task<GetTagSchemaResponse> GetTagSchema(GetTagSchemaRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.Tags.GetTagSchemaRequest() {
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (Telemetry.ActivitySource.StartGetTagSchemaActivity(adapter.Adapter.Descriptor.Id)) {
                var result = await adapter.Feature.GetTagSchemaAsync(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                return new GetTagSchemaResponse() {
                    Schema = result.ToProtoValue()
                };
            }
        }


        /// <inheritdoc/>
        public override async Task<TagDefinition> CreateTag(CreateTagRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.Tags.CreateTagRequest() {
                Properties = new Dictionary<string, string>(request.Properties),
                Body = request.Body.ToJsonElement() ?? default
            };
            Util.ValidateObject(adapterRequest);

            using (Telemetry.ActivitySource.StartCreateTagActivity(adapter.Adapter.Descriptor.Id)) {
               JsonElement schema;

                using (Telemetry.ActivitySource.StartGetTagSchemaActivity(adapter.Adapter.Descriptor.Id)) {
                    schema = await adapter.Feature.GetTagSchemaAsync(adapterCallContext, new Tags.GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);
                }

                if (!Json.Schema.JsonSchemaUtility.TryValidate(adapterRequest.Body, schema, _jsonOptions, out var validationResults)) {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, JsonSerializer.Serialize(validationResults, _jsonOptions)));
                }

                var result = await adapter.Feature.CreateTagAsync(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                return result.ToGrpcTagDefinition();
            }
        }


        /// <inheritdoc/>
        public override async Task<TagDefinition> UpdateTag(UpdateTagRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.Tags.UpdateTagRequest() {
                Properties = new Dictionary<string, string>(request.Properties),
                Tag = request.Tag,
                Body = request.Body.ToJsonElement() ?? default
            };
            Util.ValidateObject(adapterRequest);

            using (Telemetry.ActivitySource.StartUpdateTagActivity(adapter.Adapter.Descriptor.Id, request.Tag)) {
                JsonElement schema;

                using (Telemetry.ActivitySource.StartGetTagSchemaActivity(adapter.Adapter.Descriptor.Id)) {
                    schema = await adapter.Feature.GetTagSchemaAsync(adapterCallContext, new Tags.GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);
                }

                if (!Json.Schema.JsonSchemaUtility.TryValidate(adapterRequest.Body, schema, _jsonOptions, out var validationResults)) {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, JsonSerializer.Serialize(validationResults, _jsonOptions)));
                }

                var result = await adapter.Feature.UpdateTagAsync(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                return result.ToGrpcTagDefinition();
            }
        }


        /// <inheritdoc/>
        public override async Task<DeleteTagResponse> DeleteTag(DeleteTagRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.Tags.DeleteTagRequest() {
                Tag = request.Tag,
                Properties = new Dictionary<string, string>(request.Properties)
            };
            Util.ValidateObject(adapterRequest);

            using (Telemetry.ActivitySource.StartDeleteTagActivity(adapter.Adapter.Descriptor.Id, adapterRequest.Tag)) {
                var result = await adapter.Feature.DeleteTagAsync(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                return new DeleteTagResponse() {
                    Success = result
                };
            }
        }

    }
}
