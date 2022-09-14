using System.Collections.Generic;

namespace DataCore.Adapter.AspNetCore.Internal {

    /// <summary>
    /// Default <see cref="IAvailableApiService"/> implementation.
    /// </summary>
    internal sealed class DefaultAvailableApiService : IAvailableApiService {

        /// <summary>
        /// The API descriptor providers.
        /// </summary>
        private readonly IEnumerable<IApiDescriptorProvider> _providers;


        /// <summary>
        /// Creates a new <see cref="DefaultAvailableApiService"/> instance.
        /// </summary>
        /// <param name="providers">
        ///   The API descriptor providers.
        /// </param>
        public DefaultAvailableApiService(IEnumerable<IApiDescriptorProvider> providers) {
            _providers = providers;
        }


        /// <inheritdoc/>
        public IEnumerable<ApiDescriptor> GetApiDescriptors() {
            foreach (var provider in _providers) {
                yield return provider.GetApiDescriptor();
            }
        }
    }
}
