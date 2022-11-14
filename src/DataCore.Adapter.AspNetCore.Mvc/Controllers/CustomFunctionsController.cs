#pragma warning disable CS0618 // Type or member is obsolete
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;

using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// Controller for invoking custom functions on adapters.
    /// </summary>
    [ApiController]
    [Area("app-store-connect")]
    [Route("api/[area]/v2.0/custom-functions")]
    public class CustomFunctionsController : ControllerBase {

        /// <summary>
        /// For accessing the available adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="CustomFunctionsController"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public CustomFunctionsController(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Gets the available custom functions.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The request
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the descriptors for the available custom functions.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(IEnumerable<CustomFunctionDescriptor>), 200)]
        public async Task<IActionResult> GetFunctionsAsync(string adapterId, GetCustomFunctionsRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ICustomFunctions>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ICustomFunctions))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var functions = await feature.GetFunctionsAsync(callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(functions); // 200
        }


        /// <summary>
        /// Gets the available custom functions.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="id">
        ///   The function ID filter.
        /// </param>
        /// <param name="name">
        ///   The function name filter.
        /// </param>
        /// <param name="description">
        ///   The function description filter.
        /// </param>
        /// <param name="pageSize">
        ///   The page size for the results.
        /// </param>
        /// <param name="page">
        ///   The results page to return.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the descriptors for the available custom functions.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}")]
        [ProducesResponseType(typeof(IEnumerable<CustomFunctionDescriptor>), 200)]
        public async Task<IActionResult> GetFunctionsAsync(string adapterId, string? id = null, string? name = null, string? description = null, int pageSize = 10, int page = 1, CancellationToken cancellationToken = default) {
            return await GetFunctionsAsync(adapterId, new GetCustomFunctionsRequest() { 
                Id = id,
                Name = name,
                Description = description,
                PageSize = pageSize,
                Page = page
            }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the extended descriptor for the specified custom function.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the extended descriptor for the requested custom function.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/details")]
        [ProducesResponseType(typeof(CustomFunctionDescriptorExtended), 200)]
        public async Task<IActionResult> GetFunctionAsync(string adapterId, GetCustomFunctionRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ICustomFunctions>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ICustomFunctions))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var function = await feature.GetFunctionAsync(callContext, request, cancellationToken).ConfigureAwait(false);
            return Ok(function); // 200
        }


        /// <summary>
        /// Gets the extended descriptor for the specified custom function.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="id">
        ///   The custom function ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the extended descriptor for the requested custom function.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/details")]
        [ProducesResponseType(typeof(CustomFunctionDescriptorExtended), 200)]
        public async Task<IActionResult> GetFunctionAsync(string adapterId, [FromQuery] Uri id, CancellationToken cancellationToken) {
            return await GetFunctionAsync(adapterId, new GetCustomFunctionRequest() { 
                Id = id
            }, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Invokes a custom function on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <param name="jsonOptions">
        ///   The JSON options for the application.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the request.
        /// </param>
        /// <returns>
        ///   Successful responses contain the result of the custom function invocation.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/invoke")]
        [ProducesResponseType(typeof(CustomFunctionInvocationResponse), 200)]
        public async Task<IActionResult> InvokeFunctionAsync(
            string adapterId,
            CustomFunctionInvocationRequest request,
            [FromServices] Microsoft.Extensions.Options.IOptions<JsonOptions> jsonOptions,
            CancellationToken cancellationToken
        ) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ICustomFunctions>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ICustomFunctions))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var function = await feature.GetFunctionAsync(callContext, new GetCustomFunctionRequest() {
                Id = request.Id,
            }, cancellationToken).ConfigureAwait(false);

            if (function == null) {
                return BadRequest(string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_UnableToResolveCustomFunction, request.Id)); // 400
            }

            if (!request.TryValidateBody(function, jsonOptions.Value?.JsonSerializerOptions, out var validationResults)) {
                var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, 400, title: SharedResources.Error_InvalidRequestBody);
                problem.Extensions["errors"] = validationResults;
                return new ObjectResult(problem) { StatusCode = 400 }; // 400
            }

            return Ok(await feature.InvokeFunctionAsync(callContext, request, cancellationToken).ConfigureAwait(false)); // 200
        }

    }
}
#pragma warning restore CS0618 // Type or member is obsolete
