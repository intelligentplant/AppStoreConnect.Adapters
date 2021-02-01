using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter {

    /// <summary>
    /// <see cref="IAdapterAccessor"/> implementation that resolves <see cref="IAdapter"/> objects that 
    /// are passed in as constructor parameters via dependency injection.
    /// </summary>
    public class AspNetCoreAdapterAccessor : AdapterAccessor {

        /// <summary>
        /// The available adapters.
        /// </summary>
        private readonly IAdapter[] _adapters;


        /// <summary>
        /// Creates a new <see cref="AspNetCoreAdapterAccessor"/> object.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The background task service to use.
        /// </param>
        /// <param name="authorizationService">
        ///   The authorization service that will be used to control access to adapters.
        /// </param>
        /// <param name="adapters">
        ///   The ASP.NET Core hosted services.
        /// </param>
        public AspNetCoreAdapterAccessor(IBackgroundTaskService backgroundTaskService, IAdapterAuthorizationService authorizationService, IEnumerable<IAdapter>? adapters) 
            : base(backgroundTaskService, authorizationService) {
            _adapters = adapters?.OrderBy(x => x.Descriptor.Name, StringComparer.OrdinalIgnoreCase)?.ToArray() ?? Array.Empty<IAdapter>();
        }


        /// <summary>
        /// Returns the available adapters.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available adapters.
        /// </returns>
        protected override Task<ChannelReader<IAdapter>> GetAdapters(CancellationToken cancellationToken) {
            var channel = _adapters.PublishToChannel();
            return Task.FromResult(channel);
        }
    }
}
