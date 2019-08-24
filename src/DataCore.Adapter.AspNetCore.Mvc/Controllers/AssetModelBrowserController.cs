using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AssetModel.Features;
using DataCore.Adapter.AssetModel.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// API controller for browsing an adapter's asset model hierarchy.
    /// </summary>
    [ApiController]
    [Area("data-core")]
    [Route("api/[area]/v1.0/asset-model")]
    public class AssetModelBrowserController : ControllerBase {

        /// <summary>
        /// The Data Core context for the caller.
        /// </summary>
        private readonly IAdapterCallContext _callContext;

        /// <summary>
        /// The service for accessing the running adapters.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// Maximum number of asset model nodes that will be returned in a single query.
        /// </summary>
        public const int MaxNodesPerQuery = 1000;


        /// <summary>
        /// Creates a new <see cref="AssetModelBrowserController"/> object.
        /// </summary>
        /// <param name="callContext">
        ///   The <see cref="IAdapterCallContext"/> for the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   Service for accessing the available adapters.
        /// </param>
        public AssetModelBrowserController(IAdapterCallContext callContext, IAdapterAccessor adapterAccessor) {
            _callContext = callContext ?? throw new ArgumentNullException(nameof(callContext));
            _adapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
        }


        /// <summary>
        /// Browses the asset model hierarchy. Up to <see cref="MaxNodesPerQuery"/> will be 
        /// returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <param name="start">
        ///   The optional starting node ID. If no value is specified, browsing will start at the 
        ///   top-level nodes in the hierarchy.
        /// </param>
        /// <param name="depth">
        ///   The maximum depth of node to retrieve.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpGet]
        [Route("{adapterId}/browse")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public async Task<IActionResult> BrowseNodes(string adapterId, CancellationToken cancellationToken, string start = null, int depth = -1) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAssetModelBrowser>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IAssetModelBrowser))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var resultChannel = feature.BrowseAssetModelNodes(_callContext, new BrowseAssetModelNodesRequest() {
                ParentId = string.IsNullOrWhiteSpace(start)
                    ? null
                    : start,
                Depth = depth
            }, cancellationToken);

            var result = new List<AssetModelNode>(MaxNodesPerQuery);

            var itemsRead = 0;
            while (await resultChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (resultChannel.TryRead(out var item) && item != null) {
                    ++itemsRead;
                    result.Add(item);

                    if (itemsRead >= MaxNodesPerQuery) {
                        Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxNodesPerQuery));
                        break;
                    }
                }
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Browses the asset model hierarchy. Up to <see cref="MaxNodesPerQuery"/> will be 
        /// returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The browse request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/browse")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public async Task<IActionResult> BrowseNodesPost(string adapterId, BrowseAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAssetModelBrowser>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IAssetModelBrowser))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var resultChannel = feature.BrowseAssetModelNodes(_callContext, request, cancellationToken);

            var result = new List<AssetModelNode>(MaxNodesPerQuery);

            var itemsRead = 0;
            while (await resultChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (resultChannel.TryRead(out var item) && item != null) {
                    ++itemsRead;
                    result.Add(item);

                    if (itemsRead >= MaxNodesPerQuery) {
                        Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxNodesPerQuery));
                        break;
                    }
                }
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Gets a collection of nodes by ID. Up to <see cref="MaxNodesPerQuery"/> will be returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The request object describing the nodes to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/get-by-id")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public async Task<IActionResult> GetNodes(string adapterId, GetAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAssetModelBrowser>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IAssetModelBrowser))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var resultChannel = feature.GetAssetModelNodes(_callContext, request, cancellationToken);

            var result = new List<AssetModelNode>(MaxNodesPerQuery);

            var itemsRead = 0;
            while (await resultChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (resultChannel.TryRead(out var item) && item != null) {
                    ++itemsRead;
                    result.Add(item);

                    if (itemsRead >= MaxNodesPerQuery) {
                        Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxNodesPerQuery));
                        break;
                    }
                }
            }

            return Ok(result); // 200
        }


        /// <summary>
        /// Finds nodes matching the specified search filter. Up to <see cref="MaxNodesPerQuery"/> 
        /// will be returned.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to query.
        /// </param>
        /// <param name="request">
        ///   The request object describing the nodes to retrieve.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Successful responses contain the matching <see cref="AssetModelNode"/> objects.
        /// </returns>
        [HttpPost]
        [Route("{adapterId}/find")]
        [ProducesResponseType(typeof(IEnumerable<AssetModelNode>), 200)]
        public async Task<IActionResult> FindNodes(string adapterId, FindAssetModelNodesRequest request, CancellationToken cancellationToken) {
            var resolvedFeature = await _adapterAccessor.GetAdapterAndFeature<IAssetModelBrowser>(_callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return BadRequest(string.Format(Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return BadRequest(string.Format(Resources.Error_UnsupportedInterface, nameof(IAssetModelBrowser))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Unauthorized(); // 401
            }
            var feature = resolvedFeature.Feature;

            var resultChannel = feature.FindAssetModelNodes(_callContext, request, cancellationToken);

            var result = new List<AssetModelNode>(MaxNodesPerQuery);

            var itemsRead = 0;
            while (await resultChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (resultChannel.TryRead(out var item) && item != null) {
                    ++itemsRead;
                    result.Add(item);

                    if (itemsRead >= MaxNodesPerQuery) {
                        Util.AddIncompleteResponseHeader(Response, string.Format(Resources.Warning_MaxResponseItemsReached, MaxNodesPerQuery));
                        break;
                    }
                }
            }

            return Ok(result); // 200
        }

    }
}
