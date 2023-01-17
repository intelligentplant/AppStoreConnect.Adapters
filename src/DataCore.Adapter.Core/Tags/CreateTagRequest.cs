using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// A request to create a new tag definition.
    /// </summary>
    /// <seealso cref="GetTagSchemaRequest"/>
    public class CreateTagRequest : AdapterRequest {

        /// <summary>
        /// The request body.
        /// </summary>
        /// <remarks>
        ///   The schema for the <see cref="Body"/> is obtained by sending a <see cref="GetTagSchemaRequest"/> 
        ///   to the adapter.
        /// </remarks>
        [Required]
        public JsonElement Body { get; set; }


        /// <summary>
        /// Creates a new <see cref="CreateTagRequest"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The type of the request body.
        /// </typeparam>
        /// <param name="body">
        ///   The request body.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use when serializing the <paramref name="body"/> 
        ///   to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="CreateTagRequest"/> object.
        /// </returns>
        public static CreateTagRequest Create<TBody>(TBody body, JsonSerializerOptions? options = null) {
            return new CreateTagRequest() {
                Body = JsonSerializer.SerializeToElement(body, options)
            };
        }


        /// <summary>
        /// Creates a new <see cref="CreateTagRequest"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The type of the request body.
        /// </typeparam>
        /// <param name="body">
        ///   The request body.
        /// </param>
        /// <param name="jsonTypeInfo">
        ///   The <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> to use 
        ///   when serializing the <paramref name="body"/> to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="CreateTagRequest"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static CreateTagRequest Create<TBody>(TBody body, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TBody> jsonTypeInfo) {
            if (jsonTypeInfo == null) {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return new CreateTagRequest() {
                Body = JsonSerializer.SerializeToElement(body, jsonTypeInfo)
            };
        }

    }
}
