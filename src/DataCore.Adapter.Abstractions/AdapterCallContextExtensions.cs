using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapterCallContext"/>.
    /// </summary>
    public static class AdapterCallContextExtensions {

        /// <summary>
        /// Sets an entry in the <see cref="IAdapterCallContext.Items"/> dictionary.
        /// </summary>
        /// <typeparam name="TKey">
        ///   The key type.
        /// </typeparam>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        public static void SetItem<TKey, TValue>(this IAdapterCallContext context, TKey key, TValue? value) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            context.Items[key] = value;
        }


        /// <summary>
        /// Tries to get an entry from the <see cref="IAdapterCallContext.Items"/> dictionary.
        /// </summary>
        /// <typeparam name="TKey">
        ///   The key type.
        /// </typeparam>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="key"/> was found in the <see cref="IAdapterCallContext.Items"/> 
        ///   dictionary and the value is an instance of <typeparamref name="TValue"/>, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetItem<TKey, TValue>(this IAdapterCallContext context, TKey key, out TValue? value) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            value = default;
            if (!context.Items.TryGetValue(key, out var val) || !(val is TValue valActual)) {
                return false;
            }

            value = valActual;
            return true;
        }


        /// <summary>
        /// Specifies if <see cref="AdapterFeatureWrapper{TFeature}"/> instances should validate 
        /// request objects prior to invoking methods on their wrapped features that use this 
        /// <see cref="IAdapterCallContext"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <param name="enabled">
        ///   <see langword="true"/> to require request validation or <see langword="false"/> 
        ///   otherwise.
        /// </param>
        /// <remarks>
        ///   It may be desirable to disable request validation at the <see cref="AdapterFeatureWrapper{TFeature}"/> 
        ///   level if the request object has already been validated (for example, by an ASP.NET 
        ///   Core route handler).
        /// </remarks>
        public static void ValidateRequests(this IAdapterCallContext context, bool enabled) { 
            if (context == null) {
                return;
            }

            context.SetItem("adapter:validate-request-objects", enabled);
        }


        /// <summary>
        /// Checks if calls to <see cref="AdapterFeatureWrapper{TFeature}"/> that use this <see cref="IAdapterCallContext"/> 
        /// should validate request objects prior to invoking the inner feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> to require request validation or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        internal static bool ShouldValidateRequests(this IAdapterCallContext context) => context?.Items == null || !context.TryGetItem<string, bool>("adapter:validate-request-objects", out var value) || value;

    }
}
