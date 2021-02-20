using System;
using System.ComponentModel.DataAnnotations;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a result for an annotations query on a tag.
    /// </summary>
    public class TagValueAnnotationQueryResult : TagDataContainer {

        /// <summary>
        /// The annotation.
        /// </summary>
        [Required]
        public TagValueAnnotationExtended Annotation { get; }


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
        public TagValueAnnotationQueryResult(string tagId, string tagName, TagValueAnnotationExtended annotation) 
            : base(tagId, tagName) {
            Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
        }


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
        public static TagValueAnnotationQueryResult Create(string tagId, string tagName, TagValueAnnotationExtended annotation) {
            return new TagValueAnnotationQueryResult(tagId, tagName, annotation);
        }


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationQueryResult"/> object.
        /// </summary>
        /// <param name="tagIdentifier">
        ///   The tag identifier.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagIdentifier"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="annotation"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueAnnotationQueryResult Create(TagIdentifier tagIdentifier, TagValueAnnotationExtended annotation) {
            return new TagValueAnnotationQueryResult(tagIdentifier?.Id!, tagIdentifier?.Name!, annotation);
        }

    }
}
