using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Base <see cref="IAdapterAccessor"/> implementation that will authorize access to individual 
    /// adapters based on the provided <see cref="IAdapterAuthorizationService"/>.
    /// </summary>
    public abstract class AdapterAccessor : IAdapterAccessor {

        /// <summary>
        /// Service for running background operations.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;

        /// <inheritdoc/>
        public IAdapterAuthorizationService AuthorizationService { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterAccessor"/> object.
        /// </summary>
        /// <param name="backgroundTaskService">
        ///   The background task service to use.
        /// </param>
        /// <param name="authorizationService">
        ///   The adapter authorization service to use.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="authorizationService"/> is <see langword="null"/>.
        /// </exception>
        protected AdapterAccessor(IBackgroundTaskService backgroundTaskService, IAdapterAuthorizationService authorizationService) {
            _backgroundTaskService = backgroundTaskService ?? throw new ArgumentNullException(nameof(backgroundTaskService));
            AuthorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }


        /// <summary>
        /// Gets the available adapters.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available adapters.
        /// </returns>
        protected abstract Task<ChannelReader<IAdapter>> GetAdapters(CancellationToken cancellationToken);


        /// <inheritdoc/>
        public Task<ChannelReader<IAdapter>> FindAdapters(
            IAdapterCallContext context, 
            FindAdaptersRequest request, 
            bool enabledOnly = true,
            CancellationToken cancellationToken = default
        ) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            var result = ChannelExtensions.CreateChannel<IAdapter>(50);
            var skipCount = (request.Page - 1) * request.PageSize;
            var takeCount = request.PageSize;

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                using (var queryCtSource = CancellationTokenSource.CreateLinkedTokenSource(ct)) {
                    try {
                        var adapters = await GetAdapters(queryCtSource.Token).ConfigureAwait(false);

                        while (takeCount > 0 && await adapters.WaitToReadAsync(ct).ConfigureAwait(false)) {
                            while (adapters.TryRead(out var item)) {
                                if (enabledOnly && !item.IsEnabled) {
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(request.Id) && !item.Descriptor.Id.Like(request.Id!)) {
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(request.Name) && !item.Descriptor.Name.Like(request.Name!)) {
                                    continue;
                                }

                                if (!string.IsNullOrWhiteSpace(request.Description) && !item.Descriptor.Description.Like(request.Description!)) {
                                    continue;
                                }

                                if (request.Features != null) {
                                    foreach (var feature in request.Features) {
                                        if (string.IsNullOrWhiteSpace(feature)) {
                                            continue;
                                        }

                                        if (!item.HasFeature(feature)) {
                                            continue;
                                        }
                                    }
                                }

                                if (!await AuthorizationService.AuthorizeAdapter(item, context, ct).ConfigureAwait(false)) {
                                    // Caller is not authorized to use this adapter.
                                    continue;
                                }

                                if (skipCount > 0) {
                                    --skipCount;
                                    continue;
                                }

                                --takeCount;
                                await ch.WriteAsync(item, ct).ConfigureAwait(false);

                                if (takeCount < 1) {
                                    // We can finish now.
                                    break;
                                }
                            }
                        }
                    }
                    finally {
                        queryCtSource.Cancel();
                    }
                }
            }, true, _backgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc/>
        public async Task<IAdapter?> GetAdapter(
            IAdapterCallContext context, 
            string adapterId, 
            bool enabledOnly = true,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var adapters = await GetAdapters(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            while (await adapters.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (adapters.TryRead(out var item)) {
                    if (!item.Descriptor.Id.Equals(adapterId, StringComparison.OrdinalIgnoreCase)) {
                        continue;
                    }

                    if (enabledOnly && !item.IsEnabled) {
                        return null;
                    }

                    if (!await AuthorizationService.AuthorizeAdapter(item, context, cancellationToken).ConfigureAwait(false)) {
                        return null;
                    }

                    return item;
                }
            }

            return null;
        }

    }
}
