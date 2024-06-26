using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a base class for data associated with a tag.
    /// </summary>
    public abstract class TagDataContainer {

        /// <summary>
        /// The tag ID.
        /// </summary>
        [Required]
        public string TagId { get; }

        /// <summary>
        /// The tag name.
        /// </summary>
        [Required]
        public string TagName { get; }


        /// <summary>
        /// Creates a new <see cref="TagDataContainer"/>.
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
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }
            if (tagName == null) {
                throw new ArgumentNullException(nameof(tagName));
            }

            TagId = tagId.InternToStringCache();
            TagName = tagName.InternToStringCache();
        }

    }
}
