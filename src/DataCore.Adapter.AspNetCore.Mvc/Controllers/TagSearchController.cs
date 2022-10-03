using System;
using System.Collections.Generic;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Tags;
using DataCore.Adapter.Tags;

using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for performing tag searches.
    /// </summary>
    [ApiController]
    [Area("app-store-connect")]
    [Route("api/[area]/v2.0/tags")]
    // Legacy route for compatibility with v1 of the toolkit
    [Route("api/data-core/v1.0/tags")] 
    public class TagSearchController : ControllerBase {

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagSearchController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public TagSearchController(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Gets tag property definitions from an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="AdapterProperty"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/properties")]
        [ProducesResponseType(typeof(IAsyncEnumerable<AdapterProperty>), 200)]
        public async Task<IActionResult> GetTagProperties(string adapterId, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ITagInfo>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ITagInfo))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;
            var activity = Telemetry.ActivitySource.StartGetTagPropertiesActivity(resolvedFeature.Adapter.Descriptor.Id, request);

            return Util.StreamResults(
                feature.GetTagProperties(callContext, request, cancellationToken),
                activity
            );
        }


        /// <summary>
        /// Gets tag property definitions from an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="pageSize">
        ///   The page size.
        /// </param>
        /// <param name="page">
        ///   The results page to return.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="TagDefinition"/> objects.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/properties")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTagProperties(string adapterId, int pageSize = 10, int page = 1, CancellationToken cancellationToken = default) {
            return await GetTagProperties(adapterId, new GetTagPropertiesRequest() {
                PageSize = pageSize,
                Page = page
            }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Performs a tag search on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="TagDefinition"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/find")]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> FindTags(string adapterId, FindTagsRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ITagSearch>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ITagSearch))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            var feature = resolvedFeature.Feature;
            var activity = Telemetry.ActivitySource.StartFindTagsActivity(resolvedFeature.Adapter.Descriptor.Id, request);

            return Util.StreamResults(
                feature.FindTags(callContext, request, cancellationToken),
                activity
            );
        }


        /// <summary>
        /// Performs a tag search on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The tag name filter.
        /// </param>
        /// <param name="description">
        ///   The tag description filter.
        /// </param>
        /// <param name="units">
        ///   The tag units filter.
        /// </param>
        /// <param name="pageSize">
        ///   The page size.
        /// </param>
        /// <param name="page">
        ///   The results page to return.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="TagDefinition"/> objects.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/find")]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> FindTags(string adapterId, string? name = null, string? description = null, string? units = null, int pageSize = 10, int page = 1, CancellationToken cancellationToken = default) {
            return await FindTags(adapterId, new FindTagsRequest() {
                Name = name,
                Description = description,
                Units = units,
                PageSize = pageSize,
                Page = page
            }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets tags by ID.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of matching <see cref="TagDefinition"/> 
        ///   objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/get-by-id")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTags(string adapterId, GetTagsRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ITagInfo>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ITagInfo))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;
            var activity = Telemetry.ActivitySource.StartGetTagsActivity(resolvedFeature.Adapter.Descriptor.Id, request);

            return Util.StreamResults(
                feature.GetTags(callContext, request, cancellationToken),
                activity
            );
        }


        /// <summary>
        /// Gets tags by ID.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tag">
        ///   The IDs of the tags to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of matching <see cref="TagDefinition"/> 
        ///   objects.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/get-by-id")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTags(string adapterId, [FromQuery] string[] tag, CancellationToken cancellationToken) {
            return await GetTags(adapterId, new GetTagsRequest() {
                Tags = tag
            }, cancellationToken).ConfigureAwait(false);
        }

    }

}
