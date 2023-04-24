using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Builder for constructing <see cref="AdapterDescriptorExtended"/> instances.
    /// </summary>
    public sealed class AdapterDescriptorBuilder {

        /// <summary>
        /// The adapter ID.
        /// </summary>
        private string _id = default!;

        /// <summary>
        /// The adapter name.
        /// </summary>
        private string _name = default!;

        /// <summary>
        /// The adapter description.
        /// </summary>
        private string? _description;

        /// <summary>
        /// The type descriptor.
        /// </summary>
        private AdapterTypeDescriptor? _typeDescriptor;

        /// <summary>
        /// The adapter features.
        /// </summary>
        private readonly HashSet<string> _adapterFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The adapter extension features.
        /// </summary>
        private readonly HashSet<string> _adapterExtensionFeatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The adapter properties.
        /// </summary>
        private readonly List<AdapterProperty> _properties = new List<AdapterProperty>();


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorBuilder"/> instance.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The adapter name.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public AdapterDescriptorBuilder(string id, string name) {
            _id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentOutOfRangeException(nameof(id), SharedResources.Error_IdIsRequired)
                : id;
            _name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentOutOfRangeException(nameof(name), SharedResources.Error_NameIsRequired)
                : name;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorBuilder"/> using an existing <paramref name="descriptor"/> 
        /// to initialise the builder.
        /// </summary>
        /// <param name="descriptor">
        ///   The existing <see cref="AdapterDescriptorExtended"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        public AdapterDescriptorBuilder(AdapterDescriptorExtended descriptor) : this((AdapterDescriptor) descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            _typeDescriptor = descriptor.TypeDescriptor;
            foreach (var item in descriptor.Features) {
                _adapterFeatures.Add(item);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var item in descriptor.Extensions) {
                _adapterExtensionFeatures.Add(item);
            }
#pragma warning restore CS0618 // Type or member is obsolete
            _properties.AddRange(descriptor.Properties);
        }


        /// <summary>
        /// Creates a new <see cref="AdapterDescriptorBuilder"/> using an existing <paramref name="descriptor"/> 
        /// to initialise the builder.
        /// </summary>
        /// <param name="descriptor">
        ///   The existing <see cref="AdapterDescriptor"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="descriptor"/> is <see langword="null"/>.
        /// </exception>
        public AdapterDescriptorBuilder(AdapterDescriptor descriptor) {
            if (descriptor == null) {
                throw new ArgumentNullException(nameof(descriptor));
            }

            _id = descriptor.Id;
            _name = descriptor.Name;
            _description = descriptor.Description;
        }


        /// <summary>
        /// Sets the adapter ID.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        public AdapterDescriptorBuilder WithId(string id) {
            _id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentOutOfRangeException(nameof(id), SharedResources.Error_IdIsRequired)
                : id;
            return this;
        }


        /// <summary>
        /// Sets the adapter name.
        /// </summary>
        /// <param name="name">
        ///   The adapter name.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public AdapterDescriptorBuilder WithName(string name) {
            _name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentOutOfRangeException(nameof(name), SharedResources.Error_NameIsRequired)
                : name;
            return this;
        }


        /// <summary>
        /// Sets the adapter description.
        /// </summary>
        /// <param name="description">
        ///   The adapter description.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder WithDescription(string? description) {
            _description = description;
            return this;
        }


        /// <summary>
        /// Sets the adapter type descriptor.
        /// </summary>
        /// <param name="typeDescriptor">
        ///   The adapter type descriptor.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder WithTypeDescriptor(AdapterTypeDescriptor? typeDescriptor) {
            _typeDescriptor = typeDescriptor;
            return this;
        }


        /// <summary>
        /// Clears the list of features supported by the adapter.
        /// </summary>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder ClearFeatures() {
            _adapterFeatures.Clear();
            return this;
        }


        /// <summary>
        /// Adds features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent standard adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        public AdapterDescriptorBuilder WithFeatures(params Uri[] features) => WithFeatures((IEnumerable<Uri>) features);


        /// <summary>
        /// Adds features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent standard adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        public AdapterDescriptorBuilder WithFeatures(IEnumerable<Uri> features) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }

            foreach (var feature in features) {
                if (feature == null || !feature.IsStandardFeatureUri()) {
                    continue;
                }

                _adapterFeatures.Add(feature.ToString());
            }

            return this;
        }


        /// <summary>
        /// Adds features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="features"/> contains one or more entries that are not valid feature URIs.
        /// </exception>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent standard adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        public AdapterDescriptorBuilder WithFeatures(params string[] features) => WithFeatures((IEnumerable<string>) features);


        /// <summary>
        /// Adds features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent standard adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        public AdapterDescriptorBuilder WithFeatures(IEnumerable<string> features) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }

            foreach (var item in features) {
                if (item == null || !Uri.TryCreate(item, UriKind.Absolute, out var feature) || !feature.IsStandardFeatureUri()) {
                    continue;
                }

                _adapterFeatures.Add(feature.ToString());
            }

            return this;
        }


        /// <summary>
        /// Adds the specified feature to the descriptor.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type. Types that do not represent standard adapter features will be 
        ///   ignored.
        /// </typeparam>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder WithFeature<TFeature>() where TFeature : IAdapterFeature {
            if (typeof(TFeature).IsStandardAdapterFeature()) {
                _adapterFeatures.Add(typeof(TFeature).GetAdapterFeatureUri()!.ToString());
            }
            return this;
        }


        /// <summary>
        /// Clears the list of extension features supported by the adapter.
        /// </summary>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        [Obsolete(Extensions.ExtensionFeatureConstants.ObsoleteMessage, Extensions.ExtensionFeatureConstants.ObsoleteError)]
        public AdapterDescriptorBuilder ClearExtensionFeatures() {
            _adapterExtensionFeatures.Clear();
            return this;
        }


        /// <summary>
        /// Adds extension features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The extension feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent extension adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        [Obsolete(Extensions.ExtensionFeatureConstants.ObsoleteMessage, Extensions.ExtensionFeatureConstants.ObsoleteError)]
        public AdapterDescriptorBuilder WithExtensionFeatures(params Uri[] features) => WithExtensionFeatures((IEnumerable<Uri>) features);


        /// <summary>
        /// Adds extension features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The extension feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent extension adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        [Obsolete(Extensions.ExtensionFeatureConstants.ObsoleteMessage, Extensions.ExtensionFeatureConstants.ObsoleteError)]
        public AdapterDescriptorBuilder WithExtensionFeatures(IEnumerable<Uri> features) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }

            foreach (var feature in features) {
                if (feature == null || !feature.IsExtensionFeatureUri()) {
                    continue;
                }

                _adapterExtensionFeatures.Add(feature.ToString());
            }

            return this;
        }


        /// <summary>
        /// Adds extension features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The extension feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent extension adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        [Obsolete(Extensions.ExtensionFeatureConstants.ObsoleteMessage, Extensions.ExtensionFeatureConstants.ObsoleteError)]
        public AdapterDescriptorBuilder WithExtensionFeatures(params string[] features) => WithExtensionFeatures((IEnumerable<string>) features);


        /// <summary>
        /// Adds extension features supported by the adapter.
        /// </summary>
        /// <param name="features">
        ///   The extension feature IDs.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="features"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="features"/> entries that do not represent extension adapter feature 
        ///   IDs will be ignored.
        /// </remarks>
        [Obsolete(Extensions.ExtensionFeatureConstants.ObsoleteMessage, Extensions.ExtensionFeatureConstants.ObsoleteError)]
        public AdapterDescriptorBuilder WithExtensionFeatures(IEnumerable<string> features) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }

            foreach (var item in features) {
                if (!Uri.TryCreate(item, UriKind.Absolute, out var feature) || !feature.IsExtensionFeatureUri()) {
                    continue;
                }

                _adapterExtensionFeatures.Add(feature.ToString());
            }

            return this;
        }


        /// <summary>
        /// Adds the specified feature to the descriptor.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type. Types that do not represent extension adapter features will be 
        ///   ignored.
        /// </typeparam>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        [Obsolete(Extensions.ExtensionFeatureConstants.ObsoleteMessage, Extensions.ExtensionFeatureConstants.ObsoleteError)]
        public AdapterDescriptorBuilder WithExtensionFeature<TFeature>() where TFeature : Extensions.IAdapterExtensionFeature {
            if (typeof(TFeature).IsExtensionAdapterFeature()) {
                _adapterExtensionFeatures.Add(typeof(TFeature).GetAdapterFeatureUri()!.ToString());
            }
            return this;
        }


        /// <summary>
        /// Clears the list of custom adapter properties.
        /// </summary>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder ClearProperties() {
            _properties.Clear();
            return this;
        }


        /// <summary>
        /// Adds the specified custom adapter properties.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder WithProperties(params AdapterProperty[] properties) => WithProperties((IEnumerable<AdapterProperty>) properties);


        /// <summary>
        /// Adds the specified custom adapter properties.
        /// </summary>
        /// <param name="properties">
        ///   The properties.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="properties"/> is <see langword="null"/>.
        /// </exception>
        public AdapterDescriptorBuilder WithProperties(IEnumerable<AdapterProperty> properties) {
            if (properties == null) {
                throw new ArgumentNullException(nameof(properties));
            }

            foreach (var item in properties) {
                if (item == null) {
                    continue;
                }
                _properties.Add(item);
            }
            return this;
        }


        /// <summary>
        /// Builds a new <see cref="AdapterDescriptorExtended"/> instance using the configured 
        /// options.
        /// </summary>
        /// <returns>
        ///   A new <see cref="AdapterDescriptorExtended"/> instance.
        /// </returns>
        public AdapterDescriptorExtended Build() {
            return new AdapterDescriptorExtended(_id, _name, _description, _adapterFeatures.Select(x => x.ToString()), _adapterExtensionFeatures.Select(x => x.ToString()), _properties, _typeDescriptor);
        }


    }
}
