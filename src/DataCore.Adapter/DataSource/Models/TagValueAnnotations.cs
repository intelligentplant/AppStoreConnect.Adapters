using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.DataSource.Models {

    /// <summary>
    /// Describes the results of an annotations query on a tag.
    /// </summary>
    public class TagValueAnnotations: TagDataContainer {

        /// <summary>
        /// The annotations.
        /// </summary>
        public IEnumerable<TagValueAnnotation> Annotations { get; }

        /// <summary>
        /// Creates a new <see cref="TagValueAnnotations"/> object.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <param name="tagName">
        ///   The tag name.
        /// </param>
        /// <param name="annotations">
        ///   The annotations.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagName"/> is <see langword="null"/>.
        /// </exception>
        public TagValueAnnotations(string tagId, string tagName, IEnumerable<TagValueAnnotation> annotations): base(tagId, tagName) {
            Annotations = annotations?.ToArray() ?? new TagValueAnnotation[0]; 
        }

    }
}
