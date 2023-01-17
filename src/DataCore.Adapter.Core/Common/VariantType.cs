using System.Text.Json.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes the type of a variant value.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Enum members all refer to data types")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VariantType {

        /// <summary>
        /// Unknown value type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// No value.
        /// </summary>
        Null = 1,

        /// <summary>
        /// Custom object.
        /// </summary>
        ExtensionObject = 2,

        /// <summary>
        /// Boolean.
        /// </summary>
        Boolean = 3,

        /// <summary>
        /// Signed byte.
        /// </summary>
        SByte = 4,

        /// <summary>
        /// Unsigned byte.
        /// </summary>
        Byte = 5,

        /// <summary>
        /// Signed 16-bit integer.
        /// </summary>
        Int16 = 6,

        /// <summary>
        /// Unsigned 16-bit integer.
        /// </summary>
        UInt16 = 7,

        /// <summary>
        /// Signed 32-bit integer.
        /// </summary>
        Int32 = 8,

        /// <summary>
        /// Unsigned 32-bit integer.
        /// </summary>
        UInt32 = 9,

        /// <summary>
        /// Signed 64-bit integer.
        /// </summary>
        Int64 = 10,

        /// <summary>
        /// Unsigned 64-bit integer.
        /// </summary>
        UInt64 = 11,

        /// <summary>
        /// Single precision floating point number.
        /// </summary>
        Float = 12,

        /// <summary>
        /// Double precision floating point number.
        /// </summary>
        Double = 13,

        /// <summary>
        /// String.
        /// </summary>
        String = 14,

        /// <summary>
        /// Timestamp.
        /// </summary>
        DateTime = 15,

        /// <summary>
        /// Time span.
        /// </summary>
        TimeSpan = 16,

        /// <summary>
        /// URL
        /// </summary>
        Url = 17,

        /// <summary>
        /// JSON
        /// </summary>
        Json = 18

    }

}
