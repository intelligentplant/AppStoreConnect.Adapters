using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapterAccessor"/>.
    /// </summary>
    public static class AdapterAccessorExtensions {

        /// <summary>
        /// Gets all adapters registered with the <see cref="IAdapterAccessor"/>.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return the available adapters.
        /// </returns>
        public static async Task<IEnumerable<IAdapter>> GetAllAdapters(this IAdapterAccessor adapterAccessor, IAdapterCallContext context, CancellationToken cancellationToken = default) {
            if (adapterAccessor == null) {
                throw new ArgumentNullException(nameof(adapterAccessor));
            }
            
            const int pageSize = 100;
            var result = new List<IAdapter>(pageSize);

            var page = 0;
            var @continue = false;
            var request = new Common.FindAdaptersRequest() { 
                PageSize = pageSize
            };

            do {
                @continue = false;
                ++page;
                request.Page = page;
                var adapters = await adapterAccessor.FindAdapters(context, request, false, cancellationToken).ConfigureAwait(false);
                if (adapters != null) {
                    var countBefore = result.Count;
                    result.AddRange(adapters);
                    // If we received a full page of results, we will continue the loop.
                    @continue = (result.Count - countBefore) == pageSize;
                }
            } while (@continue);

            return result;

        }

    }
}
