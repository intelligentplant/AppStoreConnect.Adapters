using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting annotations on tag values.
    /// </summary>
    [ApiController]
    [Area("data-core")]
    [Route("api/[area]/v1.0/tag-annotations")]
    public class TagAnnotationsController: ControllerBase {

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the calling user.
        /// </summary>
        private readonly IAdapterCallContext _callContext;

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The maximum number of annotations that can be returned per query.
        /// </summary>
        public const int MaxAnnotationsPerQuery = 1000;


        /// <summary>
        /// Creates a new <see cref="TagAnnotationsController"/> object.
        /// </summary>
        /// <param name="callContext">
        ///   The <see cref="IAdapterCallContext"/> for the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public TagAnnotationsController(IAdapterCallContext callContext, IAdapterAccessor adapterAccessor) {
            _callContext = callContext ?? throw new ArgumentNullException(nameof(callContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Reads tag value annotations from an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="TagValueAnnotationQueryResult"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(IEnumerable<TagValueAnnotationQueryResult>), 200)]
        public async Task<IActionResult> ReadAnnotations(string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadTagValueAnnotations>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var reader = feature.ReadAnnotations(_callContext, request, cancellationToken);

            var result = new List<TagValueAnnotationQueryResult>();
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                if (result.Count > MaxAnnotationsPerQuery) {
                    Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxAnnotationsPerQuery));
                    break;
                }

                result.Add(value);
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Gets an annotation by ID.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tagId">
        ///   The tag ID for the annotation.
        /// </param>
        /// <param name="annotationId">
        ///   The ID for the annotation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="TagValueAnnotation"/> object.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/{tagId}/{annotationId}")]
        [ProducesResponseType(typeof(TagValueAnnotation), 200)]
        public async Task<IActionResult> ReadAnnotation(string adapterId, string tagId, string annotationId, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadTagValueAnnotations>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IReadTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var result = await feature.ReadAnnotation(_callContext, new ReadAnnotationRequest() {
                TagId = tagId,
                AnnotationId = annotationId
            }, cancellationToken).ConfigureAwait(false);

            return Ok(result); // 200
        }


        /// <summary>
        /// Creates an annotation on a tag.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a <see cref="WriteTagValueAnnotationResult"/> 
        ///   describing the operation.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/{tagId}/create")]
        [ProducesResponseType(typeof(WriteTagValueAnnotationResult), 200)]
        public async Task<IActionResult> CreateAnnotation(string adapterId, string tagId, TagValueAnnotationBase annotation, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteTagValueAnnotations>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IWriteTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var result = await feature.CreateAnnotation(_callContext, new CreateAnnotationRequest() {
                TagId = tagId,
                Annotation = annotation
            }, cancellationToken).ConfigureAwait(false);

            return Ok(result); // 200
        }


        /// <summary>
        /// Deletes an annotation on a tag.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="annotationId">
        ///   The annotation ID.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a <see cref="WriteTagValueAnnotationResult"/> 
        ///   describing the operation.
        /// </returns>
        [HttpPut]
        [Route("{adapterId}/{tagId}/{annotationId}")]
        [ProducesResponseType(typeof(WriteTagValueAnnotationResult), 200)]
        public async Task<IActionResult> UpdateAnnotation(string adapterId, string tagId, string annotationId, TagValueAnnotationBase annotation, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteTagValueAnnotations>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IWriteTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var result = await feature.UpdateAnnotation(_callContext, new UpdateAnnotationRequest() {
                TagId = tagId,
                AnnotationId = annotationId,
                Annotation = annotation
            }, cancellationToken).ConfigureAwait(false);

            return Ok(result); // 200
        }


        /// <summary>
        /// Deletes an annotation on a tag.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="annotationId">
        ///   The annotation ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a <see cref="WriteTagValueAnnotationResult"/> 
        ///   describing the operation.
        /// </returns>
        [HttpDelete]
        [Route("{adapterId}/{tagId}/{annotationId}")]
        [ProducesResponseType(typeof(WriteTagValueAnnotationResult), 200)]
        public async Task<IActionResult> DeleteAnnotation(string adapterId, string tagId, string annotationId, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteTagValueAnnotations>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IWriteTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var result = await feature.DeleteAnnotation(_callContext, new DeleteAnnotationRequest() {
                TagId = tagId,
                AnnotationId = annotationId
            }, cancellationToken).ConfigureAwait(false);

            return Ok(result); // 200
        }

    }

}
