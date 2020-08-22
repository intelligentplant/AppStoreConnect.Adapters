using System;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter extension feature.
    /// </summary>
    [AdapterFeature("asc:extension:example")]
    public interface IExampleExtensionFeature : IAdapterExtensionFeature {

        /// <summary>
        /// Gets the current time.
        /// </summary>
        /// <returns>
        ///   The current time.
        /// </returns>
        GetCurrentTimeResponse GetCurrentTime();

    }


    [ExtensionType("asc:extension:example:get-current-time:response")]
    public class GetCurrentTimeResponse {

        public DateTime UtcTime { get; set; }

    }

}
