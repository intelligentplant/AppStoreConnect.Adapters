using Microsoft.AspNetCore.Http;

namespace DataCore.Adapter.AspNetCore.Internal {
    internal readonly record struct ResolvedAdapter<TFeature> where TFeature : IAdapterFeature {

        public required IResult? Error { get; init; }

        public required IAdapterCallContext CallContext { get; init; }

        public required IAdapter Adapter { get; init; }

        public required TFeature Feature { get; init; }

    }


    internal readonly record struct ResolvedAdapter {

        public required IResult? Error { get; init; }

        public required IAdapterCallContext CallContext { get; init; }

        public required IAdapter Adapter { get; init; }

    }
}
