using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {
    partial class AdapterCore : IAdapterFeaturesCollection {

        /// <summary>
        /// The feature lookup for the adapter.
        /// </summary>
        private readonly ConcurrentDictionary<Uri, IAdapterFeature> _featureLookup = new ConcurrentDictionary<Uri, IAdapterFeature>();

        /// <inheritdoc/>
        IEnumerable<Uri> IAdapterFeaturesCollection.Keys { 
            get {
                CheckDisposed();
                return _featureLookup.Keys;
            }
        }

        /// <inheritdoc/>
        IAdapterFeature? IAdapterFeaturesCollection.this[Uri key] {
            get {
                CheckDisposed();
                return _featureLookup.TryGetValue(key ?? throw new ArgumentNullException(nameof(key)), out var feature) 
                    ? feature 
                    : null;
            }
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <param name="featureType">
        ///   The feature interface type.
        /// </param>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <param name="throwOnAlreadyAdded">
        ///   Flags if an exception should be thrown if the feature type has already been registered.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="feature"/> is not an instance of <paramref name="featureType"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <paramref name="featureType"/> has already been registered and 
        ///   <paramref name="throwOnAlreadyAdded"/> is <see langword="true"/>.
        /// </exception>
        private void AddFeatureCore(Type featureType, IAdapterFeature feature, bool throwOnAlreadyAdded) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (!featureType.IsInstanceOfType(feature)) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_NotAFeatureImplementation, featureType.FullName), nameof(feature));
            }

            var uri = featureType.GetAdapterFeatureUri();

            AdapterFeatureWrapper? wrapper = null;

            if (feature is AdapterFeatureWrapper afw) {
                wrapper = afw;
            }
            else if (featureType.Equals(typeof(AssetModel.IAssetModelBrowse))) {
                wrapper = new AssetModel.AssetModelBrowseWrapper(this, (AssetModel.IAssetModelBrowse) feature);
            }
            else if (featureType.Equals(typeof(AssetModel.IAssetModelSearch))) {
                wrapper = new AssetModel.AssetModelSearchWrapper(this, (AssetModel.IAssetModelSearch) feature);
            }
            else if (featureType.Equals(typeof(Diagnostics.IConfigurationChanges))) {
                wrapper = new Diagnostics.ConfigurationChangesWrapper(this, (Diagnostics.IConfigurationChanges) feature);
            }
            else if (featureType.Equals(typeof(Diagnostics.IHealthCheck))) {
                wrapper = new Diagnostics.HealthCheckWrapper(this, (Diagnostics.IHealthCheck) feature);
            }
            else if (featureType.Equals(typeof(Events.IEventMessagePush))) {
                wrapper = new Events.EventMessagePushWrapper(this, (Events.IEventMessagePush) feature);
            }
            else if (featureType.Equals(typeof(Events.IEventMessagePushWithTopics))) {
                wrapper = new Events.EventMessagePushWithTopicsWrapper(this, (Events.IEventMessagePushWithTopics) feature);
            }
            else if (featureType.Equals(typeof(Events.IReadEventMessagesForTimeRange))) {
                wrapper = new Events.ReadEventMessagesForTimeRangeWrapper(this, (Events.IReadEventMessagesForTimeRange) feature);
            }
            else if (featureType.Equals(typeof(Events.IReadEventMessagesUsingCursor))) {
                wrapper = new Events.ReadEventMessagesUsingCursorWrapper(this, (Events.IReadEventMessagesUsingCursor) feature);
            }
            else if (featureType.Equals(typeof(Events.IWriteEventMessages))) {
                wrapper = new Events.WriteEventMessagesWrapper(this, (Events.IWriteEventMessages) feature);
            }
            else if (featureType.Equals(typeof(Extensions.ICustomFunctions))) {
                wrapper = new Extensions.CustomFunctionsWrapper(this, (Extensions.ICustomFunctions) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IReadPlotTagValues))) {
                wrapper = new RealTimeData.ReadPlotTagValuesWrapper(this, (RealTimeData.IReadPlotTagValues) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IReadProcessedTagValues))) {
                wrapper = new RealTimeData.ReadProcessedTagValuesWrapper(this, (RealTimeData.IReadProcessedTagValues) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IReadRawTagValues))) {
                wrapper = new RealTimeData.ReadRawTagValuesWrapper(this, (RealTimeData.IReadRawTagValues) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IReadSnapshotTagValues))) {
                wrapper = new RealTimeData.ReadSnapshotTagValuesWrapper(this, (RealTimeData.IReadSnapshotTagValues) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IReadTagValueAnnotations))) {
                wrapper = new RealTimeData.ReadTagValueAnnotationsWrapper(this, (RealTimeData.IReadTagValueAnnotations) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IReadTagValuesAtTimes))) {
                wrapper = new RealTimeData.ReadTagValuesAtTimesWrapper(this, (RealTimeData.IReadTagValuesAtTimes) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.ISnapshotTagValuePush))) {
                wrapper = new RealTimeData.SnapshotTagValuePushWrapper(this, (RealTimeData.ISnapshotTagValuePush) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IWriteHistoricalTagValues))) {
                wrapper = new RealTimeData.WriteHistoricalTagValuesWrapper(this, (RealTimeData.IWriteHistoricalTagValues) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IWriteSnapshotTagValues))) {
                wrapper = new RealTimeData.WriteSnapshotTagValuesWrapper(this, (RealTimeData.IWriteSnapshotTagValues) feature);
            }
            else if (featureType.Equals(typeof(RealTimeData.IWriteTagValueAnnotations))) {
                wrapper = new RealTimeData.WriteTagValueAnnotationsWrapper(this, (RealTimeData.IWriteTagValueAnnotations) feature);
            }
            else if (featureType.Equals(typeof(Tags.ITagConfiguration))) {
                wrapper = new Tags.TagConfigurationWrapper(this, (Tags.ITagConfiguration) feature);
            }
            else if (featureType.Equals(typeof(Tags.ITagInfo))) {
                wrapper = new Tags.TagInfoWrapper(this, (Tags.ITagInfo) feature);
            }
            else if (featureType.Equals(typeof(Tags.ITagSearch))) {
                wrapper = new Tags.TagSearchWrapper(this, (Tags.ITagSearch) feature);

            }

            if (wrapper == null) {
                Logger.LogWarning(AbstractionsResources.Log_UnableToCreateFeatureWrapper, featureType.FullName);
            }

            if (!_featureLookup.TryAdd(uri!, wrapper ?? feature)) {
                if (throwOnAlreadyAdded) {
                    throw new ArgumentException(AbstractionsResources.Error_FeatureIsAlreadyRegistered, nameof(featureType));
                }
            }
        }


        /// <summary>
        /// Adds a feature to the adapter.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature. This must be an interface derived from <see cref="IAdapterFeature"/>.
        /// </typeparam>
        /// <param name="feature">
        ///   The implementation object.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not an interface, or it does not interit from 
        ///   <see cref="IAdapterFeature"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <typeparamref name="TFeature"/> has already been registered.
        /// </exception>
        public void AddFeature<TFeature>(TFeature feature) where TFeature : IAdapterFeature {
            CheckDisposed();
            if (!typeof(TFeature).IsAdapterFeature()) {
#pragma warning disable CS0618 // Type or member is obsolete
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    AbstractionsResources.Error_NotAnAdapterFeature,
                    typeof(TFeature).FullName,
                    nameof(IAdapterFeature),
                    nameof(AdapterFeatureAttribute),
                    nameof(IAdapterExtensionFeature),
                    nameof(ExtensionFeatureAttribute)
                ), nameof(feature));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            AddFeatureCore(typeof(TFeature), feature, true);
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <param name="featureType">
        ///   The feature type. This must be an interface derived from <see cref="IAdapterFeature"/>.
        /// </param>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="featureType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="featureType"/> is not an adapter feature type.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="feature"/> is not an instance of <paramref name="featureType"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <paramref name="featureType"/> has already been registered.
        /// </exception>
        public void AddFeature(Type featureType, IAdapterFeature feature) {
            CheckDisposed();

            if (featureType == null) {
                throw new ArgumentNullException(nameof(featureType));
            }
            if (!featureType.IsAdapterFeature()) {
#pragma warning disable CS0618 // Type or member is obsolete
                throw new ArgumentException(string.Format(
                    CultureInfo.CurrentCulture,
                    AbstractionsResources.Error_NotAnAdapterFeature,
                    featureType.FullName,
                    nameof(IAdapterFeature),
                    nameof(AdapterFeatureAttribute),
                    nameof(IAdapterExtensionFeature),
                    nameof(ExtensionFeatureAttribute)
                ), nameof(featureType));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            AddFeatureCore(featureType, feature, true);
        }


        /// <summary>
        /// Adds all adapter features implemented by the specified feature provider.
        /// </summary>
        /// <param name="provider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        /// <param name="addStandardFeatures">
        ///   Specifies if standard adapter feature implementations should be added to the 
        ///   collection. Standard feature types can be obtained by calling 
        ///   <see cref="TypeExtensions.GetStandardAdapterFeatureTypes"/>.
        /// </param>
        /// <param name="addExtensionFeatures">
        ///   Specifies if extension adapter feature implementations should be added to the 
        ///   collection. Extension features must derive from <see cref="IAdapterExtensionFeature"/> 
        ///   and must match the criteria specified in <see cref="TypeExtensions.IsExtensionAdapterFeature"/>.
        /// </param>
        /// <remarks>
        ///   All interfaces implemented by the <paramref name="provider"/> that extend 
        ///   <see cref="IAdapterFeature"/> will be registered with the <see cref="Adapter"/> 
        ///   (assuming that they meet the <paramref name="addStandardFeatures"/> and 
        ///   <paramref name="addExtensionFeatures"/> constraints).
        /// </remarks>
        public void AddFeatures(object provider, bool addStandardFeatures = true, bool addExtensionFeatures = true) {
            CheckDisposed();

            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }

            var type = provider.GetType();

            var implementedFeatures = type.GetInterfaces().Where(x => x.IsAdapterFeature());
            foreach (var feature in implementedFeatures) {
                if (!addStandardFeatures && feature.IsStandardAdapterFeature()) {
                    continue;
                }
                if (!addExtensionFeatures && feature.IsExtensionAdapterFeature()) {
                    continue;
                }
                AddFeatureCore(feature, (IAdapterFeature) provider, false);
            }

            if (addExtensionFeatures && type.IsConcreteExtensionAdapterFeature()) {
                AddFeatureCore(type, (IAdapterFeature) provider, false);
            }
        }


        /// <summary>
        /// Adds all standard adapter features implemented by the specified feature provider. 
        /// Standard feature types can be obtained by calling <see cref="TypeExtensions.GetStandardAdapterFeatureTypes"/>
        /// </summary>
        /// <param name="provider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        public void AddStandardFeatures(object provider) {
            AddFeatures(provider, true, false);
        }


        /// <summary>
        /// Adds all extension adapter features implemented by the specified feature provider. See 
        /// the remarks for details on how extension feature types are identified.
        /// </summary>
        /// <param name="provider"></param>
        /// <remarks>
        /// 
        /// <para>
        ///   Extension feature implementations supplied by the <paramref name="provider"/> are 
        ///   identified in one of two ways:
        /// </para>
        /// 
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       If the <paramref name="provider"/> implements <see cref="IAdapterExtensionFeature"/> 
        ///       and is directly annotated with <see cref="ExtensionFeatureAttribute"/>, the 
        ///       <paramref name="provider"/> will be directly registered using its own type as the 
        ///       index in the adapter's features dictionary.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       If the <paramref name="provider"/> implements any interfaces that extend 
        ///       <see cref="IAdapterExtensionFeature"/> that are annotated with 
        ///       <see cref="ExtensionFeatureAttribute"/>, the <paramref name="provider"/> 
        ///       will be registered using each of the implemented extension feature interfaces.
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        public void AddExtensionFeatures(object provider) {
            AddFeatures(provider, false, true);
        }


        /// <summary>
        /// Removes an adapter feature.
        /// </summary>
        /// <param name="uri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was removed, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uri"/> is <see langword="null"/>.
        /// </exception>
        public bool RemoveFeature(Uri uri) {
            if (uri == null) {
                throw new ArgumentNullException(nameof(uri));
            }
            return _featureLookup.TryRemove(uri, out var _);
        }


        /// <summary>
        /// Removes an adapter feature.
        /// </summary>
        /// <param name="uriString">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the feature was removed, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        public bool RemoveFeature(string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            return _featureLookup.TryRemove(uri, out var _);
        }


        /// <summary>
        /// Removes an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type to remove.
        /// </typeparam>
        /// <returns>
        ///   <see langword="true"/> if the feature was removed, or <see langword="false"/>
        ///   otherwise.
        /// </returns>
        public bool RemoveFeature<TFeature>() where TFeature : IAdapterFeature {
            CheckDisposed();
            var uri = typeof(TFeature).GetAdapterFeatureUri();
            if (uri == null) {
                return false;
            }
            return _featureLookup.TryRemove(uri, out var _);
        }


        /// <summary>
        /// Removes all adapter features.
        /// </summary>
        public void RemoveAllFeatures() {
            CheckDisposed();
            _featureLookup.Clear();
        }


        /// <summary>
        /// Disposes of the adapter features that that implement <see cref="IDisposable"/> or
        /// <see cref="IAsyncDisposable"/>.
        /// </summary>
        /// <remarks>
        ///   Any features that implement <see cref="IAsyncDisposable"/> but do not also implement 
        ///   <see cref="IDisposable"/> will be disposed in a background task.
        /// </remarks>
        private void DisposeFeatures() {
            var features = _featureLookup.Values.ToArray();
            _featureLookup.Clear();

            var processedItems = new HashSet<object>();
            var asyncDisposableItems = new List<IAsyncDisposable>();

            foreach (var item in features) {
                if (item == null) {
                    continue;
                }

                var unwrapped = item.Unwrap();
                
                if (unwrapped == null || ReferenceEquals(unwrapped, this)) {
                    continue;
                }

                if (!processedItems.Add(unwrapped)) {
                    // Item has already been dealt with (e.g. if it implements multiple features
                    // and has already been disposed using one of the other feature IDs).
                    continue;
                }

                try {
                    var ad = DisposeFeature(unwrapped);
                    if (ad != null) {
                        asyncDisposableItems.Add(ad);
                    }
                }
                catch (Exception e) {
                    Logger.LogError(e, AbstractionsResources.Log_ErrorWhileDisposingOfFeature, unwrapped);
                }
            }

            if (asyncDisposableItems.Count > 0) {
                // Dispose of IAsyncDisposable items in a background task.
                _ = Task.Run(async () => {
                    foreach (var item in asyncDisposableItems) {
                        try {
                            await item.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception e) {
                            Logger.LogError(e, AbstractionsResources.Log_ErrorWhileDisposingOfFeature, item);
                        }
                    }
                });
            }
        }


        /// <summary>
        /// Asynchronously disposes of the adapter features that that implement <see cref="IDisposable"/> 
        /// or <see cref="IAsyncDisposable"/>.
        /// </summary>
        private async ValueTask DisposeFeaturesAsync() {
            var features = _featureLookup.Values.ToArray();
            _featureLookup.Clear();

            var processedItems = new HashSet<object>();

            foreach (var item in features) {
                if (item == null) {
                    continue;
                }
                
                var unwrapped = item.Unwrap();

                if (unwrapped == null || ReferenceEquals(unwrapped, this)) {
                    continue;
                }

                if (!processedItems.Add(unwrapped)) {
                    // Item has already been dealt with (e.g. if it implements multiple features
                    // and has already been disposed using one of the other feature IDs).
                    continue;
                }

                try {
                    await DisposeFeatureAsync(unwrapped).ConfigureAwait(false);
                }
                catch (Exception e) {
                    Logger.LogError(e, AbstractionsResources.Log_ErrorWhileDisposingOfFeature, unwrapped);
                }
            }
        }


        private IAsyncDisposable? DisposeFeature(IAdapterFeature item) {
            // Prefer synchronous dispose over asynchronous.
            if (item is IDisposable d) {
                d.Dispose();
                return null;
            }

            if (item is IAsyncDisposable ad) {
                return ad;
            }

            return null;
        }


        private async ValueTask DisposeFeatureAsync(IAdapterFeature item) {
            // Prefer asynchronous dispose over synchronous.
            if (item is IAsyncDisposable ad) {
                await ad.DisposeAsync().ConfigureAwait(false);
            }
            else if (item is IDisposable d) {
                d.Dispose();
            }
        }

    }
}
