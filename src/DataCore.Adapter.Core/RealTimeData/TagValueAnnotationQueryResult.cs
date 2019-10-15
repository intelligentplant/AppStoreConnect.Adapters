using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a result for an annotations query on a tag.
    /// </summary>
    public class TagValueAnnotationQueryResult : TagDataContainer {

        /// <summary>
        /// The annotation.
        /// </summary>
        [Required]
        public TagValueAnnotation Annotation { get; set; }

        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationQueryResult"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="annotation"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueAnnotationQueryResult Create(string tagId, string tagName, TagValueAnnotation annotation) {
            return new TagValueAnnotationQueryResult() {
                TagId = tagId ?? throw new ArgumentNullException(nameof(tagId)),
                TagName = tagName ?? throw new ArgumentNullException(nameof(tagName)),
                Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation))
            };
            
        }

    }
}
