using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions to support validation on adapter requests.
    /// </summary>
    public static class ValidationExtensions {

        /// <summary>
        /// Validates an object.
        /// </summary>
        /// <param name="o">
        ///   The object.
        /// </param>
        /// <param name="canBeNull">
        ///   When <see langword="true"/>, validation will succeed if <paramref name="o"/> is 
        ///   <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="o"/> is <see langword="null"/> and <paramref name="canBeNull"/> is 
        ///   <see langword="false"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="o"/> fails validation.
        /// </exception>
        public static void ValidateObject(object o, bool canBeNull = false) {
            if (canBeNull && o == null) {
                return;
            }

            if (o == null) {
                throw new ArgumentNullException(nameof(o));
            }

            Validator.ValidateObject(o, new ValidationContext(o), true);
        }

    }
}
