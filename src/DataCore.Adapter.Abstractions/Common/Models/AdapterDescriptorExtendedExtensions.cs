﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.Common.Models {

    /// <summary>
    /// Extensions for <see cref="AdapterDescriptorExtended"/>.
    /// </summary>
    public static class AdapterDescriptorExtendedExtensions {

        /// <summary>
        /// Tests if the descriptor contains the specified feature in its <see cref="AdapterDescriptorExtended.Features"/> 
        /// list.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature<TFeature>(this AdapterDescriptorExtended descriptor) where TFeature : IAdapterFeature {
            if (descriptor == null) {
                return false;
            }

            return typeof(TFeature).IsExtensionAdapterFeature()
                ? descriptor.HasFeature(typeof(TFeature).FullName)
                : descriptor.HasFeature(typeof(TFeature).Name);
        }


        /// <summary>
        /// Tests if the descriptor contains the specified feature in its <see cref="AdapterDescriptorExtended.Features"/> 
        /// list.
        /// </summary>
        /// <param name="descriptor">
        ///   The descriptor.
        /// </param>
        /// <param name="featureName">
        ///   The feature name.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature is in the list, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool HasFeature(this AdapterDescriptorExtended descriptor, string featureName) {
            if (descriptor == null) {
                return false;
            }

            return descriptor.Features.Any(f => string.Equals(f, featureName)) || descriptor.Extensions.Any(f => string.Equals(f, featureName));
        }


        /// <summary>
        /// Creates an <see cref="AdapterDescriptorExtended"/> for the <see cref="IAdapter"/>.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The <see cref="AdapterDescriptorExtended"/> for the adapter.
        /// </returns>
        public static AdapterDescriptorExtended CreateExtendedAdapterDescriptor(this IAdapter adapter) {
            if (adapter == null) {
                return null;
            }

            var standardFeatures = adapter
                .Features
                ?.Keys
                ?.Where(x => x.IsStandardAdapterFeature())
                .ToArray() ?? new Type[0];

            var extensionFeatures = adapter
                .Features
                ?.Keys
                ?.Except(standardFeatures)
                .ToArray();

            return new AdapterDescriptorExtended(
                adapter.Descriptor.Id,
                adapter.Descriptor.Name,
                adapter.Descriptor.Description,
                standardFeatures.OrderBy(x => x.Name).Select(x => x.Name).ToArray(),
                extensionFeatures.OrderBy(x => x.FullName).Select(x => x.FullName).ToArray()
            );
        }

    }
}
