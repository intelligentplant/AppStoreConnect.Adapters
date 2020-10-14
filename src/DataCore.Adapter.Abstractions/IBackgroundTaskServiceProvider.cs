using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes an interface that has a <see cref="IBackgroundTaskService"/> that can be used to 
    /// register background operations.
    /// </summary>
    public interface IBackgroundTaskServiceProvider {

        /// <summary>
        /// The <see cref="IBackgroundTaskService"/> used by the provider.
        /// </summary>
        IBackgroundTaskService BackgroundTaskService { get; }

    }
}
