using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Builder for constructing <see cref="AdapterDescriptorExtended"/> instances.
    /// </summary>
    public sealed class AdapterDescriptorBuilder : AdapterEntityBuilder<AdapterDescriptorExtended> {

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
        /// Creates a new <see cref="AdapterDescriptorBuilder"/> instance.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID. This will also be used as the adapter name.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        public AdapterDescriptorBuilder(string id): this(id, id) { } 


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
            WithId(id);
            WithName(name);
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

            WithTypeDescriptor(descriptor.TypeDescriptor);
            if (descriptor.Features != null) {
                foreach (var item in descriptor.Features) {
                    AddStandardFeature(item);
                }
            }
#pragma warning disable CS0618 // Type or member is obsolete
            if (descriptor.Extensions != null) {
                foreach (var item in descriptor.Extensions) {
                    AddExtensionFeature(item);
                }
            }
#pragma warning restore CS0618 // Type or member is obsolete
            this.WithProperties(descriptor.Properties);
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

            WithId(descriptor.Id);
            WithName(descriptor.Name);
            WithDescription(descriptor.Description);
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


        private void AddStandardFeature(string feature) {
            _adapterFeatures.Add(feature.InternToStringCache());
        }


        private void AddExtensionFeature(string feature) {
            _adapterExtensionFeatures.Add(feature.InternToStringCache());
        }


        private void RemoveStandardFeature(string feature) {
            _adapterFeatures.Remove(feature.InternToStringCache());
        }


        private void RemoveExtensionFeature(string feature) {
            _adapterExtensionFeatures.Remove(feature.InternToStringCache());
        }


        /// <summary>
        /// Clears the list of features supported by the adapter.
        /// </summary>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder ClearFeatures() {
            _adapterFeatures.Clear();
            _adapterExtensionFeatures.Clear();
            return this;
        }


        /// <summary>
        /// Removes the specified feature from the descriptor.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder ClearFeature<TFeature>() where TFeature : IAdapterFeature {
            var featureUri = typeof(TFeature).GetAdapterFeatureUri();

            if (featureUri == null) {
                return this;
            }

            return ClearFeature(featureUri);
        }


        /// <summary>
        /// Removes the specified feature from the descriptor.
        /// </summary>
        /// <param name="feature">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder ClearFeature(Uri feature) {
            if (feature != null) {
                var featureString = feature.ToString();
                RemoveStandardFeature(featureString);
                RemoveExtensionFeature(featureString);
            }
            return this;
        }


        /// <summary>
        /// Removes the specified feature from the descriptor.
        /// </summary>
        /// <param name="feature">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder ClearFeature(string feature) {
            if (feature != null && Uri.TryCreate(feature, UriKind.Absolute, out var featureUri)) {
                return ClearFeature(featureUri);
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
                if (feature == null || !feature.IsAbsoluteUri) {
                    continue;
                }

                WithFeature(feature);
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
        public AdapterDescriptorBuilder WithFeatures(IEnumerable<string> features) {
            if (features == null) {
                throw new ArgumentNullException(nameof(features));
            }

            foreach (var item in features) {
                if (item == null || !Uri.TryCreate(item, UriKind.Absolute, out var feature)) {
                    continue;
                }

                WithFeature(feature);
            }

            return this;
        }


        /// <summary>
        /// Adds the specified feature to the descriptor.
        /// </summary>
        /// <param name="feature">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="feature"/> is not a valid absolute URI.
        /// </exception>
        public AdapterDescriptorBuilder WithFeature(string feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            if (!Uri.TryCreate(feature, UriKind.Absolute, out var featureUri)) {
                throw new ArgumentOutOfRangeException(nameof(feature), SharedResources.Error_AbsoluteUriRequired);
            }

            return WithFeature(featureUri);
        }


        /// <summary>
        /// Adds the specified feature to the descriptor.
        /// </summary>
        /// <param name="feature">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="feature"/> is not a valid absolute URI.
        /// </exception>
        public AdapterDescriptorBuilder WithFeature(Uri feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (!feature.IsAbsoluteUri) {
                throw new ArgumentOutOfRangeException(nameof(feature), SharedResources.Error_AbsoluteUriRequired);
            }

            if (feature.IsExtensionFeatureUri()) {
                AddExtensionFeature(feature.ToString());
            }
            else {
                AddStandardFeature(feature.ToString());
            }

            return this;
        }


        /// <summary>
        /// Adds the specified feature to the descriptor.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <returns>
        ///   The <see cref="AdapterDescriptorBuilder"/>.
        /// </returns>
        public AdapterDescriptorBuilder WithFeature<TFeature>() where TFeature : IAdapterFeature {
            var featureUri = typeof(TFeature).GetAdapterFeatureUri();
            if (featureUri == null) {
                throw new InvalidOperationException(nameof(TFeature));
            }

            return WithFeature(featureUri);
        }


        /// <summary>
        /// Builds a new <see cref="AdapterDescriptorExtended"/> instance using the configured 
        /// options.
        /// </summary>
        /// <returns>
        ///   A new <see cref="AdapterDescriptorExtended"/> instance.
        /// </returns>
        public override AdapterDescriptorExtended Build() {
            return new AdapterDescriptorExtended(_id, _name, _description, _adapterFeatures.Select(x => x.ToString()), _adapterExtensionFeatures.Select(x => x.ToString()), GetProperties(), _typeDescriptor);
        }


    }
}
