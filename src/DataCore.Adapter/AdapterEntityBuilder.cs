using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for a builder class.
    /// </summary>
    public abstract class AdapterEntityBuilder {

        /// <summary>
        /// Bespoke tag value properties.
        /// </summary>
        private readonly Dictionary<string, AdapterProperty> _properties = new Dictionary<string, AdapterProperty>(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// Gets the bespoke properties for the entity in alphabetical order.
        /// </summary>
        /// <returns>
        ///   The entity properties.
        /// </returns>
        protected IEnumerable<AdapterProperty> GetProperties() => _properties.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// Adds a property to the builder.
        /// </summary>
        /// <param name="property">
        ///   The property to add.
        /// </param>
        internal void AddPropertyCore(AdapterProperty property) {
            if (property == null) {
                return;
            }

            _properties[property.Name] = property;
        }


        /// <summary>
        /// Adds a collection properties to the builder.
        /// </summary>
        /// <param name="properties">
        ///   The properties to add.
        /// </param>
        internal void AddPropertiesCore(IEnumerable<AdapterProperty> properties) {
            if (properties == null) {
                return;
            }

            foreach (var property in properties) {
                AddPropertyCore(property);
            }
        }


        /// <summary>
        /// Adds a property to the builder if it is not already present.
        /// </summary>
        /// <param name="property">
        ///   The property to add.
        /// </param>
        internal void TryAddPropertyCore(AdapterProperty property) {
            if (property == null) {
                return;
            }

#if NETSTANDARD2_1_OR_GREATER
            _properties.TryAdd(property.Name, property);
#else
            if (!_properties.ContainsKey(property.Name)) {
                _properties.Add(property.Name, property);
            }
#endif
        }


        /// <summary>
        /// Adds a collection properties to the builder if they do not already exist.
        /// </summary>
        /// <param name="properties">
        ///   The properties to add.
        /// </param>
        internal void TryAddPropertiesCore(IEnumerable<AdapterProperty> properties) {
            if (properties == null) {
                return;
            }

            foreach (var property in properties) {
                TryAddPropertyCore(property);
            }
        }


        /// <summary>
        /// Removes a property from the builder.
        /// </summary>
        /// <param name="name">
        ///   The name of the property to remove.
        /// </param>
        internal void RemovePropertyCore(string name) {
            if (name == null) {
                return;
            }

            _properties.Remove(name);
        }


        /// <summary>
        /// Removes all properties from the builder.
        /// </summary>
        internal void ClearPropertiesCore() {
            _properties.Clear();
        }

    }


    /// <summary>
    /// Base class for a builder class that can be used to construct an adapter entity.
    /// </summary>
    /// <typeparam name="T">
    ///   The entity type.
    /// </typeparam>
    public abstract class AdapterEntityBuilder<T> : AdapterEntityBuilder where T : class {

        /// <summary>
        /// Builds the entity.
        /// </summary>
        /// <returns>
        ///   The entity.
        /// </returns>
        public abstract T Build();

    }

}
