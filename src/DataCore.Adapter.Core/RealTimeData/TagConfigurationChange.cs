using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a configuration change for a tag.
    /// </summary>
    public class TagConfigurationChange {

        /// <summary>
        /// The tag that was modified.
        /// </summary>
        public TagIdentifier Tag { get; }

        /// <summary>
        /// The change type.
        /// </summary>
        public ConfigurationChangeType ChangeType { get; }

        /// <summary>
        /// Additional properties associated with the change.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="TagConfigurationChange"/> object.
        /// </summary>
        /// <param name="tag">
        ///   The tag that was modified.
        /// </param>
        /// <param name="changeType">
        ///   The change type.
        /// </param>
        /// <param name="properties">
        ///   Additional properties associated with the change.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public TagConfigurationChange(TagIdentifier tag, ConfigurationChangeType changeType, IEnumerable<AdapterProperty>? properties) {
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            ChangeType = changeType;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }

    }
}
