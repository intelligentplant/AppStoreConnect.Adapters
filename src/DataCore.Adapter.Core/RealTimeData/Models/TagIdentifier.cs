﻿using System;
using System.Collections.Generic;

namespace DataCore.Adapter.RealTimeData.Models {

    /// <summary>
    /// Defines basic information for identifying a real-time data tag.
    /// </summary>
    public class TagIdentifier : ITagIdentifier {

        /// <summary>
        /// The unique identifier for the tag.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The tag name.
        /// </summary>
        public string Name { get; }


        /// <summary>
        /// Creates a new <see cref="TagIdentifier"/> object.
        /// </summary>
        /// <param name="id">
        ///   The unique identifier for the tag.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagIdentifier(string id, string name) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

    }


    /// <summary>
    /// Equality comparer for <see cref="TagIdentifier"/> objects.
    /// </summary>
    public class TagIdentifierComparer : IEqualityComparer<TagIdentifier> {

        /// <summary>
        /// <see cref="TagIdentifierComparer"/> that tests for equality based solely on the 
        /// <see cref="TagIdentifier.Id"/> property.
        /// </summary>
        public static TagIdentifierComparer Id { get; } = new TagIdentifierComparer(true);

        /// <summary>
        /// <see cref="TagIdentifierComparer"/> that tests for equality based on the 
        /// <see cref="TagIdentifier.Id"/> and <see cref="TagIdentifier.Name"/> properties.
        /// </summary>
        public static TagIdentifierComparer IdAndName { get; } = new TagIdentifierComparer(false);

        /// <summary>
        /// Flags if only ID should be compared.
        /// </summary>
        private readonly bool _compareIdOnly;


        /// <summary>
        /// Creates a new <see cref="TagIdentifierComparer"/> object.
        /// </summary>
        /// <param name="compareIdOnly">
        ///   Flags if only ID should be compared.
        /// </param>
        private TagIdentifierComparer(bool compareIdOnly) {
            _compareIdOnly = compareIdOnly;
        }


        /// <summary>
        /// Tests two <see cref="TagIdentifier"/> instances for equality.
        /// </summary>
        /// <param name="x">
        ///   The first instance to compare.
        /// </param>
        /// <param name="y">
        ///   The second instance to compare.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the instances are equal, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public bool Equals(TagIdentifier x, TagIdentifier y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }

            var idMatch = string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);
            if (!idMatch || _compareIdOnly) {
                return idMatch;
            }

            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Gets the hash code for the specific <see cref="TagIdentifier"/> instance.
        /// </summary>
        /// <param name="obj">
        ///   The instance.
        /// </param>
        /// <returns>
        ///   The hash code for the instance.
        /// </returns>
        public int GetHashCode(TagIdentifier obj) {
            return obj?.Id.ToUpperInvariant().GetHashCode() ?? default;
        }
    }
}
