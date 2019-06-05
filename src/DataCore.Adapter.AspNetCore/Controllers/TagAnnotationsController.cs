using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting annotations on tag values.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/tag-annotations")]
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
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
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
        public async Task<IActionResult> ReadAnnotations(ApiVersion apiVersion, string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
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

            var reader = feature.ReadTagValueAnnotations(_callContext, request, cancellationToken);

            var result = new List<TagValueAnnotationQueryResult>();
            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var value) || value == null) {
                    continue;
                }

                result.Add(value);
            }

            return Ok(result); // 200
        }

    }

}
