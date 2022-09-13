namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Service that provides information about an API that is registered with the adapter host.
    /// </summary>
    public interface IApiDescriptorProvider {

        /// <summary>
        /// Gets the descriptor from the provider.
        /// </summary>
        /// <returns>
        ///   The API descriptor.
        /// </returns>
        ApiDescriptor GetApiDescriptor();

    }
}
