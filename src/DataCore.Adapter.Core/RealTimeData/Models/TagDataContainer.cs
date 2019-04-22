using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a base class for data associated with a tag.
    /// </summary>
    public abstract class TagDataContainer {

        /// <summary>
        /// The tag ID.
        /// </summary>
        public string TagId { get; }

        /// <summary>
        /// The tag name.
        /// </summary>
        public string TagName { get; }


        /// <summary>
        /// Creates a new <see cref="TagDataContainer"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        protected TagDataContainer(string tagId, string tagName) {
            TagId = tagId ?? throw new ArgumentNullException(nameof(tagId));
            TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
        }

    }
}
