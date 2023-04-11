using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Defines names for well-known properties on adapter entities.
    /// </summary>
    public static class WellKnownProperties {

        /// <summary>
        /// Properties for <see cref="RealTimeData.TagValueExtended"/>.
        /// </summary>
        public static class TagValue {

            /// <summary>
            /// The display value for the tag value.
            /// </summary>
            /// <remarks>
            ///   This property can be used to supply a pre-formatted value instead of relying on 
            ///   calling <see cref="Common.Variant.ToString(string?, IFormatProvider?)"/>. An 
            ///   example use case is to supply the value of a digital tag as an <see cref="int"/>
            ///   value, but to use the <see cref="DisplayValue"/> property to define the name of
            ///   the digital state.
            /// </remarks>
            public const string DisplayValue = "Display-Value";

            /// <summary>
            /// Specifies if a tag value represents a stepped value transition (such as a discrete 
            /// value change on a digital tag or an analogue process limit). 
            /// </summary>
            /// <remarks>
            ///   This property can be used to provide a hint to an application about how a tag 
            ///   value should be visualized.
            /// </remarks>
            public const string Stepped = "Stepped";

        }

    }
}
