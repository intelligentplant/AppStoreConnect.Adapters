﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataCore.Adapter.Common;
using DataCore.Adapter.DataValidation;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A request to invoke a custom adapter function.
    /// </summary>
    /// <remarks>
    ///   Use <see cref="Create{TBody}(Uri, TBody, JsonSerializerOptions?)"/> or 
    ///   <see cref="Create{TBody}(Uri, TBody, System.Text.Json.Serialization.Metadata.JsonTypeInfo{TBody})"/> 
    ///   to simplify creation of <see cref="CustomFunctionInvocationRequest"/> instances.
    /// </remarks>
    /// <seealso cref="Create{TBody}(Uri, TBody, JsonSerializerOptions?)"/>
    /// <seealso cref="Create{TBody}(Uri, TBody, System.Text.Json.Serialization.Metadata.JsonTypeInfo{TBody})"/>
    public class CustomFunctionInvocationRequest : AdapterRequest {

        /// <summary>
        /// The ID of the operation.
        /// </summary>
        /// <remarks>
        ///   If <see cref="Id"/> is a relative URI, the adapter will make it absolute relative 
        ///   to an adapter-defined base URI.
        /// </remarks>
        [Required]
        [MaxUriLength(500)]
        public Uri Id { get; set; } = default!;

        /// <summary>
        /// The request body.
        /// </summary>
        public JsonElement? Body { get; set; }


        /// <summary>
        /// Creates a new <see cref="CustomFunctionInvocationRequest"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The request body type.
        /// </typeparam>
        /// <param name="id">
        ///   The custom function ID.
        /// </param>
        /// <param name="body">
        ///   The request body.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use when serializing the <paramref name="body"/> 
        ///   to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="CustomFunctionInvocationRequest"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public static CustomFunctionInvocationRequest Create<TBody>(Uri id, TBody? body, JsonSerializerOptions? options = null) { 
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            return new CustomFunctionInvocationRequest() { 
                Id = id,
                Body = body is JsonElement json
                    ? json
                    : body == null 
                        ? null 
                        : JsonSerializer.SerializeToElement(body, options)
            };
        }


        /// <summary>
        /// Creates a new <see cref="CustomFunctionInvocationRequest"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The request body type.
        /// </typeparam>
        /// <param name="id">
        ///   The custom function ID.
        /// </param>
        /// <param name="body">
        ///   The request body.
        /// </param>
        /// <param name="jsonTypeInfo">
        ///   The <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> to use 
        ///   when serializing the <paramref name="body"/> to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="CustomFunctionInvocationRequest"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static CustomFunctionInvocationRequest Create<TBody>(Uri id, TBody? body, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TBody> jsonTypeInfo) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }
            if (jsonTypeInfo == null) {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return new CustomFunctionInvocationRequest() {
                Id = id,
                Body = body is JsonElement json
                    ? json
                    : body == null
                        ? null
                        : JsonSerializer.SerializeToElement(body, jsonTypeInfo)
            };
        }

    }
}
