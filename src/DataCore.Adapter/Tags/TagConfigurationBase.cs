using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Json.Schema;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Base implementation of <see cref="ITagConfiguration"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The type that describes the model to use when creating or updating tags.
    /// </typeparam>
    /// <remarks>
    ///   When constructed using <see cref="TagConfigurationBase(IBackgroundTaskService?, JsonSerializerOptions?)"/>, 
    ///   a JSON schema will be automatically generated for <typeparamref name="T"/>. See 
    ///   <see cref="JsonSchemaUtility.CreateJsonSchema{T}"/> for details about how the schema is
    ///   generated.
    /// </remarks>
    /// <seealso cref="JsonSchemaUtility.CreateJsonSchema{T}"/>
    public abstract class TagConfigurationBase<T> : ITagConfiguration where T : class {

        /// <summary>
        /// JSON seralizer options.
        /// </summary>
        private readonly JsonSerializerOptions? _jsonSerializerOptions;

        /// <summary>
        /// The JSON schema for the tag configuration model.
        /// </summary>
        private readonly JsonElement _schema;

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }


        /// <summary>
        /// Creates a new <see cref="TagConfigurationBase{T}"/> using automatic schema generation.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use.
        /// </param>
        /// <param name="jsonSerializerOptions">
        ///   The JSON serializer options to use.
        /// </param>
        /// <remarks>
        ///   See <see cref="JsonSchemaUtility.CreateJsonSchema{T}"/> for details about how the 
        ///   schema is generated.
        /// </remarks>
        /// <seealso cref="JsonSchemaUtility.CreateJsonSchema{T}"/>
        protected TagConfigurationBase(IBackgroundTaskService? backgroundTaskService = null, JsonSerializerOptions? jsonSerializerOptions = null)
            : this(JsonSchemaUtility.CreateJsonSchema<T>(jsonSerializerOptions), backgroundTaskService, jsonSerializerOptions) { }


        /// <summary>
        /// Creates a new <see cref="TagConfigurationBase{T}"/> using the provided schema to 
        /// describe <typeparamref name="T"/>.
        /// </summary>
        /// <param name="schema">
        ///   The JSON schema to use.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use.
        /// </param>
        /// <param name="jsonSerializerOptions">
        ///   The JSON serializer options to use.
        /// </param>
        /// <remarks>
        ///   See <see cref="JsonSchemaUtility.CreateJsonSchema{T}"/> for details about how the 
        ///   schema is generated.
        /// </remarks>
        protected TagConfigurationBase(JsonElement schema, IBackgroundTaskService? backgroundTaskService = null , JsonSerializerOptions? jsonSerializerOptions = null) {
            _jsonSerializerOptions = jsonSerializerOptions;
            _schema = schema;
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
        }


        /// <inheritdoc/>
        public Task<JsonElement> GetTagSchemaAsync(IAdapterCallContext context, GetTagSchemaRequest request, CancellationToken cancellationToken) {
            ValidationExtensions.ValidateObject(context);
            ValidationExtensions.ValidateObject(request);

            return Task.FromResult(_schema);
        }


        /// <inheritdoc/>
        public async Task<TagDefinition> CreateTagAsync(IAdapterCallContext context, CreateTagRequest request, CancellationToken cancellationToken) {
            ValidationExtensions.ValidateObject(context);
            ValidationExtensions.ValidateObject(request);

            // Validation of the request body against the schema needs to be performed externally.
            // We don't do it here to allow it to be performed at the edges of the system e.g. in
            // an API controller.

            return await CreateTagAsync(
                context, 
                request.Body.Deserialize<T>(_jsonSerializerOptions) ?? throw new ArgumentException(Resources.Error_InvalidTagConfiguration, nameof(request)), 
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<TagDefinition> UpdateTagAsync(IAdapterCallContext context, UpdateTagRequest request, CancellationToken cancellationToken) {
            ValidationExtensions.ValidateObject(context);
            ValidationExtensions.ValidateObject(request);

            // Validation of the request body against the schema needs to be performed externally.
            // We don't do it here to allow it to be performed at the edges of the system e.g. in
            // an API controller.

            return await UpdateTagAsync(
                context,
                request.Tag,
                request.Body.Deserialize<T>(_jsonSerializerOptions) ?? throw new ArgumentException(Resources.Error_InvalidTagConfiguration, nameof(request)),
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<bool> DeleteTagAsync(IAdapterCallContext context, DeleteTagRequest request, CancellationToken cancellationToken) {
            ValidationExtensions.ValidateObject(context);
            ValidationExtensions.ValidateObject(request);

            return await DeleteTagAsync(context, request.Tag, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="tagConfiguration">
        ///   The tag configuration.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="TagDefinition"/> describing the new tag.
        /// </returns>
        protected abstract Task<TagDefinition> CreateTagAsync(IAdapterCallContext context, T tagConfiguration, CancellationToken cancellationToken);


        /// <summary>
        /// Updates an existing tag.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="tag">
        ///   The ID or name of the tag to update.
        /// </param>
        /// <param name="tagConfiguration">
        ///   The tag configuration.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="TagDefinition"/> describing the updated tag.
        /// </returns>
        protected abstract Task<TagDefinition> UpdateTagAsync(IAdapterCallContext context, string tag, T tagConfiguration, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes a tag.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="tag">
        ///   The ID or name of the tag to delete.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the tag exists and was deleted, or <see langword="false"/> 
        ///   if the tag was not deleted.
        /// </returns>
        protected abstract Task<bool> DeleteTagAsync(IAdapterCallContext context, string tag, CancellationToken cancellationToken);

    }
}
