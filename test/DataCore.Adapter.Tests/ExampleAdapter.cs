﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.DependencyInjection;

namespace DataCore.Adapter.Tests {
    public class ExampleAdapter : IAdapter, ITagInfo, IReadSnapshotTagValues {

        private CancellationTokenSource _stopTokenSource;

        public IBackgroundTaskService BackgroundTaskService { get; }

        public AdapterDescriptor Descriptor { get; }

        public AdapterTypeDescriptor TypeDescriptor { get; }

        public IAdapterFeaturesCollection Features { get; }

        public IEnumerable<AdapterProperty> Properties { get; } = Array.Empty<AdapterProperty>();

        public bool IsEnabled { get; set; } = true;

        public bool IsRunning { get; } = true;

        private readonly SnapshotSubscriptionManager _snapshotSubscriptionManager;

        private readonly EventSubscriptionManager _eventSubscriptionManager;

        private readonly EventTopicSubscriptionManager _eventTopicSubscriptionManager;


        public ExampleAdapter() {
            BackgroundTaskService = new BackgroundTaskServiceWrapper(
                IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default,
                () => _stopTokenSource?.Token ?? default
            );
            Descriptor = AdapterDescriptor.Create("unit-tests", "Unit Tests Adapter", "Adapter for use in unit tests");
            TypeDescriptor = this.CreateTypeDescriptor();
            var features = new AdapterFeaturesCollection();
            _snapshotSubscriptionManager = new SnapshotSubscriptionManager(this);
            _eventSubscriptionManager = new EventSubscriptionManager();
            _eventTopicSubscriptionManager = new EventTopicSubscriptionManager();
            features.AddFromProvider(this);
            features.Add<ISnapshotTagValuePush, SnapshotSubscriptionManager>(_snapshotSubscriptionManager);
            features.Add<IEventMessagePush, EventSubscriptionManager>(_eventSubscriptionManager);
            features.Add<IEventMessagePushWithTopics, EventTopicSubscriptionManager>(_eventTopicSubscriptionManager);
            features.AddFromProvider(new PingPongExtension(BackgroundTaskService, AssemblyInitializer.ApplicationServices.GetServices<IObjectEncoder>()));
            Features = features;
        }


        public Task StartAsync(CancellationToken cancellationToken = default) {
            _stopTokenSource = new CancellationTokenSource();
            return Task.CompletedTask;
        }


        public Task StopAsync(CancellationToken cancellationToken = default) {
            _stopTokenSource?.Cancel();
            _stopTokenSource?.Dispose();
            return Task.CompletedTask;
        }


        public IAsyncEnumerable<AdapterProperty>GetTagProperties(IAdapterCallContext context, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);

            return Array.Empty<AdapterProperty>().ToAsyncEnumerable(cancellationToken);
        }


        public async IAsyncEnumerable<TagDefinition> GetTags(
            IAdapterCallContext context, 
            GetTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            Validator.ValidateObject(request, new ValidationContext(request), true);
            await Task.CompletedTask.ConfigureAwait(false);

            foreach (var item in request.Tags) {
                yield return new TagDefinition(item, item, null, null, VariantType.Double, null, null, null, null);
            }
        }


        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
            IAdapterCallContext context, 
            ReadSnapshotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            await Task.CompletedTask.ConfigureAwait(false);
            foreach (var tag in request.Tags) {
                yield return new TagValueQueryResult(
                    tag,
                    tag,
                    new TagValueBuilder()
                        .WithUtcSampleTime(DateTime.MinValue)
                        .WithValue(0)
                        .Build()
                );
            }
        }


        public void AddFeatures(object provider) {
            ((AdapterFeaturesCollection) Features).AddFromProvider(provider);
        }


        public void Dispose() {
            _snapshotSubscriptionManager.Dispose();
        }


        public ValueTask DisposeAsync() {
            Dispose();
            return default;
        }


        public ValueTask<bool> WriteSnapshotValue(TagValueQueryResult value) {
            return _snapshotSubscriptionManager.ValueReceived(value);
        }


        public async ValueTask<bool> WriteTestEventMessage(EventMessage msg) {
            return await _eventSubscriptionManager.ValueReceived(msg).ConfigureAwait(false) && await _eventTopicSubscriptionManager.ValueReceived(msg).ConfigureAwait(false);
        }


        private class SnapshotSubscriptionManager : SnapshotTagValuePush {


            public SnapshotSubscriptionManager(ITagInfo tagInfo) : base(new SnapshotTagValuePushOptions() {
                TagResolver = CreateTagResolverFromFeature(tagInfo)
            }, null, null) { }


            protected override async Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
                await base.OnTagsAdded(tags, cancellationToken).ConfigureAwait(false);
                foreach (var tag in tags) {
                    ValueReceived(new TagValueQueryResult(
                        tag.Id,
                        tag.Name,
                        new TagValueBuilder()
                            .WithUtcSampleTime(DateTime.MinValue)
                            .WithValue(0)
                            .Build()
                    )).GetAwaiter().GetResult();
                }
            }

        }


        private class AdapterFeaturesCollection : IAdapterFeaturesCollection {

            private readonly ConcurrentDictionary<Uri, IAdapterFeature> _features = new ConcurrentDictionary<Uri, IAdapterFeature>();


            /// <inheritdoc/>
            public IEnumerable<Uri> Keys {
                get { return _features.Keys; }
            }



            /// <inheritdoc/>
            public IAdapterFeature this[Uri key] {
                get {
                    return key == null || !_features.TryGetValue(key, out var value)
                        ? default!
                        : value;
                }
            }


            private void AddInternal(Type type, IAdapterFeature implementation) {
                var uri = type.GetAdapterFeatureUri();
                _features[uri!] = implementation;
            }


            public void Add<TFeature, TFeatureImpl>(TFeatureImpl feature) where TFeature : IAdapterFeature where TFeatureImpl : class, TFeature {
                AddInternal(typeof(TFeature), feature);
            }


            public void AddFromProvider(object featureProvider, bool addStandardFeatures = true, bool addExtensionFeatures = true) {
                if (featureProvider == null) {
                    return;
                }

                var type = featureProvider.GetType();

                var implementedFeatures = type.GetInterfaces().Where(x => x.IsAdapterFeature());
                foreach (var feature in implementedFeatures) {
                    if (!addStandardFeatures && feature.IsStandardAdapterFeature()) {
                        continue;
                    }
                    if (!addExtensionFeatures && feature.IsExtensionAdapterFeature()) {
                        continue;
                    }
                    AddInternal(feature, (IAdapterFeature) featureProvider);
                }

                if (addExtensionFeatures && type.IsConcreteExtensionAdapterFeature()) {
                    AddInternal(type, (IAdapterFeature) featureProvider);
                }
            }

        }


        private class EventSubscriptionManager : EventMessagePush {

            public EventSubscriptionManager() : base(null, null, null) { }

        }


        private class EventTopicSubscriptionManager : EventMessagePushWithTopics {

            public EventTopicSubscriptionManager() : base(null, null, null) { }

        }

    }
}
