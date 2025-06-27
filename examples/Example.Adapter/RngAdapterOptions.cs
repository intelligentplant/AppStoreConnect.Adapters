using System.ComponentModel.DataAnnotations;

using DataCore.Adapter;

namespace Example.Adapter {

    // This class defines the runtime options used to configure your adapter.

    public class RngAdapterOptions : AdapterOptions {

        // Add properties required to configure your adapter e.g. connection endpoints, 
        // credentials, etc. The Program.cs file is configured to bind adapter options
        // via app configuration.

        /// <summary>
        /// The random number generator seed to use when computing tag values.
        /// </summary>
        [Display(Description = "The random number generator seed to use when computing tag values (0 - 1000).")]
        [Required(ErrorMessage = "You must specify a seed.")]
        [Range(0, 1000, ErrorMessage = "Seed must be between 0 and 1000.")]
        public int Seed { get; set; }

    }

}
