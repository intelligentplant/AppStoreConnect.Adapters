using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes a service for resolving adapters at runtime.
    /// </summary>
    public interface IAdapterAccessor {

        /// <summary>
        /// Gets the available adapters.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapters available to the caller.
        /// </returns>
        Task<IEnumerable<IAdapter>> GetAdapters(IAdapterCallContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the specified adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The requested adapter.
        /// </returns>
        Task<IAdapter> GetAdapter(IAdapterCallContext context, string adapterId, CancellationToken cancellationToken);

        /// <summary>
        /// Resolves the specified adapter and feature, and verifies if the caller is authorized to access the feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The adapter feature.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ResolvedAdapterFeature{TFeature}"/> describing the adapter, feature, and 
        ///   authorization result.
        /// </returns>
        Task<ResolvedAdapterFeature<TFeature>> GetAdapterAndFeature<TFeature>(IAdapterCallContext context, string adapterId, CancellationToken cancellationToken) where TFeature : IAdapterFeature;

    }


    /// <summary>
    /// Describes the result of a request to resolve and authorize an adapter feature.
    /// </summary>
    /// <typeparam name="TFeature">
    ///   The adapter feature type.
    /// </typeparam>
    public struct ResolvedAdapterFeature<TFeature> where TFeature : IAdapterFeature {

        /// <summary>
        /// The resolved adapter.
        /// </summary>
        private IAdapter _adapter;

        /// <summary>
        /// The resolved feature.
        /// </summary>
        private TFeature _feature;

        /// <summary>
        /// A flag indicating if access to the feature is authorized.
        /// </summary>
        private bool _isAuthorized;

        /// <summary>
        /// The adapter. The value will be <see langword="null"/> if the adapter could not be resolved.
        /// </summary>
        public IAdapter Adapter {  get { return _adapter; } }

        /// <summary>
        /// The feature. The value will be <see langword="null"/> if the adapter or feature could not 
        /// be resolved.
        /// </summary>
        public TFeature Feature { get { return IsFeatureResolved ? _feature : default; } }

        /// <summary>
        /// <see langword="true"/> if the adapter was resolved, or <see langword="false"/> otherwise.
        /// </summary>
        public bool IsAdapterResolved { get { return _adapter != null; } }

        /// <summary>
        /// <see langword="true"/> if the feature was resolved, or <see langword="false"/> otherwise.
        /// </summary>
        public bool IsFeatureResolved { get { return _feature != null; } }

        /// <summary>
        /// <see langword="true"/> if access to the feature was authorized, or <see langword="false"/> 
        /// otherwise.
        /// </summary>
        public bool IsAuthorized { get { return _isAuthorized; } }


        /// <summary>
        /// Creates a new <see cref="ResolvedAdapterFeature{TFeature}"/> object.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="feature">
        ///   The feature.
        /// </param>
        /// <param name="isAuthorized">
        ///   A flag indicating if access to the feature was authorized.
        /// </param>
        public ResolvedAdapterFeature(IAdapter adapter, TFeature feature, bool isAuthorized) {
            _adapter = adapter;
            _feature = feature;
            _isAuthorized = isAuthorized;
        }

    }
}
