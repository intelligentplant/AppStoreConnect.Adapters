using DataCore.Adapter.Http.Client;

namespace DataCore.Adapter.Http.Proxy {

    /// <summary>
    /// Extensions for <see cref="IAdapterCallContext"/>.
    /// </summary>
    public static class AdapterCallContextExtensions {

        /// <summary>
        /// Creates a <see cref="RequestMetadata"/> object from the context.
        /// </summary>
        /// <param name="context">
        ///   The context.
        /// </param>
        /// <returns>
        ///   A new <see cref="RequestMetadata"/> object.
        /// </returns>
        public static RequestMetadata ToRequestMetadata(this IAdapterCallContext? context) {
            return new RequestMetadata(
                context?.User,
                context?.CorrelationId,
                null,
                context?.Items
            );
        }

    }
}
