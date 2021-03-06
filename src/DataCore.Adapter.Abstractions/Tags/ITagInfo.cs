﻿using System.Collections.Generic;
using System.Threading;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Feature for requesting information about tags.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Tags.TagInfo,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_TagInfo),
        Description = nameof(AbstractionsResources.Description_TagInfo)
    )]
    public interface ITagInfo : IAdapterFeature {

        /// <summary>
        /// Gets the definitions of the bespoke properties that can are included in <see cref="TagDefinition"/> 
        /// objects returned by the adapter.
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
        ///   The available tag properties.
        /// </returns>
        IAsyncEnumerable<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request, 
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Gets tags by ID or name.
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
        ///   The matching tag definitions.
        /// </returns>
        IAsyncEnumerable<TagDefinition> GetTags(
            IAdapterCallContext context, 
            GetTagsRequest request, 
            CancellationToken cancellationToken
        );

    }
}
