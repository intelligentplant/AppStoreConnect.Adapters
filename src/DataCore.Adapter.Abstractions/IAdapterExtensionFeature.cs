namespace DataCore.Adapter {
    /// <summary>
    /// Interface that all non-standard adapter features must implement.
    /// </summary>

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Need to distinguish standard features from extension features.")]
    public interface IAdapterExtensionFeature : IAdapterFeature { }

}
