using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Feature for creating, updating and deleting tag definitions.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Tags.TagConfiguration,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_TagConfiguration),
        Description = nameof(AbstractionsResources.Description_TagConfiguration)
    )]
    public interface ITagConfiguration : IAdapterFeature {

        /// <summary>
        /// Gets the JSON schema that describes the tag configuration model to use when creating 
        /// or updating tags on this adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The JSON schema that describes the tag configuration model used by the adapter.
        /// </returns>
        Task<JsonElement> GetTagSchemaAsync(
            IAdapterCallContext context, 
            GetTagSchemaRequest request, 
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="TagDefinition"/> describing the new tag.
        /// </returns>
        Task<TagDefinition> CreateTagAsync(
            IAdapterCallContext context, 
            CreateTagRequest request, 
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Updates an existing tag.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="TagDefinition"/> describing the updated tag.
        /// </returns>
        Task<TagDefinition> UpdateTagAsync(IAdapterCallContext context, UpdateTagRequest request, CancellationToken cancellationToken);


        /// <summary>
        /// Deletes a tag.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the tag exists and was deleted, or <see langword="false"/> 
        ///   if the tag was not deleted.
        /// </returns>
        Task<bool> DeleteTagAsync(IAdapterCallContext context, DeleteTagRequest request, CancellationToken cancellationToken);

    }

}
