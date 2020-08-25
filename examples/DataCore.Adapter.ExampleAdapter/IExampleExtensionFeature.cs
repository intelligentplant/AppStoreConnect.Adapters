using System;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter extension feature.
    /// </summary>
    [AdapterFeature("asc:extension/example")]
    public interface IExampleExtensionFeature : IAdapterExtensionFeature {

        /// <summary>
        /// Gets the current time.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The response.
        /// </returns>
        GetCurrentTimeResponse GetCurrentTime(GetCurrentTimeRequest request);

    }

}
