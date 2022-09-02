using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes a response to an <see cref="InvocationRequest"/>.
    /// </summary>
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
    public class InvocationResponse {

        /// <summary>
        /// The invocation results.
        /// </summary>
        [Required]
        public Variant[] Results { get; set; } = Array.Empty<Variant>();

    }
}
