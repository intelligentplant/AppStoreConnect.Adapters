using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;
using DataCore.Adapter.DataValidation;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A request to get the extended descriptor for a custom function.
    /// </summary>
    public class GetCustomFunctionRequest : AdapterRequest {

        /// <summary>
        /// The function ID.
        /// </summary>
        /// <remarks>
        ///   If <see cref="Id"/> is a relative URI, the adapter will make it absolute relative 
        ///   to an adapter-defined base URI.
        /// </remarks>
        [Required]
        [MaxUriLength(500)]
        public Uri Id { get; set; } = default!;

    }
}
