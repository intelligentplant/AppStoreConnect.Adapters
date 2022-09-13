using System.Collections.Generic;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Service that provides information about the APIs that have been registered with the 
    /// adapter host.
    /// </summary>
    public interface IAvailableApiService {

        /// <summary>
        /// Gets the descriptors for the available APIs.
        /// </summary>
        /// <returns>
        ///   The API descriptors.
        /// </returns>
        IEnumerable<ApiDescriptor> GetApiDescriptors();

    }

}
