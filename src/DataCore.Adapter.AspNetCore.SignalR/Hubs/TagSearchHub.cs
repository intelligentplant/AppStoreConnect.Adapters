using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ITagSearch>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.FindTags(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.GetTags(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ITagInfo>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.GetTagProperties(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Gets the schema for creating or updating tags on the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   A <see cref="System.Text.Json.JsonElement"/> describing the tag creation JSON schema.
        /// </returns>
        public async Task<System.Text.Json.JsonElement> GetTagSchema(string adapterId, GetTagSchemaRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            return await adapter.Feature.GetTagSchemaAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a tag on the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The created tag.
        /// </returns>
        public async Task<TagDefinition> CreateTag(string adapterId, CreateTagRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var schema = await adapter.Feature.GetTagSchemaAsync(adapterCallContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);

            if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, _jsonOptions, out var validationResults)) {
                throw new System.ComponentModel.DataAnnotations.ValidationException(System.Text.Json.JsonSerializer.Serialize(validationResults));
            }

            return await adapter.Feature.CreateTagAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates a tag on the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The updated tag.
        /// </returns>
        public async Task<TagDefinition> UpdateTag(string adapterId, UpdateTagRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            var schema = await adapter.Feature.GetTagSchemaAsync(adapterCallContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);

            if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, _jsonOptions, out var validationResults)) {
                throw new System.ComponentModel.DataAnnotations.ValidationException(System.Text.Json.JsonSerializer.Serialize(validationResults));
            }

            return await adapter.Feature.UpdateTagAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a tag on the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the tag was deleted, or <see langword="false"/> otherwise.
        /// </returns>
        public async Task<bool> DeleteTag(string adapterId, DeleteTagRequest request) {
            var cancellationToken = Context.ConnectionAborted;

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<ITagConfiguration>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            return await adapter.Feature.DeleteTagAsync(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }

    }
}
