using System;
using System.Collections.Generic;

namespace DataCore.Adapter.WaveGenerator {

    /// <summary>
    /// Options for <see cref="WaveGeneratorAdapter"/>.
    /// </summary>
    public class WaveGeneratorAdapterOptions : AdapterOptions {

        /// <summary>
        /// The interval between raw samples calculated by a wave generator function.
        /// </summary>
        public TimeSpan SampleInterval { get; set; } = TimeSpan.FromSeconds(30);


        /// <summary>
        /// A map from tag name to the wave generator settings for that tag.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Setter required for easier deserialization")]
        public IDictionary<string, string>? Tags { get; set; }


        /// <summary>
        /// When <see langword="true"/>, callers can request values from ad hoc wave generators by 
        /// specifying a tag name using the wave generator syntax. When <see langword="false"/>, 
        /// only the built-in generators and those defined in the <see cref="Tags"/> property can 
        /// be requested.
        /// </summary>
        public bool EnableAdHocGenerators { get; set; }

    }
}
