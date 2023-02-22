namespace DataCore.Adapter {

    /// <summary>
    /// Extends <see cref="IAdapterCallContext"/> to define a <see cref="Provider"/> that is used 
    /// to implement the other <see cref="IAdapterCallContext"/> members.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of the provider.
    /// </typeparam>
    public interface IAdapterCallContext<T> : IAdapterCallContext {

        /// <summary>
        /// The provider that is used to implement the other <see cref="IAdapterCallContext"/> 
        /// members.
        /// </summary>
        T Provider { get; }

    }

}
