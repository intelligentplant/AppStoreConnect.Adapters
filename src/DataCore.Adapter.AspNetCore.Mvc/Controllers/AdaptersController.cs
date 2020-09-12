using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for requesting information about the hosted adapters.
    /// </summary>
    [ApiController]
    [Area("data-core")]
    [Route("api/[area]/v1.0/adapters")]
    public class AdaptersController : ControllerBase {

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AdaptersController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public AdaptersController(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Finds adapters matching the specified search filters.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID filter.
        /// </param>
        /// <param name="name">
        ///   The adapter name filter.
        /// </param>
        /// <param name="description">
        ///   The adapter description filter.
        /// </param>
        /// <param name="feature">
        ///   The adapter feature filter. Unlike the ID, name and description filters, the feature 
        ///   filter must exactly match the standard or extension feature name.
        /// </param>
        /// <param name="pageSize">
        ///   The page size for the query.
        /// </param>
        /// <param name="page">
        ///   The page number for the query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="AdapterDescriptor"/> 
        ///   objects.
        /// </returns>
        [HttpGet]
        [Route("")]
        [ProducesResponseType(typeof(IEnumerable<AdapterDescriptor>), 200)]
        public Task<IActionResult> FindAdapters(string? id = null, string? name = null, string? description = null, [FromQuery] string[]? feature = null, int pageSize = 10, int page = 1, CancellationToken cancellationToken = default) {
            var request = new FindAdaptersRequest() { 
                Id = id,
                Name = name,
                Description = description,
                Features = feature ?? Array.Empty<string>(),
                PageSize = pageSize,
                Page = page
            };

            return FindAdapters(request, cancellationToken);
        }


        /// <summary>
        /// Finds adapters matching the specified search filter.
        /// </summary>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="AdapterDescriptor"/> 
        ///   objects.
        /// </returns>
        [HttpPost]
        [Route("")]
        [ProducesResponseType(typeof(IEnumerable<AdapterDescriptor>), 200)]
        public async Task<IActionResult> FindAdapters(FindAdaptersRequest request, CancellationToken cancellationToken = default) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var adapters = await _adapterAccessor.FindAdapters(callContext, request, true, cancellationToken).ConfigureAwait(false);
            var result = adapters.Select(x => AdapterDescriptor.FromExisting(x.Descriptor)).ToArray();
            return Ok(result); // 200
        }


        /// <summary>
        /// Gets information about the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the <see cref="AdapterDescriptorExtended"/> for the 
        ///   requested adapter.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(AdapterDescriptorExtended), 200)]
        public async Task<IActionResult> GetAdapterById(string adapterId, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var adapter = await _adapterAccessor.GetAdapter(callContext, adapterId, true, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            return Ok(adapter.CreateExtendedAdapterDescriptor()); // 200
        }


        /// <summary>
        /// Performs a health check on the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the <see cref="HealthCheckResult"/> for the requested 
        ///   adapter.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/health-status")]
        [ProducesResponseType(typeof(HealthCheckResult), 200)]
        public async Task<IActionResult> CheckAdapterHealth(string adapterId, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IHealthCheck>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IHealthCheck))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            try {
                return Ok(await feature.CheckHealthAsync(callContext, cancellationToken).ConfigureAwait(false)); // 200
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
        }

    }

}
