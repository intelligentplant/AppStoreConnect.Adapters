using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for performing tag searches.
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Area("data-core")]
    [Route("api/[area]/v{version:apiVersion}/tags")]
    public class TagSearchController: ControllerBase {

        /// <summary>
        /// The adapter API authorization service to use.
        /// </summary>
        private readonly AdapterApiAuthorizationService _authorizationService;

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the calling user.
        /// </summary>
        private readonly IAdapterCallContext _callContext;

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagSearchController"/> object.
        /// </summary>
        /// <param name="authorizationService">
        ///   The API authorization service to use.
        /// </param>
        /// <param name="callContext">
        ///   The <see cref="IAdapterCallContext"/> for the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public TagSearchController(AdapterApiAuthorizationService authorizationService, IAdapterCallContext callContext, IAdapterAccessor adapterAccessor) {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _callContext = callContext ?? throw new ArgumentNullException(nameof(callContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Performs a tag search on an adapter.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
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
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> FindTags(ApiVersion apiVersion, string adapterId, FindTagsRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<ITagSearch>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(ITagSearch))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<ITagSearch>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var tags = await feature.FindTags(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(tags); // 200
        }


        /// <summary>
        /// Performs a tag search on an adapter.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
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
        /// <returns>
        ///   Successful responses contain a collection of <see cref="TagDefinition"/> objects.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/find")]
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> FindTags(ApiVersion apiVersion, CancellationToken cancellationToken, string adapterId, string name = null, string description = null, string units = null, int pageSize = 10, int page = 1) {
            return await FindTags(apiVersion, adapterId, new FindTagsRequest() {
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
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
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
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTags(ApiVersion apiVersion, string adapterId, GetTagsRequest request, CancellationToken cancellationToken) {
            var adapter = await _adapterAccessor.GetAdapter(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var feature = adapter.Features.Get<ITagSearch>();
            if (feature == null) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(ITagSearch))); // 400
            }

            var authResponse = await _authorizationService.AuthorizeAsync<ITagSearch>(
                User,
                adapter
            ).ConfigureAwait(false);

            if (!authResponse.Succeeded) {
                return Unauthorized(); // 401
            }

            var tags = await feature.GetTags(_callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(tags); // 200
        }


        /// <summary>
        /// Gets tags by ID.
        /// </summary>
        /// <param name="apiVersion">
        ///   The API version.
        /// </param>
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
        [ProducesResponseType(typeof(IEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTags(ApiVersion apiVersion, string adapterId, [FromQuery] string[] tag, CancellationToken cancellationToken) {
            return await GetTags(apiVersion, adapterId, new GetTagsRequest() {
                Tags = tag
            }, cancellationToken).ConfigureAwait(false);
        }

    }

}
