using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Describes a result for an annotations query on a tag.
    /// </summary>
    public class TagValueAnnotationQueryResult : TagDataContainer {

        /// <summary>
        /// The annotation.
        /// </summary>
        public TagValueAnnotation Annotation { get; }

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
        public TagValueAnnotationQueryResult(string tagId, string tagName, TagValueAnnotation annotation): base(tagId, tagName) {
            Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation)); 
        }

    }
}
