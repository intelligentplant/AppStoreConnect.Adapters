using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting annotations on tag values.
    /// </summary>
    [ApiController]
    [Area("app-store-connect")]
    [Route("api/[area]/v2.0/tag-annotations")]
    // Legacy route for compatibility with v1 of the toolkit
    [Route("api/data-core/v1.0/tag-annotations")]
    [UseAdapterRequestValidation(false)]
    public class TagAnnotationsController: ControllerBase {

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagAnnotationsController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public TagAnnotationsController(IAdapterAccessor adapterAccessor) {
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
        [ProducesResponseType(typeof(IAsyncEnumerable<TagValueAnnotationQueryResult>), 200)]
        public async Task<IActionResult> ReadAnnotations(string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadTagValueAnnotations>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;

            return Util.StreamResults(
                feature.ReadAnnotations(callContext, request, cancellationToken)
            );
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
        ///   Successful responses contain the matching <see cref="TagValueAnnotationExtended"/> object.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/{tagId}/{annotationId}")]
        [ProducesResponseType(typeof(TagValueAnnotationExtended), 200)]
        [UseAdapterRequestValidation(true)]
        public async Task<IActionResult> ReadAnnotation(string adapterId, string tagId, string annotationId, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IReadTagValueAnnotations>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IReadTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;
            var request = new ReadAnnotationRequest() {
                Tag = tagId,
                AnnotationId = annotationId
            };

            try {
                var result = await feature.ReadAnnotation(callContext, request, cancellationToken).ConfigureAwait(false);
                return Ok(result); // 200
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
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
        [UseAdapterRequestValidation(true)]
        public async Task<IActionResult> CreateAnnotation(string adapterId, string tagId, TagValueAnnotation annotation, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteTagValueAnnotations>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IWriteTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;
            var request = new CreateAnnotationRequest() {
                Tag = tagId,
                Annotation = annotation
            };

            try {
                var result = await feature.CreateAnnotation(callContext, request, cancellationToken).ConfigureAwait(false);

                return Ok(result); // 200
            }
            catch (SecurityException) {
                return Forbid(); // 403

            }
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
        [UseAdapterRequestValidation(true)]
        public async Task<IActionResult> UpdateAnnotation(string adapterId, string tagId, string annotationId, TagValueAnnotation annotation, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteTagValueAnnotations>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IWriteTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var request = new UpdateAnnotationRequest() {
                Tag = tagId,
                AnnotationId = annotationId,
                Annotation = annotation
            };

            try {
                var result = await feature.UpdateAnnotation(callContext, request, cancellationToken).ConfigureAwait(false);

                return Ok(result); // 200
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
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
        [UseAdapterRequestValidation(true)]
        public async Task<IActionResult> DeleteAnnotation(string adapterId, string tagId, string annotationId, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IWriteTagValueAnnotations>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IWriteTagValueAnnotations))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var request = new DeleteAnnotationRequest() {
                Tag = tagId,
                AnnotationId = annotationId
            };

            try {
                var result = await feature.DeleteAnnotation(callContext, request, cancellationToken).ConfigureAwait(false);

                return Ok(result); // 200
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
        }

    }

}
