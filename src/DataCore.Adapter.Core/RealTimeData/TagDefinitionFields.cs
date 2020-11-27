using System;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Indicates which parts of a <see cref="TagDefinition"/> to populate when returning the 
    /// results of a <see cref="FindTagsRequest"/>.
    /// </summary>
    [Flags]
    public enum TagDefinitionFields {

        /// <summary>
        /// Basic information (ID, name, description, units, data type).
        /// </summary>
        BasicInformation = 0,

        /// <summary>
        /// Digital state definitions associated with the tag.
        /// </summary>
        DigitalStates = 1,

        /// <summary>
        /// Bespoke tag properties.
        /// </summary>
        Properties = 2,

        /// <summary>
        /// Tag labels.
        /// </summary>
        Labels = 4,

        /// <summary>
        /// All available information.
        /// </summary>
        All = BasicInformation | DigitalStates | Properties | Labels

    }
}
