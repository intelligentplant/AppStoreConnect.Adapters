using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="IAdapterAccessor"/> implementation that resolves <see cref="IAdapter"/> objects that 
    /// are passed in as constructor parameters via dependency injection.
    /// </summary>
    public class AspNetCoreAdapterAccessor : AdapterAccessor {

        /// <summary>
        /// The available adapters.
        /// </summary>
        private readonly IEnumerable<IAdapter> _adapters;


        /// <summary>
        /// Creates a new <see cref="AspNetCoreAdapterAccessor"/> object.
        /// </summary>
        /// <param name="authorizationService">
        ///   The authorization service that will be used to control access to adapters.
        /// </param>
        /// <param name="adapters">
        ///   The ASP.NET Core hosted services.
        /// </param>
        public AspNetCoreAdapterAccessor(IAdapterAuthorizationService authorizationService, IEnumerable<IAdapter>? adapters) 
            : base(authorizationService) {
            _adapters = adapters ?? Array.Empty<IAdapter>();
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<IAdapter> FindAdapters(
            IAdapterCallContext context, 
            FindAdaptersRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapters = _adapters
                .Where(x => x.IsEnabled)
                .Where(x => MatchesFilter(x, request))
                .OrderBy(x => x.GetName(), StringComparer.OrdinalIgnoreCase);

            var skip = (request.Page - 1) * request.PageSize;
            var take = request.PageSize;

            foreach (var adapter in adapters) {
                if (!await IsAuthorized(adapter, context, cancellationToken).ConfigureAwait(false)) {
                    continue;
                }

                if (skip > 0) {
                    --skip;
                    continue;
                }

                yield return adapter;
                --take;
                if (take <= 0) {
                    break;
                }
            }
        }


        /// <inheritdoc/>
        protected override async Task<IAdapter?> GetAdapter(IAdapterCallContext context, string adapterId, CancellationToken cancellationToken) {
            var adapter = _adapters.FirstOrDefault(x => string.Equals(x.Descriptor.Id, adapterId, StringComparison.OrdinalIgnoreCase));
            if (adapter == null || !adapter.IsEnabled || !await IsAuthorized(adapter, context, cancellationToken).ConfigureAwait(false)) {
                return null;
            }

            return adapter;
        }

    }
}
