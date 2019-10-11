using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a base class for data associated with a tag.
    /// </summary>
    public abstract class TagDataContainer {

        /// <summary>
        /// The tag ID.
        /// </summary>
        [Required]
        public string TagId { get; set; }

        /// <summary>
        /// The tag name.
        /// </summary>
        [Required]
        public string TagName { get; set; }

    }
}
