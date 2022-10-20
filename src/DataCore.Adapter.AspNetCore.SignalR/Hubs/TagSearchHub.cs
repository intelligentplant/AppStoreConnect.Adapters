using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Tags;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for tag search queries.

    public partial class AdapterHub {

        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tags.
        /// </returns>
        public async IAsyncEnumerable<TagDefinition> FindTags(
            string adapterId, 
            FindTagsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagSearch>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartFindTagsActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.FindTags(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }


        /// <summary>
        /// Gets tags by ID or name.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tags.
        /// </returns>
        public async IAsyncEnumerable<TagDefinition> GetTags(
            string adapterId, 
            GetTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartGetTagsActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.GetTags(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }


        /// <summary>
        /// Gets tag property definitions.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tags.
        /// </returns>
        public async IAsyncEnumerable<AdapterProperty> GetTagProperties(
            string adapterId, 
            GetTagPropertiesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartGetTagPropertiesActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.GetTagProperties(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
            }
        }


        public async Task<System.Text.Json.JsonElement> GetTagSchema(string adapterId, GetTagSchemaRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartGetTagSchemaActivity(adapter.Adapter.Descriptor.Id)) {
                return await adapter.Feature.GetTagSchemaAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
            }
        }


        public async Task<TagDefinition> CreateTag(string adapterId, CreateTagRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartCreateTagActivity(adapter.Adapter.Descriptor.Id)) {
                System.Text.Json.JsonElement schema;
                using (Telemetry.ActivitySource.StartGetTagSchemaActivity(adapter.Adapter.Descriptor.Id)) {
                    schema = await adapter.Feature.GetTagSchemaAsync(adapterCallContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);
                }

                if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, _jsonOptions, out var validationResults)) {
                    throw new System.ComponentModel.DataAnnotations.ValidationException(System.Text.Json.JsonSerializer.Serialize(validationResults));
                }

                return await adapter.Feature.CreateTagAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
            }
        }


        public async Task<TagDefinition> UpdateTag(string adapterId, UpdateTagRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartUpdateTagActivity(adapter.Adapter.Descriptor.Id, request.Tag)) {
                System.Text.Json.JsonElement schema;
                using (Telemetry.ActivitySource.StartGetTagSchemaActivity(adapter.Adapter.Descriptor.Id)) {
                    schema = await adapter.Feature.GetTagSchemaAsync(adapterCallContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);
                }

                if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, _jsonOptions, out var validationResults)) {
                    throw new System.ComponentModel.DataAnnotations.ValidationException(System.Text.Json.JsonSerializer.Serialize(validationResults));
                }

                return await adapter.Feature.UpdateTagAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
            }
        }


        public async Task<bool> DeleteTag(string adapterId, DeleteTagRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartDeleteTagActivity(adapter.Adapter.Descriptor.Id, request.Tag)) {
                return await adapter.Feature.DeleteTagAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
            }
        }

    }
}
