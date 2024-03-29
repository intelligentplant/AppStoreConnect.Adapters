﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
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
    [UseAdapterRequestValidation(false)]
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
        [Route("{adapterId:maxlength(200)}/properties")]
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

            return Util.StreamResults(
                feature.GetTagProperties(callContext, request, cancellationToken)
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
        [Route("{adapterId:maxlength(200)}/properties")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTagProperties(string adapterId, int pageSize = 10, int page = 1, CancellationToken cancellationToken = default) {
            var request = new GetTagPropertiesRequest() {
                PageSize = pageSize,
                Page = page
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return await GetTagProperties(adapterId, request, cancellationToken).ConfigureAwait(false);
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
        [Route("{adapterId:maxlength(200)}/find")]
        [Route("{adapterId:maxlength(200)}")]
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

            return Util.StreamResults(
                feature.FindTags(callContext, request, cancellationToken)
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
        [Route("{adapterId:maxlength(200)}/find")]
        [Route("{adapterId:maxlength(200)}")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> FindTags(string adapterId, string? name = null, string? description = null, string? units = null, int pageSize = 10, int page = 1, CancellationToken cancellationToken = default) {
            var request = new FindTagsRequest() {
                Name = name,
                Description = description,
                Units = units,
                PageSize = pageSize,
                Page = page
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return await FindTags(adapterId, request, cancellationToken).ConfigureAwait(false);
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
        [Route("{adapterId:maxlength(200)}/get-by-id")]
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

            return Util.StreamResults(
                feature.GetTags(callContext, request, cancellationToken)
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
        [Route("{adapterId:maxlength(200)}/get-by-id")]
        [ProducesResponseType(typeof(IAsyncEnumerable<TagDefinition>), 200)]
        public async Task<IActionResult> GetTags(string adapterId, [FromQuery] string[] tag, CancellationToken cancellationToken) {
            var request = new GetTagsRequest() {
                Tags = tag
            };
            Validator.ValidateObject(request, new ValidationContext(request), true);
            return await GetTags(adapterId, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the configuration schema to use when creating or updating tags.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to retrieve the tag configuration schema for.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the JSON schema for the adapter's tag configuration 
        ///   model.
        /// </returns>
        [HttpGet]
        [Route("{adapterId:maxlength(200)}/schema")]
        [ProducesResponseType(typeof(System.Text.Json.JsonElement), 200)]
        public async Task<IActionResult> GetTagSchema(string adapterId, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ITagConfiguration>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ITagConfiguration))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var schema = await feature.GetTagSchemaAsync(callContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);
            return Ok(schema);
        }


        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The tag creation request.
        /// </param>
        /// <param name="jsonOptions">
        ///   The configured JSON options.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the created <see cref="TagDefinition"/> object.
        /// </returns>
        [HttpPost]
        [Route("{adapterId:maxlength(200)}/create")]
        [ProducesResponseType(typeof(TagDefinition), 200)]
        public async Task<IActionResult> CreateTag(string adapterId, CreateTagRequest request, [FromServices] Microsoft.Extensions.Options.IOptions<JsonOptions> jsonOptions, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ITagConfiguration>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ITagConfiguration))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var schema = await feature.GetTagSchemaAsync(callContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);

            if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, jsonOptions.Value?.JsonSerializerOptions, out var validationResults)) {
                var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, 400, title: SharedResources.Error_InvalidRequestBody);
                problem.Extensions["errors"] = validationResults;
                return new ObjectResult(problem) { StatusCode = 400 }; // 400
            }

            return Ok(await feature.CreateTagAsync(callContext, request, cancellationToken).ConfigureAwait(false));
        }


        /// <summary>
        /// Updates an existing tag.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The tag update request.
        /// </param>
        /// <param name="jsonOptions">
        ///   The configured JSON options.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the updated <see cref="TagDefinition"/> object.
        /// </returns>
        [HttpPost]
        [Route("{adapterId:maxlength(200)}/update")]
        [ProducesResponseType(typeof(TagDefinition), 200)]
        public async Task<IActionResult> UpdateTag(string adapterId, UpdateTagRequest request, [FromServices] Microsoft.Extensions.Options.IOptions<JsonOptions> jsonOptions, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ITagConfiguration>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ITagConfiguration))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            var schema = await feature.GetTagSchemaAsync(callContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);

            if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, jsonOptions.Value?.JsonSerializerOptions, out var validationResults)) {
                var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, 400, title: SharedResources.Error_InvalidRequestBody);
                problem.Extensions["errors"] = validationResults;
                return new ObjectResult(problem) { StatusCode = 400 }; // 400
            }

            return Ok(await feature.UpdateTagAsync(callContext, request, cancellationToken).ConfigureAwait(false));
        }


        /// <summary>
        /// Deletes an existing tag.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The tag delete request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain a Boolean value indicating if the tag was deleted.
        /// </returns>
        [HttpPost]
        [Route("{adapterId:maxlength(200)}/delete")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteTag(string adapterId, DeleteTagRequest request, CancellationToken cancellationToken) {
            var callContext = new HttpAdapterCallContext(HttpContext);
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<ITagConfiguration>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(ITagConfiguration))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            return Ok(await feature.DeleteTagAsync(callContext, request, cancellationToken).ConfigureAwait(false));
        }

    }

}
