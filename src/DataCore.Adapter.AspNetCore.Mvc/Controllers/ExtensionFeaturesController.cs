using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// Controller for invoking extension adapter features.
    /// </summary>
    [ApiController]
    [Area("data-core")]
    [Route("api/[area]/v1.0/extensions")]
    public class ExtensionFeaturesController : ControllerBase {

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="ExtensionFeaturesController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public ExtensionFeaturesController(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Gets the extension feature URIs for an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of adapter extension feature URIs.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}")]
        public async Task<IActionResult> GetAvailableExtensions(string adapterId, CancellationToken cancellationToken = default) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var adapter = await _adapterAccessor.GetAdapter(callContext, adapterId, true, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }

            var descriptor = adapter.CreateExtendedAdapterDescriptor();
            return Ok(descriptor.Extensions); // 200
        }


        /// <summary>
        /// Gets the descriptor for the specified extension feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="id">
        ///   The extension feature URI.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a <see cref="FeatureDescriptor"/> object.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/descriptor")]
        public async Task<IActionResult> GetDescriptor(
            string adapterId, 
            [FromQuery] Uri id, 
            CancellationToken cancellationToken = default
        ) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            if (id == null || !id.IsAbsoluteUri) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, id)); // 400
            }

            id = UriExtensions.EnsurePathHasTrailingSlash(id);

            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAdapterExtensionFeature>(callContext, adapterId, id, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, id)); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            try {
                var descriptor = await resolvedFeature.Feature.GetDescriptor(callContext, id, cancellationToken).ConfigureAwait(false);
                return Ok(descriptor); // 200
            }
            catch (ArgumentException e) {
                return BadRequest(e.Message); // 400
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
        }


        /// <summary>
        /// Gets the available operations on an extension feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="id">
        ///   The extension feature URI.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a collection of <see cref="ExtensionFeatureOperationDescriptor"/> 
        ///   objects.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/operations")]
        public async Task<IActionResult> GetAvailableOperations(
            string adapterId, 
            [FromQuery] Uri id, 
            CancellationToken cancellationToken = default
        ) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            if (id == null || !id.IsAbsoluteUri) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, id)); // 400
            }

            id = UriExtensions.EnsurePathHasTrailingSlash(id);

            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAdapterExtensionFeature>(callContext, adapterId, id, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, id)); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            try {
                var ops = await resolvedFeature.Feature.GetOperations(callContext, id, cancellationToken).ConfigureAwait(false);
                // Note that we filter out any non-invocation operations here!
                return Ok(ops?.Where(x => x != null && x.OperationType == ExtensionFeatureOperationType.Invoke).ToArray() ?? Array.Empty<ExtensionFeatureOperationDescriptor>()); // 200
            }
            catch (ArgumentException e) {
                return BadRequest(e.Message); // 400
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
        }


        /// <summary>
        /// Invokes an extension adapter feature.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="id">
        ///   The URI of the operation to invoke.
        /// </param>
        /// <param name="argument">
        ///   The argument for the operation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a string describing the result of the operation. 
        ///   Callers are responsible for correctly parsing or inferring meaning from this result.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/operations/invoke")]
        public async Task<IActionResult> InvokeExtension(
            string adapterId, 
            [FromQuery] Uri id, 
            [FromForm]
            string argument = null, 
            CancellationToken cancellationToken = default
        ) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            if (id == null || !id.IsAbsoluteUri) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, id)); // 400
            }

            id = UriExtensions.EnsurePathHasTrailingSlash(id);
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(id, out var featureUri, out var error)) {
                return BadRequest(error); // 400
            }

            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAdapterExtensionFeature>(callContext, adapterId, featureUri, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, featureUri)); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }

            try {
                var result = new ContentResult() {
                    Content = await resolvedFeature.Feature.Invoke(callContext, id, argument, cancellationToken).ConfigureAwait(false),
                    ContentType = "application/json",
                    StatusCode = 200
                };

                return result; // 200
            }
            catch (ArgumentException e) {
                return BadRequest(e.Message); // 400
            }
            catch (SecurityException) {
                return Forbid(); // 403
            }
        }

    }

}
