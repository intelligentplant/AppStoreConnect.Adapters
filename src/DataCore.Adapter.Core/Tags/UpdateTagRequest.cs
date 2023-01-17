using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// A request to update an existing tag definition.
    /// </summary>
    /// <seealso cref="GetTagSchemaRequest"/>
    public class UpdateTagRequest : AdapterRequest {

        /// <summary>
        /// The name or ID of the tag to modify.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Tag { get; set; } = default!;

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
        /// Creates a new <see cref="UpdateTagRequest"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The type of the request body.
        /// </typeparam>
        /// <param name="tag">
        ///   The name or ID of the tag to modify.
        /// </param>
        /// <param name="body">
        ///   The request body.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use when serializing the <paramref name="body"/> 
        ///   to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="UpdateTagRequest"/> object.
        /// </returns>
        public static UpdateTagRequest Create<TBody>(string tag, TBody body, JsonSerializerOptions? options = null) {
            return new UpdateTagRequest() {
                Tag = tag,
                Body = JsonSerializer.SerializeToElement(body, options)
            };
        }


        /// <summary>
        /// Creates a new <see cref="UpdateTagRequest"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The type of the request body.
        /// </typeparam>
        /// <param name="tag">
        ///   The name or ID of the tag to modify.
        /// </param>
        /// <param name="body">
        ///   The request body.
        /// </param>
        /// <param name="jsonTypeInfo">
        ///   The <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> to use 
        ///   when serializing the <paramref name="body"/> to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="UpdateTagRequest"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static UpdateTagRequest Create<TBody>(string tag, TBody body, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TBody> jsonTypeInfo) {
            if (jsonTypeInfo == null) {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return new UpdateTagRequest() {
                Tag = tag,
                Body = JsonSerializer.SerializeToElement(body, jsonTypeInfo)
            };
        }

    }
}
