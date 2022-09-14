
using System.Threading;

using Json.Schema.Generation;

namespace DataCore.Adapter.Json.Schema {

    /// <summary>
    /// Utility functions for JSON schema generation and validation.
    /// </summary>
    internal static class JsonSchemaUtility {

        /// <summary>
        /// Flags if JSON schema extensions have been registered.
        /// </summary>
        private static int s_extensionsRegistered;


        /// <summary>
        /// Registers JSON schema extensions.
        /// </summary>
        internal static void RegisterExtensions() {
            if (Interlocked.CompareExchange(ref s_extensionsRegistered, 1, 0) != 0) {
                // Already registered.
                return;
            }

            GeneratorRegistry.Register(new TimeSpanSchemaGenerator());

            AttributeHandler.AddHandler<DataTypeAttributeHandler>();
            AttributeHandler.AddHandler<DescriptionAttributeHandler>();
            AttributeHandler.AddHandler<DisplayAttributeHandler>();
            AttributeHandler.AddHandler<DisplayNameAttributeHandler>();
            AttributeHandler.AddHandler<MinLengthAttributeHandler>();
            AttributeHandler.AddHandler<MaxLengthAttributeHandler>();
            AttributeHandler.AddHandler<RangeAttributeHandler>();
            AttributeHandler.AddHandler<RegularExpressionAttributeHandler>();
            AttributeHandler.AddHandler<RequiredAttributeHandler>();
        }

    }
}
