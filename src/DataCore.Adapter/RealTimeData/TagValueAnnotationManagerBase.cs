using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Base <see cref="IReadTagValueAnnotations"/> and <see cref="IWriteTagValueAnnotations"/> 
    /// implementation.
    /// </summary>
    public abstract class TagValueAnnotationManagerBase : IReadTagValueAnnotations, IWriteTagValueAnnotations {

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// The options for the service.
        /// </summary>
        private readonly TagValueAnnotationManagerOptions _options;


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationManagerBase"/> instance.
        /// </summary>
        /// <param name="options">
        ///   The options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use.
        /// </param>
        protected TagValueAnnotationManagerBase(
            TagValueAnnotationManagerOptions? options,
            IBackgroundTaskService? backgroundTaskService
        ) {
            _options = options ?? new TagValueAnnotationManagerOptions();
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
        }


        /// <inheritdoc/>
        async IAsyncEnumerable<TagValueAnnotationQueryResult> IReadTagValueAnnotations.ReadAnnotations(
            IAdapterCallContext context, 
            ReadAnnotationsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);
            await foreach (var item in ReadAnnotationsAsync(context, request, cancellationToken).ConfigureAwait(false)) {
                if (item == null) {
                    continue;
                }
                yield return item;
            }
        }


        /// <inheritdoc/>
        async Task<TagValueAnnotationExtended?> IReadTagValueAnnotations.ReadAnnotation(
            IAdapterCallContext context, 
            ReadAnnotationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);
            return await ReadAnnotationAsync(context, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async Task<WriteTagValueAnnotationResult> IWriteTagValueAnnotations.CreateAnnotation(
            IAdapterCallContext context, 
            CreateAnnotationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);
            return await CreateAnnotationAsync(context, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async Task<WriteTagValueAnnotationResult> IWriteTagValueAnnotations.UpdateAnnotation(
            IAdapterCallContext context, 
            UpdateAnnotationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);
            return await UpdateAnnotationAsync(context, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async Task<WriteTagValueAnnotationResult> IWriteTagValueAnnotations.DeleteAnnotation(
            IAdapterCallContext context, 
            DeleteAnnotationRequest request, 
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            ValidationExtensions.ValidateObject(request);
            return await DeleteAnnotationAsync(context, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Resolves tag IDs and names.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="tags">
        ///   The tag IDs or names to resolve.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The <see cref="TagIdentifier"/> instances for the resolved tags.
        /// </returns>
        /// <remarks>
        ///   The default behaviour is to use <see cref="TagValueAnnotationManagerOptions.TagResolver"/> 
        ///   if it is non-<see langword="null"/>. If <see cref="TagValueAnnotationManagerOptions.TagResolver"/> 
        ///   is <see langword="null"/> then every item in <paramref name="tags"/> is resolve to a 
        ///   <see cref="TagIdentifier"/> with an identical ID and name. 
        /// </remarks>
        protected virtual async IAsyncEnumerable<TagIdentifier> ResolveTagsAsync(
            IAdapterCallContext context, 
            IEnumerable<string> tags,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (_options.TagResolver != null) {
                await foreach (var item in _options.TagResolver.Invoke(context, tags, cancellationToken).ConfigureAwait(false)) {
                    if (item != null) {
                        yield return item;
                    }
                    yield break;
                }
            }

            await Task.Yield();
            foreach (var item in tags) {
                yield return new TagIdentifier(item, item);
            }
        } 


        /// <summary>
        /// Reads annotations.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching annotations.
        /// </returns>
        /// <remarks>
        ///   <paramref name="context"/> and <paramref name="request"/> are guaranteed to be 
        ///   non-<see langword="null"/> and validated.
        /// </remarks>
        protected abstract IAsyncEnumerable<TagValueAnnotationQueryResult> ReadAnnotationsAsync(
            IAdapterCallContext context,
            ReadAnnotationsRequest request, 
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Reads a single annotation.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching annotation.
        /// </returns>
        /// <remarks>
        ///   <paramref name="context"/> and <paramref name="request"/> are guaranteed to be 
        ///   non-<see langword="null"/> and validated.
        /// </remarks>
        protected abstract Task<TagValueAnnotationExtended?> ReadAnnotationAsync(
            IAdapterCallContext context,
            ReadAnnotationRequest request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Creates an annotation.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        /// <remarks>
        ///   <paramref name="context"/> and <paramref name="request"/> are guaranteed to be 
        ///   non-<see langword="null"/> and validated.
        /// </remarks>
        protected abstract Task<WriteTagValueAnnotationResult> CreateAnnotationAsync(
            IAdapterCallContext context,
            CreateAnnotationRequest request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Updates an annotation.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        /// <remarks>
        ///   <paramref name="context"/> and <paramref name="request"/> are guaranteed to be 
        ///   non-<see langword="null"/> and validated.
        /// </remarks>
        protected abstract Task<WriteTagValueAnnotationResult> UpdateAnnotationAsync(
            IAdapterCallContext context,
            UpdateAnnotationRequest request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Deletes an annotation.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation result.
        /// </returns>
        /// <remarks>
        ///   <paramref name="context"/> and <paramref name="request"/> are guaranteed to be 
        ///   non-<see langword="null"/> and validated.
        /// </remarks>
        protected abstract Task<WriteTagValueAnnotationResult> DeleteAnnotationAsync(
            IAdapterCallContext context,
            DeleteAnnotationRequest request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Creates a delegate compatible with <see cref="TagValueAnnotationManagerOptions.TagResolver"/> using an 
        /// <see cref="ITagInfo"/> feature.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="ITagInfo"/> feature to use.
        /// </param>
        /// <returns>
        ///   A new delegate.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        public static TagResolver CreateTagResolverFromFeature(ITagInfo feature) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return SnapshotTagValuePushBase.CreateTagResolverFromFeature(feature);
        }


        /// <summary>
        /// Creates a delegate compatible with <see cref="TagValueAnnotationManagerOptions.TagResolver"/> using an 
        /// <see cref="IAdapter"/> that implements the <see cref="ITagInfo"/> feature.
        /// </summary>
        /// <param name="adapter">
        ///   The <see cref="IAdapter"/> to use to resolve tags.
        /// </param>
        /// <returns>
        ///   A new delegate.
        /// </returns>
        /// <remarks>
        ///   The adapter's <see cref="ITagInfo"/> feature will be resolved every time the resulting 
        ///   delegate is invoked. If the feature cannot be resolved, the delegate will return an 
        ///   empty <see cref="IAsyncEnumerable{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static TagResolver CreateTagResolverFromAdapter(IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }

            return SnapshotTagValuePushBase.CreateTagResolverFromAdapter(adapter);
        }

    }
}
