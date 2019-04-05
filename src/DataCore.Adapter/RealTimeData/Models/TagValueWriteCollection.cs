using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes values to be written to a tag.
    /// </summary>
    public class TagValueWriteCollection {

        /// <summary>
        /// The tag ID.
        /// </summary>
        [Required]
        public string TagId { get; set; }

        /// <summary>
        /// The values to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        public TagValueBase[] Values { get; set; }

    }
}
