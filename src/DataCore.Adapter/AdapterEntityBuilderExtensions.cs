using System;
using System.Collections.Generic;

using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods for <see cref="AdapterEntityBuilder{T}"/>.
    /// </summary>
    public static class AdapterEntityBuilderExtensions {

        /// <summary>
        /// Adds a property to the builder.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///   The builder type.
        /// </typeparam>
        /// <param name="builder">
        ///   The builder.
        /// </param>
        /// <param name="property">
        ///   The property to add.
        /// </param>
        /// <returns>
        ///   The updated builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static TBuilder WithProperty<TBuilder>(this TBuilder builder, AdapterProperty property) where TBuilder : AdapterEntityBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            if (property == null) {
                return builder;
            }

            builder.AddPropertyCore(property);
            return builder;
        }


        /// <summary>
        /// Adds a collection of properties to the builder.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///   The builder type.
        /// </typeparam>
        /// <param name="builder">
        ///   The builder.
        /// </param>
        /// <param name="properties">
        ///   The properties to add.
        /// </param>
        /// <returns>
        ///   The updated builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static TBuilder WithProperties<TBuilder>(this TBuilder builder, IEnumerable<AdapterProperty> properties) where TBuilder : AdapterEntityBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            if (properties == null) {
                return builder;
            }

            builder.AddPropertiesCore(properties);
            return builder;
        }


        /// <summary>
        /// Adds a collection of properties to the builder.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///   The builder type.
        /// </typeparam>
        /// <param name="builder">
        ///   The builder.
        /// </param>
        /// <param name="properties">
        ///   The properties to add.
        /// </param>
        /// <returns>
        ///   The updated builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static TBuilder WithProperties<TBuilder>(this TBuilder builder, params AdapterProperty[] properties) where TBuilder : AdapterEntityBuilder {
            return builder.WithProperties((IEnumerable<AdapterProperty>) properties);
        }


        /// <summary>
        /// Adds a property to the builder.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///   The builder type.
        /// </typeparam>
        /// <param name="builder">
        ///   The builder.
        /// </param>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <returns>
        ///   The updated builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static TBuilder WithProperty<TBuilder>(this TBuilder builder, string name, Variant value, string? description = null) where TBuilder : AdapterEntityBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null) {
                return builder;
            }

            return builder.WithProperty(new AdapterProperty(name, value, description));
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters


        /// <summary>
        /// Adds a property to the builder.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///   The builder type.
        /// </typeparam>
        /// <typeparam name="TValue">
        ///   The property value type.
        /// </typeparam>
        /// <param name="builder">
        ///   The builder.
        /// </param>
        /// <param name="name">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The property value.
        /// </param>
        /// <param name="description">
        ///   The property description.
        /// </param>
        /// <returns>
        ///   The updated builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static TBuilder WithProperty<TBuilder, TValue>(this TBuilder builder, string name, TValue value, string? description = null) where TBuilder : AdapterEntityBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            if (name == null) {
                return builder;
            }

            return builder.WithProperty(new AdapterProperty(name, Variant.FromValue(value), description));
        }
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters


        /// <summary>
        /// Removes a property from the builder.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///   The builder type.
        /// </typeparam>
        /// <param name="builder">
        ///   The builder.
        /// </param>
        /// <param name="name">
        ///   The name of the property to remove.
        /// </param>
        /// <returns>
        ///   The updated builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static TBuilder RemoveProperty<TBuilder>(this TBuilder builder, string name) where TBuilder : AdapterEntityBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.RemovePropertyCore(name);
            return builder;
        }


        /// <summary>
        /// Removes all properties from the builder.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///   The builder type.
        /// </typeparam>
        /// <param name="builder">
        ///   The builder.
        /// </param>
        /// <returns>
        ///   The updated builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static TBuilder ClearProperties<TBuilder>(this TBuilder builder) where TBuilder : AdapterEntityBuilder {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ClearPropertiesCore();
            return builder;
        }

    }

}
