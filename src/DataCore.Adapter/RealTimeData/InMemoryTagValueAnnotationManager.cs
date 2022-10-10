using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// <see cref="TagValueAnnotationManagerBase"/> implementations that stores annotation 
    /// in-memory.
    /// </summary>
    public sealed class InMemoryTagValueAnnotationManager : TagValueAnnotationManagerBase {

        /// <summary>
        /// Annotations indexed by tag.
        /// </summary>
        private readonly Dictionary<TagIdentifier, List<TagValueAnnotationExtended>> _annotations = new Dictionary<TagIdentifier, List<TagValueAnnotationExtended>>();

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
                    if (!_annotations.TryGetValue(tag, out var annotations)) {
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

                if (!_annotations.TryGetValue(enumerator.Current, out var annotations)) {
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

                if (!_annotations.TryGetValue(enumerator.Current, out var annotations)) {
                    annotations = new List<TagValueAnnotationExtended>();
                    _annotations[enumerator.Current] = annotations;
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

                if (!_annotations.TryGetValue(enumerator.Current, out var annotations)) {
                    annotations = new List<TagValueAnnotationExtended>();
                    _annotations[enumerator.Current] = annotations;
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

                if (!_annotations.TryGetValue(enumerator.Current, out var annotations)) {
                    annotations = new List<TagValueAnnotationExtended>();
                    _annotations[enumerator.Current] = annotations;
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

    }

}
