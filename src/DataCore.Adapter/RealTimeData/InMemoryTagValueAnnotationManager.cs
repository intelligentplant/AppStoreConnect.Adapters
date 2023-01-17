using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Json.Schema;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// <see cref="TagValueAnnotationManagerBase"/> implementations that stores annotation 
    /// in-memory.
    /// </summary>
    public sealed class InMemoryTagValueAnnotationManager : TagValueAnnotationManagerBase {

        /// <summary>
        /// Annotations indexed by tag ID.
        /// </summary>
        private readonly Dictionary<string, List<TagValueAnnotationExtended>> _annotations = new Dictionary<string, List<TagValueAnnotationExtended>>(StringComparer.Ordinal);

        /// <summary>
        /// Lock for accessing <see cref="_annotations"/>.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _annotationsLock = new Nito.AsyncEx.AsyncReaderWriterLock();


        /// <summary>
        /// Creates a new <see cref="InMemoryTagValueAnnotationManager"/> instance.
        /// </summary>
        /// <param name="options">
        ///   The options to use.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use.
        /// </param>
        public InMemoryTagValueAnnotationManager(TagValueAnnotationManagerOptions? options, IBackgroundTaskService? backgroundTaskService)
            : base(options, backgroundTaskService) { }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<TagValueAnnotationQueryResult> ReadAnnotationsAsync(
            IAdapterCallContext context, 
            ReadAnnotationsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            using (await _annotationsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                await foreach (var tag in ResolveTagsAsync(context, request.Tags, cancellationToken).ConfigureAwait(false)) {
                    if (!_annotations.TryGetValue(tag.Id, out var annotations)) {
                        continue;
                    }

                    var remaining = request.AnnotationCount;
                    if (remaining < 1) {
                        remaining = 50;
                    }

                    foreach (var item in annotations.Where(x => x.UtcStartTime >= request.UtcStartTime || (x.AnnotationType == AnnotationType.TimeRange && x.UtcStartTime < request.UtcStartTime && (x.UtcEndTime == null || x.UtcEndTime >= request.UtcStartTime)))) {
                        yield return new TagValueAnnotationQueryResult(tag.Id, tag.Name, item);
                        --remaining;
                        if (remaining < 1) {
                            break;
                        }
                    }
                }
            }
        }


        /// <inheritdoc/>
        protected override async Task<TagValueAnnotationExtended?> ReadAnnotationAsync(
            IAdapterCallContext context,
            ReadAnnotationRequest request,
            CancellationToken cancellationToken
        ) {
            using (await _annotationsLock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                var enumerator = ResolveTagsAsync(context, new[] { request.Tag }, cancellationToken).ConfigureAwait(false).GetAsyncEnumerator();
                if (!(await enumerator.MoveNextAsync()) || enumerator.Current == null) {
                    return null;
                }

                if (!_annotations.TryGetValue(enumerator.Current.Id, out var annotations)) {
                    return null;
                }

                return annotations.FirstOrDefault(x => string.Equals(x.Id, request.AnnotationId, StringComparison.OrdinalIgnoreCase));
            }
        }


        /// <inheritdoc/>
        protected override async Task<WriteTagValueAnnotationResult> CreateAnnotationAsync(
            IAdapterCallContext context,
            CreateAnnotationRequest request,
            CancellationToken cancellationToken
        ) {
            using (await _annotationsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                var enumerator = ResolveTagsAsync(context, new[] { request.Tag }, cancellationToken).ConfigureAwait(false).GetAsyncEnumerator();
                if (!(await enumerator.MoveNextAsync()) || enumerator.Current == null) {
                    throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentUICulture, SharedResources.Error_UnableToResolveNameOrId, request.Tag), nameof(request));
                }

                if (!_annotations.TryGetValue(enumerator.Current.Id, out var annotations)) {
                    annotations = new List<TagValueAnnotationExtended>();
                    _annotations[enumerator.Current.Id] = annotations;
                }

                var id = Guid.NewGuid().ToString();
                annotations.Add(new TagValueAnnotationBuilder(request.Annotation).WithId(id).Build());
                return new WriteTagValueAnnotationResult(enumerator.Current.Id, id, Common.WriteStatus.Success, null, null);
            }
        }


        /// <inheritdoc/>
        protected override async Task<WriteTagValueAnnotationResult> UpdateAnnotationAsync(
            IAdapterCallContext context, 
            UpdateAnnotationRequest request, 
            CancellationToken cancellationToken
        ) {
            using (await _annotationsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                var enumerator = ResolveTagsAsync(context, new[] { request.Tag }, cancellationToken).ConfigureAwait(false).GetAsyncEnumerator();
                if (!(await enumerator.MoveNextAsync()) || enumerator.Current == null) {
                    throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentUICulture, SharedResources.Error_UnableToResolveNameOrId, request.Tag), nameof(request));
                }

                if (!_annotations.TryGetValue(enumerator.Current.Id, out var annotations)) {
                    annotations = new List<TagValueAnnotationExtended>();
                    _annotations[enumerator.Current.Id] = annotations;
                }

                var annotation = annotations.FirstOrDefault(x => string.Equals(x.Id, request.AnnotationId, StringComparison.OrdinalIgnoreCase));
                if (annotation == null) {
                    annotation = new TagValueAnnotationBuilder(request.Annotation).WithId(Guid.NewGuid().ToString()).Build();
                    annotations.Add(annotation);
                }
                else {
                    annotations.Remove(annotation);
                    annotations.Add(new TagValueAnnotationBuilder(request.Annotation).WithId(annotation.Id).Build());
                }

                return new WriteTagValueAnnotationResult(enumerator.Current.Id, annotation.Id, Common.WriteStatus.Success, null, null);
            }
        }


        /// <inheritdoc/>
        protected override async Task<WriteTagValueAnnotationResult> DeleteAnnotationAsync(IAdapterCallContext context, DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            using (await _annotationsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                var enumerator = ResolveTagsAsync(context, new[] { request.Tag }, cancellationToken).ConfigureAwait(false).GetAsyncEnumerator();
                if (!(await enumerator.MoveNextAsync()) || enumerator.Current == null) {
                    throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentUICulture, SharedResources.Error_UnableToResolveNameOrId, request.Tag), nameof(request));
                }

                if (!_annotations.TryGetValue(enumerator.Current.Id, out var annotations)) {
                    return new WriteTagValueAnnotationResult(enumerator.Current.Id, request.AnnotationId, Common.WriteStatus.Fail, null, null);
                }

                var annotation = annotations.FirstOrDefault(x => string.Equals(x.Id, request.AnnotationId, StringComparison.OrdinalIgnoreCase));
                if (annotation == null) {
                    return new WriteTagValueAnnotationResult(enumerator.Current.Id, request.AnnotationId, Common.WriteStatus.Fail, null, null);
                }
                else {
                    return new WriteTagValueAnnotationResult(enumerator.Current.Id, annotation.Id, Common.WriteStatus.Success, null, null);
                }
            }
        }


        /// <summary>
        /// Creates or updates an annotation.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID for the annotation.
        /// </param>
        /// <param name="annotation">
        ///   The annotation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will create or update the annotation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="annotation"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="annotation"/> is not valid.
        /// </exception>
        public async Task CreateOrUpdateAnnotationAsync(string tagId, TagValueAnnotationExtended annotation, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }
            ValidationExtensions.ValidateObject(annotation);

            using (await _annotationsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_annotations.TryGetValue(tagId, out var annotations)) {
                    annotations = new List<TagValueAnnotationExtended>();
                    _annotations[tagId] = annotations;
                }

                var existing = annotations.FirstOrDefault(x => x.Id.Equals(annotation.Id, StringComparison.OrdinalIgnoreCase));
                if (existing != null) {
                    annotations.Remove(existing);
                }
                annotations.Add(new TagValueAnnotationBuilder(annotation).Build());
            }
        }


        /// <summary>
        /// Deletes an annotation.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID for the annotation.
        /// </param>
        /// <param name="annotationId">
        ///   The annotation ID for the annotation.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return <see langword="true"/> if the 
        ///   annotation was deleted or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="annotationId"/> is <see langword="null"/>.
        /// </exception>
        public async Task<bool> DeleteAnnotationAsync(string tagId, string annotationId, CancellationToken cancellationToken) {
            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }
            if (annotationId == null) {
                throw new ArgumentNullException(nameof(annotationId));
            }

            using (await _annotationsLock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (!_annotations.TryGetValue(tagId, out var annotations)) {
                    return false;
                }

                var existing = annotations.FirstOrDefault(x => x.Id.Equals(annotationId, StringComparison.OrdinalIgnoreCase));
                if (existing != null) {
                    annotations.Remove(existing);
                    return true;
                }
                return false;
            }
        }

    }

}
