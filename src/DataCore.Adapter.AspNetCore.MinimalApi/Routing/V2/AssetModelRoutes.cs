using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class AssetModelRoutes : IRouteProvider {

        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/browse", BrowseNodesGetAsync)
                .Produces<IAsyncEnumerable<AssetModelNode>>()
                .ProducesDefaultErrors();

            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/browse", BrowseNodesPostAsync)
                .Produces<IAsyncEnumerable<AssetModelNode>>()
                .ProducesDefaultErrors();

            builder.MapGet($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/get-by-id", GetNodesGetAsync)
                .Produces<IAsyncEnumerable<AssetModelNode>>()
                .ProducesDefaultErrors();

            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/get-by-id", GetNodesPostAsync)
                .Produces<IAsyncEnumerable<AssetModelNode>>()
                .ProducesDefaultErrors();
            
            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/find", FindNodesAsync)
                .Produces<IAsyncEnumerable<AssetModelNode>>()
                .ProducesDefaultErrors();
        }


        private static async Task<IResult> BrowseNodesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string? start = null, 
            int page = 1, 
            int pageSize = 10,
            CancellationToken cancellationToken = default
        ) {
            return await BrowseNodesPostAsync(context, adapterAccessor, adapterId, new BrowseAssetModelNodesRequest() { 
                ParentId = start,
                Page = page, 
                PageSize = pageSize
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> BrowseNodesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            BrowseAssetModelNodesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IAssetModelBrowse>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.BrowseAssetModelNodes(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> GetNodesGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string[] id,
            CancellationToken cancellationToken = default
        ) {
            return await GetNodesPostAsync(context, adapterAccessor, adapterId, new GetAssetModelNodesRequest() { 
                Nodes = id
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> GetNodesPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            GetAssetModelNodesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IAssetModelBrowse>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.GetAssetModelNodes(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> FindNodesAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            FindAssetModelNodesRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IAssetModelSearch>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.FindAssetModelNodes(resolverResult.CallContext, request, cancellationToken));
        }

    }
}
