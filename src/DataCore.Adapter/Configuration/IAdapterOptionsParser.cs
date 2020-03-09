using System.Collections.Generic;

namespace DataCore.Adapter.Configuration {

    /// <summary>
    /// Describes a parser that can parse options for an adapter based on a pre-defined text schema.
    /// </summary>
    /// <typeparam name="TAdapter">
    ///   The adapter type.
    /// </typeparam>
    /// <typeparam name="TAdapterOptions">
    ///   The adapter options type.
    /// </typeparam>
    public interface IAdapterOptionsParser<TAdapter, TAdapterOptions> 
        where TAdapter : AdapterBase<TAdapterOptions> 
        where TAdapterOptions : AdapterOptions 
    {

        /// <summary>
        /// Gets the schema for the adapter options.
        /// </summary>
        /// <returns>
        ///   The options schema.
        /// </returns>
        AdapterOptionsSchema GetSchema();


        /// <summary>
        /// Tries to parse the provided input string using the schema.
        /// </summary>
        /// <param name="s">
        ///   The string to parse.
        /// </param>
        /// <param name="options">
        ///   The parsed options.
        /// </param>
        /// <param name="errors">
        ///   The validation errors, if the return value is <see langword="false"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string was successfully parsed, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <remarks>
        ///   When returning <see langword="false"/>, implementers should ensure that the 
        ///   <paramref name="errors"/> parameter is populated.
        /// </remarks>
        bool TryParse(string s, out TAdapterOptions options, out IEnumerable<ValidationError> errors);


        /// <summary>
        /// Converts the specified adapter options to a string using the parser's schema.
        /// </summary>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <returns>
        ///   The serialized adapter options.
        /// </returns>
        string ToString(TAdapterOptions options);

    }
}
