using System.Runtime.CompilerServices;

using DataCore.Adapter;
using DataCore.Adapter.RealTimeData;

namespace ExampleHostedAdapter {

    // This file implements the IReadSnapshotTagValues adapter feature, which allows tags to be
    // polled for their current value.
    //
    // See https://github.com/intelligentplant/AppStoreConnect.Adapters for more details.

    partial class ExampleHostedAdapter : IReadSnapshotTagValues {

        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
            IAdapterCallContext context, 
            ReadSnapshotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            // Always call ValidateInvocation in an adapter feature method to ensure that the call
            // context and request object(s) are valid!
            ValidateInvocation(context, request);

            await Task.Yield();
            var now = DateTime.UtcNow;

            Random GetRng(string tagId) {
                return new Random((tagId.GetHashCode() + now.GetHashCode() + Options.Seed).GetHashCode());
            }

            using (var ctSource = CreateCancellationTokenSource(cancellationToken)) {
                foreach (var tag in request.Tags) {
                    if (ctSource.IsCancellationRequested) {
                        break;
                    }

                    var tagDef = await _tagManager.GetTagAsync(tag, ctSource.Token).ConfigureAwait(false);
                    if (tagDef == null) {
                        continue;
                    }

                    var rnd = GetRng(tagDef.Id);

                    var value = new TagValueBuilder().WithUtcSampleTime(now).WithValue(rnd.NextDouble() * 100).Build();
                    yield return new TagValueQueryResult(tagDef.Id, tagDef.Name, value);
                }
            }
        }

    }
}
