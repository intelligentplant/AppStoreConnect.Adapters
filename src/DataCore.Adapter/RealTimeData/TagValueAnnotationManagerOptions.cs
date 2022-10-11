using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Options for <see cref="TagValueAnnotationManagerBase"/>.
    /// </summary>
    public class TagValueAnnotationManagerOptions {

        /// <summary>
        /// A delegate that will receive tag names or IDs and will return the matching 
        /// <see cref="TagIdentifier"/>.
        /// </summary>
        /// <remarks>
        ///   <see cref="TagValueAnnotationManagerBase.CreateTagResolverFromAdapter(IAdapter)"/> or 
        ///   <see cref="TagValueAnnotationManagerBase.CreateTagResolverFromFeature(ITagInfo)"/> can be 
        ///   used to generate a compatible delegate using an existing adapter or 
        ///   <see cref="ITagInfo"/> implementation.
        /// </remarks>
        public TagResolver? TagResolver { get; set; }

    }
}
