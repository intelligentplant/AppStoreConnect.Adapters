using System;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes an input parameter on an extension feature operation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class InputParameterDescriptionAttribute : Attribute {

        /// <summary>
        /// The parameter description.
        /// </summary>
        public string Description { get; }


        /// <summary>
        /// Creates a new <see cref="InputParameterDescriptionAttribute"/> object.
        /// </summary>
        /// <param name="description">
        ///   The parameter description.
        /// </param>
        public InputParameterDescriptionAttribute(
            string description
        ) {
            Description = description;
        }

    }


    /// <summary>
    /// Describes an output parameter on an extension feature operation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OutputParameterDescriptionAttribute : Attribute {

        /// <summary>
        /// The parameter description.
        /// </summary>
        public string Description { get; }


        /// <summary>
        /// Creates a new <see cref="OutputParameterDescriptionAttribute"/> object.
        /// </summary>
        /// <param name="description">
        ///   The parameter description.
        /// </param>
        public OutputParameterDescriptionAttribute(
            string description
        ) {
            Description = description;
        }

    }


}
