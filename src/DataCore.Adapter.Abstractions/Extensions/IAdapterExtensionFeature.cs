namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Interface that all non-standard adapter features must implement.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   Extension features are defined by creating an interface that extends 
    ///   <see cref="IAdapterExtensionFeature"/>, and is annotated with 
    ///   <see cref="AdapterFeatureAttribute"/>.
    /// </para>
    /// 
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Need to identify adapter feature types")]
    public interface IAdapterExtensionFeature : IAdapterFeature { }

}
