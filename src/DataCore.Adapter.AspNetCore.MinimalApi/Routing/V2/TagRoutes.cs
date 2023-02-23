﻿using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Tags;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class TagRoutes : IRouteProvider {
        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet("/{adapterId}", FindTagsGetAsync);
            builder.MapPost("/{adapterId}", FindTagsPostAsync);
            builder.MapGet("/{adapterId}/find", FindTagsGetAsync);
            builder.MapPost("/{adapterId}/find", FindTagsPostAsync);
            builder.MapGet("/{adapterId}/properties", GetTagPropertiesGetAsync);
            builder.MapPost("/{adapterId}/properties", GetTagPropertiesPostAsync);
            builder.MapGet("/{adapterId}/get-by-id", GetTagsGetAsync);
            builder.MapPost("/{adapterId}/get-by-id", GetTagsPostAsync);
            builder.MapGet("/{adapterId}/schema", GetTagSchemaAsync);
            builder.MapPost("/{adapterId}/create", CreateTagAsync);
            builder.MapPost("/{adapterId}/update", UpdateTagAsync);
            builder.MapPost("/{adapterId}/delete", DeleteTagAsync);
        }


        private static async Task<IResult> FindTagsGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor, 
            string adapterId, 
            string? name = null, 
            string? description = null, 
            string? units = null, 
            int pageSize = 10, 
            int page = 1, 
            CancellationToken cancellationToken = default
        ) {
            return await FindTagsPostAsync(context, adapterAccessor, adapterId, new FindTagsRequest() {
                Name = name,
                Description = description,
                Units = units,
                PageSize = pageSize,
                Page = page
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> FindTagsPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            FindTagsRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ITagSearch>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.FindTags(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> GetTagsGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string[] tag,
            CancellationToken cancellationToken = default
        ) {
            return await GetTagsPostAsync(context, adapterAccessor, adapterId, new GetTagsRequest() { 
                Tags = tag,
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> GetTagsPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            GetTagsRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ITagSearch>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.GetTags(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> GetTagPropertiesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            int pageSize = 10,
            int page = 1,
            CancellationToken cancellationToken = default
        ) {
            return await GetTagPropertiesPostAsync(context, adapterAccessor, adapterId, new GetTagPropertiesRequest() {
                PageSize = pageSize,
                Page = page
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> GetTagPropertiesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            GetTagPropertiesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ITagInfo>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.GetTagProperties(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> GetTagSchemaAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAsync<ITagConfiguration>(context, adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.GetTagSchemaAsync(resolverResult.CallContext, new GetTagSchemaRequest(), cancellationToken));
        }


        private static async Task<IResult> CreateTagAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            IOptions<JsonOptions> jsonOptions,
            string adapterId,
            CreateTagRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ITagConfiguration>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            var schema = await resolverResult.Feature.GetTagSchemaAsync(resolverResult.CallContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);

            if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, jsonOptions.Value?.SerializerOptions, out var validationResults)) {
                return Results.Problem(statusCode: 400, detail: SharedResources.Error_InvalidRequestBody, extensions: new Dictionary<string, object?>() {
                    ["errors"] = validationResults
                });
            }

            return Results.Ok(resolverResult.Feature.CreateTagAsync(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> UpdateTagAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            IOptions<JsonOptions> jsonOptions,
            string adapterId,
            UpdateTagRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ITagConfiguration>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            var schema = await resolverResult.Feature.GetTagSchemaAsync(resolverResult.CallContext, new GetTagSchemaRequest(), cancellationToken).ConfigureAwait(false);

            if (!Json.Schema.JsonSchemaUtility.TryValidate(request.Body, schema, jsonOptions.Value?.SerializerOptions, out var validationResults)) {
                return Results.Problem(statusCode: 400, detail: SharedResources.Error_InvalidRequestBody, extensions: new Dictionary<string, object?>() {
                    ["errors"] = validationResults
                });
            }

            return Results.Ok(resolverResult.Feature.UpdateTagAsync(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> DeleteTagAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            DeleteTagRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ITagConfiguration>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(resolverResult.Feature.DeleteTagAsync(resolverResult.CallContext, request, cancellationToken));
        }

    }
}
