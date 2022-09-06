using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Describes a configuration change on an adapter, such as the creation of a tag.
    /// </summary>
    public class ConfigurationChange {

        /// <summary>
        /// The type of the item.
        /// </summary>
        public string ItemType { get; }

        /// <summary>
        /// The ID of the item that was modified.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// The display name of the item that was modified.
        /// </summary>
        public string ItemName { get; }

        /// <summary>
        /// The change type.
        /// </summary>
        public ConfigurationChangeType ChangeType { get; }

        /// <summary>
        /// Additional properties associated with the change.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="ConfigurationChange"/> object.
        /// </summary>
        /// <param name="itemType">
        ///   The type of the item that was modified. Common values are defined in <see cref="ConfigurationChangeItemTypes"/>.
        /// </param>
        /// <param name="itemId">
        ///   The ID of the item that was modified.
        /// </param>
        /// <param name="itemName">
        ///   The display name of the item that was modified.
        /// </param>
        /// <param name="changeType">
        ///   The change type.
        /// </param>
        /// <param name="properties">
        ///   Additional properties associated with the change.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="itemId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="itemName"/> is <see langword="null"/> or white space.
        /// </exception>
        [JsonConstructor]
        public ConfigurationChange(string itemType, string itemId, string itemName, ConfigurationChangeType changeType, IEnumerable<AdapterProperty>? properties) {
            if (string.IsNullOrWhiteSpace(itemType)) {
                throw new ArgumentException(SharedResources.Error_TypeIsRequired, nameof(itemType));
            }
            if (string.IsNullOrWhiteSpace(itemId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(itemId));
            }
            if (string.IsNullOrWhiteSpace(itemName)) {
                throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(itemName));
            }
            ItemType = itemType;
            ItemId = itemId;
            ItemName = itemName;
            ChangeType = changeType;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }

    }

}
