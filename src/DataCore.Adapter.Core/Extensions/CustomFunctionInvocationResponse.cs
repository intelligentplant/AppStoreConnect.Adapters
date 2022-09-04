using System;
using System.Text.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A response to a custom function invocation.
    /// </summary>
    /// <remarks>
    ///   Use <see cref="Create{TBody}(TBody, JsonSerializerOptions?)"/> or 
    ///   <see cref="Create{TBody}(TBody, System.Text.Json.Serialization.Metadata.JsonTypeInfo{TBody})"/> 
    ///   to simplify creation of <see cref="CustomFunctionInvocationResponse"/> instances.
    /// </remarks>
    /// <seealso cref="Create{TBody}(TBody, JsonSerializerOptions?)"/>
    /// <seealso cref="Create{TBody}(TBody, System.Text.Json.Serialization.Metadata.JsonTypeInfo{TBody})"/>
    public class CustomFunctionInvocationResponse {

        /// <summary>
        /// The response payload.
        /// </summary>
        public JsonElement Body { get; set; }


        /// <summary>
        /// Creates a new <see cref="CustomFunctionInvocationResponse"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The response body type.
        /// </typeparam>
        /// <param name="body">
        ///   The response body.
        /// </param>
        /// <param name="options">
        ///   The <see cref="JsonSerializerOptions"/> to use when serializing the <paramref name="body"/> 
        ///   to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="CustomFunctionInvocationResponse"/>.
        /// </returns>
        public static CustomFunctionInvocationResponse Create<TBody>(TBody body, JsonSerializerOptions? options = null) {
            return new CustomFunctionInvocationResponse() {
                Body = JsonSerializer.SerializeToElement(body, options)
            };
        }


        /// <summary>
        /// Creates a new <see cref="CustomFunctionInvocationResponse"/> with the specified body.
        /// </summary>
        /// <typeparam name="TBody">
        ///   The response body type.
        /// </typeparam>
        /// <param name="body">
        ///   The request body.
        /// </param>
        /// <param name="jsonTypeInfo">
        ///   The <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> to use 
        ///   when serializing the <paramref name="body"/> to a <see cref="JsonElement"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="CustomFunctionInvocationResponse"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static CustomFunctionInvocationResponse Create<TBody>(TBody body, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TBody> jsonTypeInfo) {
            if (jsonTypeInfo == null) {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return new CustomFunctionInvocationResponse() {
                Body = JsonSerializer.SerializeToElement(body, jsonTypeInfo)
            };
        }

    }

}
