using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Represents an unauthorized attempt to access an adapter or invoke an adapter feature.
    /// </summary>
    public class NotAuthorizedException : Exception {

        /// <summary>
        /// Creates a new <see cref="NotAuthorizedException"/> object.
        /// </summary>
        public NotAuthorizedException() { }


        /// <summary>
        /// Creates a new <see cref="NotAuthorizedException"/> object using the specified message.
        /// </summary>
        /// <param name="message">
        ///   The message.
        /// </param>
        public NotAuthorizedException(string message) : base(message) { }


        /// <summary>
        /// Creates a new <see cref="NotAuthorizedException"/> object using the specified message and inner exception.
        /// </summary>
        /// <param name="message">
        ///   The message.
        /// </param>
        /// <param name="innerException">
        ///   The inner exception.
        /// </param>
        public NotAuthorizedException(string message, Exception innerException) : base(message, innerException) { }

    }
}
