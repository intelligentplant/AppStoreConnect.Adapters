using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Input arguments for a duplex streaming extension feature operation. 
    /// </summary>
    public class InvocationStreamItem {

        /// <summary>
        /// The encoded invocation arguments.
        /// </summary>
        [Required]
        public EncodedObject?[] Arguments { get; set; } = Array.Empty<EncodedObject>();

    }

}
