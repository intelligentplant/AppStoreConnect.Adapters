using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    /// <summary>
    /// Base class that defines basic tests for adapter features.
    /// </summary>
    /// <typeparam name="TAdapter">
    ///   The adapter type for the tests.
    /// </typeparam>
    public abstract class AdapterTestsBase<TAdapter> : TestsBase where TAdapter : class, IAdapter {

        /// <summary>
        /// Creates an <see cref="IServiceScope"/> for the current test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   A new <see cref="IServiceScope"/> instance.
        /// </returns>
        protected abstract IServiceScope CreateServiceScope(TestContext context);


        /// <summary>
        /// Creates a <typeparamref name="TAdapter"/> instance for use the the current test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <param name="serviceProvider">
        ///   The service provider for the test.
        /// </param>
        /// <returns>
        ///   A new <typeparamref name="TAdapter"/> instance.
        /// </returns>
        protected abstract TAdapter CreateAdapter(TestContext context, IServiceProvider serviceProvider);


        /// <summary>
        /// Creates an <see cref="IAdapterCallContext"/> for use in the current test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   A new <see cref="IAdapterCallContext"/> instance.
        /// </returns>
        protected virtual IAdapterCallContext CreateCallContext(TestContext context) {
            var identity = new ClaimsIdentity(GetType().FullName, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.Name, context!.TestName));

            var principal = new ClaimsPrincipal(identity);

            return new DefaultAdapterCallContext(principal);
        }


        /// <summary>
        /// Creates and initialises a <typeparamref name="TAdapter"/> instance and runs an adapter 
        /// test.
        /// </summary>
        /// <param name="callback">
        ///   The callback delegate that will perform the test.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        protected async Task RunAdapterTest(Func<TAdapter, IAdapterCallContext, CancellationToken, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }

            using (var scope = CreateServiceScope(TestContext)) {
                var adapter = CreateAdapter(TestContext, scope.ServiceProvider);
                if (adapter == null) {
                    Assert.Inconclusive("Adapter creation delegate returned null.");
                    return;
                }

                try {
                    await adapter.StartAsync(CancellationToken).ConfigureAwait(false);
                    var context = CreateCallContext(TestContext);
                    await callback(adapter, context, CancellationToken).ConfigureAwait(false);
                }
                finally {
                    if (adapter is IAsyncDisposable iad) {
                        await iad.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (adapter is IDisposable id) {
                        id.Dispose();
                    }
                }
            }
        }


        /// <summary>
        /// Throws an <see cref="AssertInconclusiveException"/> that indicates that a test was 
        /// skipped due to a required feature not being implemented on the adapter.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The missing feature.
        /// </typeparam>
        protected void AssertFeatureNotImplemented<TFeature>() {
            Assert.Inconclusive($"Feature not implemented: {typeof(TFeature).Name}");
        }


        /// <summary>
        /// Throws an <see cref="AssertInconclusiveException"/> that indicates that a test was 
        /// skipped due to a required feature not being implemented on the adapter.
        /// </summary>
        /// <param name="feature">
        ///   The missing feature name.
        /// </param>
        protected void AssertFeatureNotImplemented(string feature) {
            Assert.Inconclusive($"Feature not implemented: {feature}");
        }


        /// <summary>
        /// Throws an <see cref="AssertInconclusiveException"/> that indicates that a test was 
        /// skipped due to a input data generation method returning <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The missing feature.
        /// </typeparam>
        /// <param name="methodName">
        ///   The method that must be overridden.
        /// </param>
        private void AssertInconclusiveDueToMissingTestInput<TFeature>(string methodName) {
            Assert.Fail($"Adapter implements {typeof(TFeature).Name}, but the '{methodName}' method used to generate input data for test '{TestContext.TestName}' returned a value that indicates that it has not been overridden. Override {methodName} in your test class to return the input data to use for this test.");
        }


        /// <summary>
        /// Reads all items from a <see cref="ChannelReader{T}"/> into an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The channel item type.
        /// </typeparam>
        /// <param name="channel">
        ///   The channel reader.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the items that were read from the channel.
        /// </returns>
        private static async Task<IEnumerable<T>> ReadAllAsync<T>(ChannelReader<T> channel, CancellationToken cancellationToken) {
            var result = new List<T>();

            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (channel.TryRead(out var item)) {
                    result.Add(item);
                }
            }

            return result;
        }

        #region [ IAdapter ]

        /// <summary>
        /// Verifies that the adapter's <see cref="IBackgroundTaskServiceProvider.BackgroundTaskService"/> 
        /// property is not <see langword="null"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public Task AdapterBackgroundTaskServiceShouldNotBeNull() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.BackgroundTaskService);
                return Task.CompletedTask;
            });
        }


        /// <summary>
        /// Verifies that the adapter's <see cref="IAdapter.Descriptor"/> property is not 
        /// <see langword="null"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public Task AdapterDescriptorShouldNotBeNull() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.Descriptor);
                return Task.CompletedTask;
            });
        }


        /// <summary>
        /// Verifies that the adapter's <see cref="IAdapter.Features"/> property is not 
        /// <see langword="null"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public Task AdapterFeaturesShouldNotBeNull() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.Features);
                return Task.CompletedTask;
            });
        }


        /// <summary>
        /// Verifies that the adapter's <see cref="IAdapter.Properties"/> property is not 
        /// <see langword="null"/>, and that none of the entries in the collection are 
        /// <see langword="null"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public Task AdapterPropertiesShouldNotBeNull() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.Properties);
                if (adapter.Properties.Any()) {
                    Assert.IsTrue(adapter.Properties.All(x => x != null), $"{nameof(IAdapter)}.{nameof(IAdapter.Properties)} entries should not be null.");
                }
                return Task.CompletedTask;
            });
        }


        /// <summary>
        /// Verifies that the adapter's <see cref="IAdapter.TypeDescriptor"/> property is not 
        /// <see langword="null"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public Task AdapterTypeDescriptorShouldNotBeNull() {
            return RunAdapterTest((adapter, context, ct) => {
                Assert.IsNotNull(adapter.TypeDescriptor);
                return Task.CompletedTask;
            });
        }

        #endregion

        #region [ IHealthCheck ]

        /// <summary>
        /// Verifies that a <see cref="HealthCheckResult.Status"/> property matches the aggregate 
        /// status of its inner results.
        /// </summary>
        /// <param name="health">
        ///   The health check result.
        /// </param>
        private void VerifyHealthCheckResult(HealthCheckResult health) {
            if (health.InnerResults != null && health.InnerResults.Any()) {
                foreach (var item in health.InnerResults) {
                    VerifyHealthCheckResult(item);
                }

                // If there are any inner results, ensure that the overall status matches the 
                // aggregate status of the inner results.
                Assert.AreEqual(health.Status, HealthCheckResult.GetAggregateHealthStatus(health.InnerResults.Select(x => x.Status)));
            }
        }


        /// <summary>
        /// Verifies that <see cref="IHealthCheck.CheckHealthAsync"/> returns a value.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will perform the test.
        /// </returns>
        [TestMethod]
        public virtual Task CheckHealthRequestShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IHealthCheck>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IHealthCheck>();
                    return;
                }

                var health = await feature.CheckHealthAsync(context, ct).ConfigureAwait(false);
                Assert.AreNotEqual(default, health, $"Adapter should not return default({nameof(HealthCheckResult)}) as their health status.");
                VerifyHealthCheckResult(health);
            });
        }


        /// <summary>
        /// Verifies that <see cref="IHealthCheck.Subscribe"/> pushes an initial value when a 
        /// subscription is created.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will perform the test.
        /// </returns>
        [TestMethod]
        public virtual Task HealthCheckSubscriptionShouldReceiveInitialValue() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IHealthCheck>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IHealthCheck>();
                    return;
                }

                var subscription = await feature.Subscribe(context, ct).ConfigureAwait(false);
                Assert.IsNotNull(subscription);

                var health = await subscription.ReadAsync(ct).ConfigureAwait(false);
                VerifyHealthCheckResult(health);
            });
        }

        #endregion

        #region [ ITagInfo ]

        /// <summary>
        /// Gets the request to use with the <see cref="GetTagPropertiesRequestShouldSucceed"/> test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="GetTagsPropertiesRequest"/> to use.
        /// </returns>
        protected virtual GetTagPropertiesRequest CreateGetTagPropertiesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Gets the request to use with the <see cref="GetTagsRequestShouldReturnResults"/> test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="GetTagsRequest"/> to use.
        /// </returns>
        protected virtual GetTagsRequest CreateGetTagsRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="ITagInfo.GetTagProperties"/> returns a non-null channel, that 
        /// the channel completes, and that none of the property definitions returned are 
        /// <see langword="null"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public virtual Task GetTagPropertiesRequestShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<ITagInfo>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ITagInfo>();
                    return;
                }

                var request = CreateGetTagPropertiesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<ITagInfo>(nameof(CreateGetTagPropertiesRequest));
                    return;
                }

                var channel = await feature.GetTagProperties(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);

                var props = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.IsTrue(props.Count() <= request.PageSize, $"Response contained {props.Count()} items, but the request page size was {request.PageSize}.");

                if (props.Any()) {
                    Assert.IsTrue(props.All(x => x != null), "Adapters must not return null tag properties.");
                }
            });
        }


        /// <summary>
        /// Verifies that <see cref="ITagInfo.GetTags"/> returns the expected results.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public virtual Task GetTagsRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<ITagInfo>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ITagInfo>();
                    return;
                }

                var request = CreateGetTagsRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<ITagInfo>(nameof(CreateGetTagsRequest));
                    return;
                }

                var channel = await feature.GetTags(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);

                var tags = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                Assert.AreEqual(request.Tags.Count(), tags.Count());

                var remainingTags = new HashSet<string>(request.Tags);

                foreach (var tag in tags) {
                    Assert.IsNotNull(tag);
                    Assert.IsTrue(remainingTags.Remove(tag.Id) || remainingTags.Remove(tag.Name), $"Expected tags list does not contain ID '{tag.Id}' or name '{tag.Name}'.");
                    if (tag.Properties.Any()) {
                        Assert.IsTrue(tag.Properties.All(x => x != null), $"Tag '{tag.Name}' (ID: '{tag.Id}') contains a null entry in its {nameof(TagDefinition.Properties)} collection.");
                    }
                }

                Assert.AreEqual(0, remainingTags.Count, $"Definitions were not received for the following tags: {string.Join(", ", remainingTags)}");
            });
        }

        #endregion

        #region [ ITagSearch ]

        /// <summary>
        /// Gets the request to use with the <see cref="FindTagsRequestShouldReturnResults"/> test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="FindTagsRequest"/> to use.
        /// </returns>
        protected virtual FindTagsRequest CreateFindTagsRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="ITagSearch.FindTags"/> returns results, and that the results 
        /// match the constraints specified in the request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public virtual Task FindTagsRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<ITagSearch>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ITagSearch>();
                    return;
                }

                var request = CreateFindTagsRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<ITagSearch>(nameof(CreateFindTagsRequest));
                    return;
                }

                var channel = await feature.FindTags(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);

                var tags = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                Assert.IsTrue(tags.Count() <= request.PageSize);

                foreach (var tag in tags) {
                    Assert.IsNotNull(tag);
                    if (tag.Properties.Any()) {
                        Assert.IsTrue(tag.Properties.All(x => x != null), $"Tag '{tag.Name}' (ID: '{tag.Id}') contains a null entry in its {nameof(TagDefinition.Properties)} collection.");
                    }
                }
            });
        }

        #endregion

        #region [ IReadSnapshotTagValues ]

        /// <summary>
        /// Gets the request to use with the <see cref="ReadSnapshotTagValuesRequestShouldReturnResults"/>.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadSnapshotTagValuesRequest"/> to use.
        /// </returns>
        protected virtual ReadSnapshotTagValuesRequest CreateReadSnapshotTagValuesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IReadSnapshotTagValues.ReadSnapshotTagValues"/> returns values.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadSnapshotTagValuesRequest"/>
        [TestMethod]
        public virtual Task ReadSnapshotTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadSnapshotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadSnapshotTagValues>();
                    return;
                }

                var request = CreateReadSnapshotTagValuesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadSnapshotTagValues>(nameof(CreateReadSnapshotTagValuesRequest));
                    return;
                }

                var channel = await feature.ReadSnapshotTagValues(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);
                var values = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                var remainingTags = new HashSet<string>(request.Tags);

                Assert.AreEqual(remainingTags.Count, values.Count());

                foreach (var value in values) {
                    Assert.IsNotNull(value);
                    Assert.IsTrue(remainingTags.Remove(value.TagId) || remainingTags.Remove(value.TagName), $"Expected tags list does not contain ID '{value.TagId}' or name '{value.TagName}'.");
                }

                Assert.AreEqual(0, remainingTags.Count, $"Values were not received for the following tags: {string.Join(", ", remainingTags)}");
            });
        }

        #endregion

        #region [ ISnapshotTagValuePush ]

        /// <summary>
        /// Gets the request to use with the <see cref="SnapshotTagValueSubscriptionShouldReceiveInitialValues"/> and 
        /// <see cref="SnapshotTagValueSubscriptionShouldAllowSubscriptionChanges"/> tests.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="RealTimeData.CreateSnapshotTagValueSubscriptionRequest"/> to use.
        /// </returns>
        protected virtual CreateSnapshotTagValueSubscriptionRequest CreateSnapshotTagValueSubscriptionRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Emits snapshot values for <see cref="SnapshotTagValueSubscriptionShouldReceiveInitialValues"/> and <see cref="SnapshotTagValueSubscriptionShouldAllowSubscriptionChanges"/>.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <param name="adapter">
        ///   The adapter that must to emit the values.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that returns a flag specifying if a test values were emitted.
        /// </returns>
        protected virtual Task<bool> EmitTestSnapshotValue(TestContext context, TAdapter adapter, IEnumerable<string> tags, CancellationToken cancellationToken) {
            return Task.FromResult(false);
        }


        /// <summary>
        /// Verifies that <see cref="ISnapshotTagValuePush.Subscribe"/> returns values for tags 
        /// that are specified in the initial subscription request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadSnapshotTagValuesRequest"/>
        [TestMethod]
        public virtual Task SnapshotTagValueSubscriptionShouldReceiveInitialValues() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ISnapshotTagValuePush>();
                    return;
                }

                var request = CreateSnapshotTagValueSubscriptionRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<ISnapshotTagValuePush>(nameof(CreateSnapshotTagValueSubscriptionRequest));
                    return;
                }

                var subscription = await feature.Subscribe(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(subscription);

                // Pause briefly to allow the subscription change to take effect, since the change 
                // will be processed asynchronously to us making the initial request.
                await Task.Delay(200, ct).ConfigureAwait(false);

                var testValuesEmitted = await EmitTestSnapshotValue(TestContext, adapter, request.Tags, ct).ConfigureAwait(false);
                if (!testValuesEmitted) {
                    AssertInconclusiveDueToMissingTestInput<ISnapshotTagValuePush>(nameof(EmitTestSnapshotValue));
                    return;
                }

                var allTags = new HashSet<string>(request.Tags);
                var remainingTags = new HashSet<string>(request.Tags);

                while (await subscription.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    while (subscription.TryRead(out var value)) {
                        Assert.IsNotNull(value);
                        if (allTags.Contains(value.TagId)) {
                            remainingTags.Remove(value.TagId);
                        }
                        else if (allTags.Contains(value.TagName)) {
                            remainingTags.Remove(value.TagName);
                        }
                        else {
                            Assert.Fail($"Expected tags list does not contain ID '{value.TagId}' or name '{value.TagName}'.");
                        }
                    }

                    if (remainingTags.Count == 0) {
                        break;
                    }
                }

                Assert.AreEqual(0, remainingTags.Count, $"Values were not received for the following tags: {string.Join(", ", remainingTags)}");
            });
        }


        /// <summary>
        /// Verifies that <see cref="ISnapshotTagValuePush.Subscribe"/> returns values for tags 
        /// that are added to a subscription after the subscription has been created.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateSnapshotTagValueSubscriptionRequest"/>
        [TestMethod]
        public virtual Task SnapshotTagValueSubscriptionShouldAllowSubscriptionChanges() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<ISnapshotTagValuePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<ISnapshotTagValuePush>();
                    return;
                }

                var request = CreateSnapshotTagValueSubscriptionRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<ISnapshotTagValuePush>(nameof(CreateSnapshotTagValueSubscriptionRequest));
                    return;
                }

                var channel = Channel.CreateUnbounded<TagValueSubscriptionUpdate>();

                var subscription = await feature.Subscribe(context, new CreateSnapshotTagValueSubscriptionRequest() { 
                    PublishInterval = request.PublishInterval,
                    Properties = request.Properties 
                }, channel.Reader, ct).ConfigureAwait(false);

                Assert.IsNotNull(subscription);

                // Now add the tags to the subscription.

                channel.Writer.TryWrite(new TagValueSubscriptionUpdate() {
                    Action = SubscriptionUpdateAction.Subscribe,
                    Tags = request.Tags
                });

                // Pause briefly to allow the subscription change to take effect, since the change 
                // will be processed asynchronously to us making the initial request.
                await Task.Delay(200, ct).ConfigureAwait(false);

                var testValuesEmitted = await EmitTestSnapshotValue(TestContext, adapter, request.Tags, ct).ConfigureAwait(false);
                if (!testValuesEmitted) {
                    AssertInconclusiveDueToMissingTestInput<ISnapshotTagValuePush>(nameof(EmitTestSnapshotValue));
                    return;
                }

                var allTags = new HashSet<string>(request.Tags);
                var remainingTags = new HashSet<string>(request.Tags);

                while (await subscription.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    while (subscription.TryRead(out var val)) {
                        Assert.IsNotNull(val);
                        if (allTags.Contains(val.TagId)) {
                            remainingTags.Remove(val.TagId);
                        }
                        else if (allTags.Contains(val.TagName)) {
                            remainingTags.Remove(val.TagName);
                        }
                        else {
                            Assert.Fail($"Expected tags list does not contain ID '{val.TagId}' or name '{val.TagName}'.");
                        }
                    }

                    if (remainingTags.Count == 0) {
                        break;
                    }
                }

                Assert.AreEqual(0, remainingTags.Count, $"Values were not received for the following tags: {string.Join(", ", remainingTags)}");
            });
        }

        #endregion

        #region [ IReadRawTagValues ]

        /// <summary>
        /// Gets the request to use with the <see cref="ReadRawTagValuesRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadRawTagValuesRequest"/> to use.
        /// </returns>
        protected virtual ReadRawTagValuesRequest CreateReadRawTagValuesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IReadRawTagValues.ReadRawTagValues"/> returns values, and 
        /// that the values returned match the constraints specified in the data request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadRawTagValuesRequest"/>
        [TestMethod]
        public virtual Task ReadRawTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadRawTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadRawTagValues>();
                    return;
                }

                var request = CreateReadRawTagValuesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadRawTagValues>(nameof(CreateReadRawTagValuesRequest));
                    return;
                }

                var channel = await feature.ReadRawTagValues(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);
                var values = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                var allTags = new HashSet<string>(request.Tags);
                var remainingTags = new HashSet<string>(request.Tags);

                Assert.IsTrue(values.Any());

                foreach (var value in values) {
                    Assert.IsNotNull(value);

                    if (allTags.Contains(value.TagId)) {
                        remainingTags.Remove(value.TagId);
                    }
                    else if (allTags.Contains(value.TagName)) {
                        remainingTags.Remove(value.TagName);
                    }
                    else {
                        Assert.Fail($"Expected tags list does not contain ID '{value.TagId}' or name '{value.TagName}'.");
                    }
                }

                Assert.AreEqual(0, remainingTags.Count, $"Values were not received for the following tags: {string.Join(", ", remainingTags)}");

                foreach (var valuesForTag in values.ToLookup(x => x.TagId)) {
                    if (request.BoundaryType == RawDataBoundaryType.Inside) {
                        Assert.IsTrue(valuesForTag.All(x => x.Value.UtcSampleTime >= request.UtcStartTime));
                        Assert.IsTrue(valuesForTag.All(x => x.Value.UtcSampleTime <= request.UtcEndTime));
                    }
                    else {
                        // Allow zero or one values earlier than the query start time.
                        Assert.AreEqual(0, valuesForTag.Count(x => x.Value.UtcSampleTime < request.UtcStartTime), 1);
                        // Allow zero or one values later than the query end time.
                        Assert.AreEqual(0, valuesForTag.Count(x => x.Value.UtcSampleTime > request.UtcEndTime), 1);
                    }

                    if (request.SampleCount > 0) {
                        Assert.IsTrue(valuesForTag.Count() <= request.SampleCount, $"Expected a maximum of {request.SampleCount} samples for tag '{valuesForTag.Key}', but {valuesForTag.Count()} samples were received.");
                    }
                }
            });
        }

        #endregion

        #region [ IReadPlotTagValues ]

        /// <summary>
        /// Gets the request to use with the <see cref="ReadPlotTagValuesRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadPlotTagValuesRequest"/> to use.
        /// </returns>
        protected virtual ReadPlotTagValuesRequest CreateReadPlotTagValuesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IReadPlotTagValues.ReadPlotTagValues"/> returns values, and 
        /// that the values returned match the constraints specified in the data request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadPlotTagValuesRequest"/>
        [TestMethod]
        public virtual Task ReadPlotTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadPlotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadPlotTagValues>();
                    return;
                }

                var request = CreateReadPlotTagValuesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadPlotTagValues>(nameof(CreateReadPlotTagValuesRequest));
                    return;
                }

                var channel = await feature.ReadPlotTagValues(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);
                var values = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                var allTags = new HashSet<string>(request.Tags);
                var remainingTags = new HashSet<string>(request.Tags);

                Assert.IsTrue(values.Any());

                foreach (var value in values) {
                    Assert.IsNotNull(value);

                    if (allTags.Contains(value.TagId)) {
                        remainingTags.Remove(value.TagId);
                    }
                    else if (allTags.Contains(value.TagName)) {
                        remainingTags.Remove(value.TagName);
                    }
                    else {
                        Assert.Fail($"Expected tags list does not contain ID '{value.TagId}' or name '{value.TagName}'.");
                    }

                    Assert.IsTrue(value.Value.UtcSampleTime >= request.UtcStartTime, $"Request start time was {request.UtcStartTime}, but sample time was {value.Value.UtcSampleTime}.");
                    Assert.IsTrue(value.Value.UtcSampleTime <= request.UtcEndTime, $"Request end time was {request.UtcEndTime}, but sample time was {value.Value.UtcSampleTime}.");
                }

                Assert.AreEqual(0, remainingTags.Count, $"Values were not received for the following tags: {string.Join(", ", remainingTags)}");
            });
        }

        #endregion

        #region [ IReadProcessedTagValues ]

        /// <summary>
        /// Gets the request to use with the <see cref="ReadProcessedTagValuesRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadProcessedTagValuesRequest"/> to use.
        /// </returns>
        protected virtual ReadProcessedTagValuesRequest CreateReadProcessedTagValuesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IReadProcessedTagValues.GetSupportedDataFunctions"/> returns values.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadProcessedTagValuesRequest"/>
        [TestMethod]
        public virtual Task GetSupportedDataFunctionsRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadProcessedTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadProcessedTagValues>();
                    return;
                }

                var channel = await feature.GetSupportedDataFunctions(context, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);
                var dataFunctions = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                Assert.IsTrue(dataFunctions.Any(), $"Adapters implementing {nameof(IReadProcessedTagValues)} should define at least one data function that can be called.");
            });
        }


        /// <summary>
        /// Verifies that <see cref="IReadProcessedTagValues.ReadProcessedTagValues"/> returns values, and 
        /// that the values returned match the constraints specified in the data request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadProcessedTagValuesRequest"/>
        [TestMethod]
        public virtual Task ReadProcessedTagValuesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadProcessedTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadProcessedTagValues>();
                    return;
                }

                var request = CreateReadProcessedTagValuesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadProcessedTagValues>(nameof(CreateReadProcessedTagValuesRequest));
                    return;
                }

                var channel = await feature.ReadProcessedTagValues(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);
                var values = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                var dataFunctions = new List<string>(request.DataFunctions);
                var allTags = new HashSet<string>(request.Tags);
                var remainingTags = new HashSet<string>(request.Tags);

                Assert.IsTrue(values.Any());

                foreach (var value in values) {
                    Assert.IsNotNull(value);

                    Assert.IsTrue(dataFunctions.Contains(value.DataFunction), $"Data function '{value.DataFunction}' is unexpected.");

                    if (allTags.Contains(value.TagId)) {
                        remainingTags.Remove(value.TagId);
                    }
                    else if (allTags.Contains(value.TagName)) {
                        remainingTags.Remove(value.TagName);
                    }
                    else {
                        Assert.Fail($"Expected tags list does not contain ID '{value.TagId}' or name '{value.TagName}'.");
                    }

                    Assert.IsTrue(value.Value.UtcSampleTime >= request.UtcStartTime, $"Request start time was {request.UtcStartTime}, but sample time was {value.Value.UtcSampleTime}.");
                    Assert.IsTrue(value.Value.UtcSampleTime <= request.UtcEndTime, $"Request end time was {request.UtcEndTime}, but sample time was {value.Value.UtcSampleTime}.");
                }

                Assert.AreEqual(0, remainingTags.Count, $"Values were not received for the following tags: {string.Join(", ", remainingTags)}");
            });
        }

        #endregion

        #region [ IReadTagValuesAtTimes ]

        /// <summary>
        /// Gets the request to use with the <see cref="ReadTagValuesAtTimesRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadTagValuesAtTimesRequest"/> to use.
        /// </returns>
        protected virtual ReadTagValuesAtTimesRequest CreateReadTagValuesAtTimesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IReadProcessedTagValues.ReadProcessedTagValues"/> returns values, and 
        /// that the values returned match the constraints specified in the data request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadTagValuesAtTimesRequest"/>
        [TestMethod]
        public virtual Task ReadTagValuesAtTimesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadTagValuesAtTimes>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadTagValuesAtTimes>();
                    return;
                }

                var request = CreateReadTagValuesAtTimesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadTagValuesAtTimes>(nameof(CreateReadTagValuesAtTimesRequest));
                    return;
                }

                var channel = await feature.ReadTagValuesAtTimes(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);
                var values = await ReadAllAsync(channel, ct).ConfigureAwait(false);

                var allTimestamps = new HashSet<DateTime>(request.UtcSampleTimes);
                var allTags = new HashSet<string>(request.Tags);
                var remainingTags = new HashSet<string>(request.Tags);

                Assert.IsTrue(values.Any());

                foreach (var value in values) {
                    Assert.IsNotNull(value);

                    if (allTags.Contains(value.TagId)) {
                        remainingTags.Remove(value.TagId);
                    }
                    else if (allTags.Contains(value.TagName)) {
                        remainingTags.Remove(value.TagName);
                    }
                    else {
                        Assert.Fail($"Expected tags list does not contain ID '{value.TagId}' or name '{value.TagName}'.");
                    }

                    Assert.IsTrue(allTimestamps.Contains(value.Value.UtcSampleTime), $"Sample time {value.Value.UtcSampleTime} was not found in the expected sample times list.");
                }

                Assert.AreEqual(0, remainingTags.Count, $"Values were not received for the following tags: {string.Join(", ", remainingTags)}");

                foreach (var valuesForTag in values.ToLookup(x => x.TagId)) {
                    var remainingTimestamps = new HashSet<DateTime>(request.UtcSampleTimes);
                    foreach (var value in valuesForTag) {
                        Assert.IsTrue(remainingTimestamps.Remove(value.Value.UtcSampleTime), $"Sample time {value.Value.UtcSampleTime} was not found in the expected sample times list. This indicates that multiple values were received with this timestamp for tag '{value.TagName}'.");
                    }
                    Assert.AreEqual(0, remainingTimestamps.Count, $"Values were not received for the following timestamps for tag '{valuesForTag.Key}': {string.Join(", ", remainingTimestamps)}");
                }
            });
        }

        #endregion

        #region [ IReadTagValueAnnotations ]

        /// <summary>
        /// Gets the request to use with the <see cref="ReadTagValueAnnotationsRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadAnnotationsRequest"/> to use.
        /// </returns>
        protected virtual ReadAnnotationsRequest CreateReadAnnotationsRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Gets the request to use with the <see cref="ReadTagValueAnnotationRequestShouldReturnResult"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadAnnotationRequest"/> to use.
        /// </returns>
        protected virtual ReadAnnotationRequest CreateReadAnnotationRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IReadTagValueAnnotations.ReadAnnotations"/> returns values, 
        /// and that the values match the constraints of the request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadAnnotationsRequest"/>
        [TestMethod]
        public virtual Task ReadTagValueAnnotationsRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadTagValueAnnotations>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadTagValueAnnotations>();
                    return;
                }

                var request = CreateReadAnnotationsRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadTagValueAnnotations>(nameof(CreateReadAnnotationsRequest));
                    return;
                }

                var channel = await feature.ReadAnnotations(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);

                var annotations = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.IsTrue(annotations.Any());

                var allTags = new HashSet<string>(request.Tags);
                var remainingTags = new HashSet<string>(request.Tags);

                foreach (var annotation in annotations) {
                    Assert.IsNotNull(annotation);

                    if (allTags.Contains(annotation.TagId)) {
                        remainingTags.Remove(annotation.TagId);
                    }
                    else if (allTags.Contains(annotation.TagName)) {
                        remainingTags.Remove(annotation.TagName);
                    }
                    else {
                        Assert.Fail($"Expected tags list does not contain ID '{annotation.TagId}' or name '{annotation.TagName}'.");
                    }

                    if (annotation.Annotation.AnnotationType == AnnotationType.TimeRange) {
                        // Time range
                        if (annotation.Annotation.UtcEndTime.HasValue) {
                            // Annotation has completed.
                            Assert.IsTrue(
                                annotation.Annotation.UtcStartTime <= annotation.Annotation.UtcEndTime.Value, 
                                $"Annotation cannot end before it has started: '{annotation.TagName}' @ {annotation.Annotation.UtcStartTime}"
                            );

                            if (annotation.Annotation.UtcStartTime < request.UtcStartTime) {
                                // Annotation started before request start time; it must end at or after the request start time.
                                Assert.IsTrue(
                                    annotation.Annotation.UtcEndTime.Value >= request.UtcStartTime, 
                                    $"Annotations starting before the request start time must end after the request start time to be returned in the query: '{annotation.TagName}' @ {annotation.Annotation.UtcStartTime}"
                                );
                            }
                            else {
                                // Annotation started after the request start time; it must also start at or before the query end time.
                                Assert.IsTrue(
                                    annotation.Annotation.UtcStartTime <= request.UtcEndTime, 
                                    $"Annotation time range does not overlap with the query time range: '{annotation.TagName}' @ {annotation.Annotation.UtcStartTime}"
                                );
                            }
                        }
                        else {
                            // The annotation is ongoing. It must have started at or before the request end time.
                            Assert.IsTrue(
                                annotation.Annotation.UtcStartTime <= request.UtcEndTime,
                                $"Annotation started after the query end time: '{annotation.TagName}' @ {annotation.Annotation.UtcStartTime}"
                            );
                        }
                    }
                    else {
                        // Instantaneous
                        Assert.IsTrue(annotation.Annotation.UtcStartTime >= request.UtcStartTime);
                        Assert.IsTrue(annotation.Annotation.UtcStartTime <= request.UtcEndTime);
                    }
                }

                Assert.AreEqual(0, remainingTags.Count, $"Annotations were not received for the following tags: {string.Join(", ", remainingTags)}");

                if (request.AnnotationCount > 0) {
                    foreach (var valuesForTag in annotations.ToLookup(x => x.TagId)) {
                        Assert.IsTrue(valuesForTag.Count() <= request.AnnotationCount, $"Expected a maximum of {request.AnnotationCount} annotations for tag '{valuesForTag.Key}', but {valuesForTag.Count()} annotations were received.");
                    }
                }
            });
        }


        /// <summary>
        /// Verifies that <see cref="IReadTagValueAnnotations.ReadAnnotation"/> returns values, 
        /// and that the values match the constraints of the request.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadAnnotationRequest"/>
        [TestMethod]
        public virtual Task ReadTagValueAnnotationRequestShouldReturnResult() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadTagValueAnnotations>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadTagValueAnnotations>();
                    return;
                }

                var request = CreateReadAnnotationRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadTagValueAnnotations>(nameof(CreateReadAnnotationRequest));
                    return;
                }

                var annotation = await feature.ReadAnnotation(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(annotation);
                Assert.AreEqual(request.AnnotationId, annotation.Id);
            });
        }

        #endregion

        #region [ IWriteSnapshotTagValues ]

        /// <summary>
        /// Gets the items to write for the <see cref="WriteSnapshotTagValuesShouldSucceed"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The collection of <see cref="WriteTagValueItem"/> objects to use.
        /// </returns>
        protected virtual IEnumerable<WriteTagValueItem> CreateWriteSnapshotTagValueItems(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IWriteSnapshotTagValues.WriteSnapshotTagValues"/> returns a 
        /// result for every item that is written.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateWriteSnapshotTagValueItems"/>
        [TestMethod]
        public virtual Task WriteSnapshotTagValuesShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteSnapshotTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteSnapshotTagValues>();
                    return;
                }

                var writeItems = CreateWriteSnapshotTagValueItems(TestContext);
                if (writeItems == null) {
                    AssertInconclusiveDueToMissingTestInput<IWriteSnapshotTagValues>(nameof(CreateWriteSnapshotTagValueItems));
                    return;
                }

                var inChannel = Channel.CreateUnbounded<WriteTagValueItem>();
                foreach (var item in writeItems) {
                    item.CorrelationId = Guid.NewGuid().ToString();
                    inChannel.Writer.TryWrite(item);
                }
                inChannel.Writer.TryComplete();

                var channel = await feature.WriteSnapshotTagValues(context, inChannel.Reader, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);

                var writeResults = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.AreEqual(writeItems.Count(), writeResults.Count(), "Incorrect number of write results received.");

                var expectedCorrelationIds = new HashSet<string>(writeItems.Select(x => x.CorrelationId!));

                foreach (var writeResult in writeResults) {
                    Assert.IsNotNull(writeResult, "Null write result was returned.");
                    Assert.IsNotNull(writeResult.CorrelationId, "Write result correlation ID should not be null because a correlation ID was specified on all values that were written.");
                    Assert.IsTrue(expectedCorrelationIds.Remove(writeResult.CorrelationId!), $"Write result returned an unknown correlation ID: {writeResult.CorrelationId}");
                    Assert.AreNotEqual(WriteStatus.Fail, writeResult.Status, "Write status indicates failure.");
                }

                Assert.AreEqual(0, expectedCorrelationIds.Count, $"Write results were not returned for the following correlation IDs: {string.Join(", ", expectedCorrelationIds)}");
            });
        }

        #endregion

        #region [ IWriteHistoricalTagValues ]

        /// <summary>
        /// Gets the items to write for the <see cref="WriteHistoricalTagValuesShouldSucceed"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The collection of <see cref="WriteTagValueItem"/> objects to use.
        /// </returns>
        protected virtual IEnumerable<WriteTagValueItem> CreateWriteHistoricalTagValueItems(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IWriteHistoricalTagValues.WriteHistoricalTagValues"/> returns a 
        /// result for every item that is written.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateWriteHistoricalTagValueItems"/>
        [TestMethod]
        public virtual Task WriteHistoricalTagValuesShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteHistoricalTagValues>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteHistoricalTagValues>();
                    return;
                }

                var writeItems = CreateWriteHistoricalTagValueItems(TestContext);
                if (writeItems == null) {
                    AssertInconclusiveDueToMissingTestInput<IWriteHistoricalTagValues>(nameof(CreateWriteHistoricalTagValueItems));
                    return;
                }

                var inChannel = Channel.CreateUnbounded<WriteTagValueItem>();
                foreach (var item in writeItems) {
                    item.CorrelationId = Guid.NewGuid().ToString();
                    inChannel.Writer.TryWrite(item);
                }
                inChannel.Writer.TryComplete();

                var channel = await feature.WriteHistoricalTagValues(context, inChannel.Reader, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);

                var writeResults = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.AreEqual(writeItems.Count(), writeResults.Count(), "Incorrect number of write results received.");

                var expectedCorrelationIds = new HashSet<string>(writeItems.Select(x => x.CorrelationId!));

                foreach (var writeResult in writeResults) {
                    Assert.IsNotNull(writeResult, "Null write result was returned.");
                    Assert.IsNotNull(writeResult.CorrelationId, "Write result correlation ID should not be null because a correlation ID was specified on all values that were written.");
                    Assert.IsTrue(expectedCorrelationIds.Remove(writeResult.CorrelationId!), $"Write result returned an unknown correlation ID: {writeResult.CorrelationId}");
                    Assert.AreNotEqual(WriteStatus.Fail, writeResult.Status, "Write status indicates failure.");
                }

                Assert.AreEqual(0, expectedCorrelationIds.Count, $"Write results were not returned for the following correlation IDs: {string.Join(", ", expectedCorrelationIds)}");
            });
        }

        #endregion

        #region [ IWriteTagValueAnnotations ]

        #endregion

        #region [ IEventMessagePush ]

        /// <summary>
        /// Gets the request to use with the <see cref="EventMessagePushShouldReceiveValues"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="CreateEventMessageSubscriptionRequest"/> to use.
        /// </returns>
        protected virtual CreateEventMessageSubscriptionRequest CreateEventMessageSubscriptionRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Emits a test event for <see cref="EventMessagePushShouldReceiveValues"/> and <see cref="EventMessageTopicPushShouldReceiveInitialMessages"/>.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <param name="adapter">
        ///   The adapter that must to emit the event.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that returns a flag specifying if a test event was emitted.
        /// </returns>
        protected virtual Task<bool> EmitTestEvent(TestContext context, TAdapter adapter, CancellationToken cancellationToken) {
            return Task.FromResult(false);
        }


        /// <summary>
        /// Verifies that <see cref="IEventMessagePush.Subscribe"/> emits event messages.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateEventMessageSubscriptionRequest"/>
        [TestMethod]
        public virtual Task EventMessagePushShouldReceiveValues() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IEventMessagePush>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePush>();
                    return;
                }

                var request = CreateEventMessageSubscriptionRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IEventMessagePush>(nameof(CreateEventMessageSubscriptionRequest));
                    return;
                }

                var subscription = await feature.Subscribe(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(subscription);

                var testEventEmitted = await EmitTestEvent(TestContext, adapter, ct).ConfigureAwait(false);
                if (!testEventEmitted) {
                    AssertInconclusiveDueToMissingTestInput<IEventMessagePush>(nameof(EmitTestEvent));
                    return;
                }

                var received = await subscription.ReadAsync(ct).ConfigureAwait(false);
                Assert.IsNotNull(received);
            });
        }

        #endregion

        #region [ IEventMessagePushWithTopics ]

        /// <summary>
        /// Gets the request to use with the <see cref="EventMessageTopicPushShouldReceiveInitialMessages"/> 
        /// and <see cref="EventMessageTopicPushShouldReceiveMessagesAfterSubscriptionChange"/> tests.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The <see cref="ReadAnnotationRequest"/> to use.
        /// </returns>
        protected virtual CreateEventMessageTopicSubscriptionRequest CreateEventMessageTopicSubscriptionRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IEventMessagePushWithTopics.Subscribe"/> emits event messages.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateEventMessageTopicSubscriptionRequest"/>
        [TestMethod]
        public virtual Task EventMessageTopicPushShouldReceiveInitialMessages() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IEventMessagePushWithTopics>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePushWithTopics>();
                    return;
                }

                var request = CreateEventMessageTopicSubscriptionRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IEventMessagePushWithTopics>(nameof(CreateEventMessageTopicSubscriptionRequest));
                    return;
                }

                var subscription = await feature.Subscribe(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(subscription);

                // Pause briefly to allow the subscription change to take effect, since the change 
                // will be processed asynchronously to us making the initial request.
                await Task.Delay(200, ct).ConfigureAwait(false);

                var testEventEmitted = await EmitTestEvent(TestContext, adapter, ct).ConfigureAwait(false);
                if (!testEventEmitted) {
                    AssertInconclusiveDueToMissingTestInput<IEventMessagePushWithTopics>(nameof(EmitTestEvent));
                    return;
                }

                var received = await subscription.ReadAsync(ct).ConfigureAwait(false);
                Assert.IsNotNull(received);
            });
        }


        /// <summary>
        /// Verifies that <see cref="IEventMessagePushWithTopics.Subscribe"/> emits event messages 
        /// after subscription changes occur.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateEventMessageTopicSubscriptionRequest"/>
        [TestMethod]
        public virtual Task EventMessageTopicPushShouldReceiveMessagesAfterSubscriptionChange() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IEventMessagePushWithTopics>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IEventMessagePushWithTopics>();
                    return;
                }

                var request = CreateEventMessageTopicSubscriptionRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IEventMessagePushWithTopics>(nameof(CreateEventMessageTopicSubscriptionRequest));
                    return;
                }

                var channel = Channel.CreateUnbounded<EventMessageSubscriptionUpdate>();

                var subscription = await feature.Subscribe(context, new CreateEventMessageTopicSubscriptionRequest() { SubscriptionType = request.SubscriptionType, Properties = request.Properties }, channel.Reader, ct).ConfigureAwait(false);
                Assert.IsNotNull(subscription);

                // Now add the topics to the subscription.

                channel.Writer.TryWrite(new EventMessageSubscriptionUpdate() {
                    Action = SubscriptionUpdateAction.Subscribe,
                    Topics = request.Topics
                });

                // Pause briefly to allow the subscription change to take effect, since the change 
                // will be processed asynchronously to us writing the update into the channel.
                await Task.Delay(200, ct).ConfigureAwait(false);

                var testEventEmitted = await EmitTestEvent(TestContext, adapter, ct).ConfigureAwait(false);
                if (!testEventEmitted) {
                    AssertInconclusiveDueToMissingTestInput<IEventMessagePushWithTopics>(nameof(EmitTestEvent));
                    return;
                }

                var received = await subscription.ReadAsync(ct).ConfigureAwait(false);
                Assert.IsNotNull(received);
            });
        }

        #endregion

        #region [ IReadEventMessagesForTimeRange ]

        /// <summary>
        /// Gets the request to use in the <see cref="ReadEventMessagesForTimeRangeRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   A new <see cref="ReadEventMessagesForTimeRangeRequest"/> object.
        /// </returns>
        protected virtual ReadEventMessagesForTimeRangeRequest CreateReadEventMessagesForTimeRangeRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Ensures that <see cref="IReadEventMessagesForTimeRange.ReadEventMessagesForTimeRange"/> 
        /// returns results, and that these results meet the constraints specified by the query.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadEventMessagesForTimeRangeRequest(TestContext)"/>
        [TestMethod]
        public virtual Task ReadEventMessagesForTimeRangeRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadEventMessagesForTimeRange>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadEventMessagesForTimeRange>();
                    return;
                }

                var request = CreateReadEventMessagesForTimeRangeRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadEventMessagesForTimeRange>(nameof(CreateReadEventMessagesForTimeRangeRequest));
                    return;
                }

                var channel = await feature.ReadEventMessagesForTimeRange(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel, $"{nameof(IReadEventMessagesForTimeRange.ReadEventMessagesForTimeRange)} should not return null.");

                var events = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.IsTrue(events.Count() <= request.PageSize, $"{events.Count()} events were returned, but the page size is {request.PageSize}.");

                foreach (var evt in events) {
                    Assert.IsNotNull(evt, "Null event messages should not be returned.");
                    Assert.IsTrue(evt.UtcEventTime >= request.UtcStartTime, "Event time is earlier than query start time.");
                    Assert.IsTrue(evt.UtcEventTime <= request.UtcEndTime, "Event time is later than query end time.");
                }
            });
        }

        #endregion

        #region [ IReadEventMessagesUsingCursor ]

        /// <summary>
        /// Gets the request to use in the <see cref="ReadEventMessagesUsingCursorRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   A new <see cref="ReadEventMessagesUsingCursorRequest"/> object.
        /// </returns>
        protected virtual ReadEventMessagesUsingCursorRequest CreateReadEventMessagesUsingCursorRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Ensures that <see cref="IReadEventMessagesUsingCursor.ReadEventMessagesUsingCursor"/> 
        /// returns results, and that these results meet the constraints specified by the query.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateReadEventMessagesUsingCursorRequest(TestContext)"/>
        [TestMethod]
        public virtual Task ReadEventMessagesUsingCursorRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IReadEventMessagesUsingCursor>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IReadEventMessagesUsingCursor>();
                    return;
                }

                var request = CreateReadEventMessagesUsingCursorRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IReadEventMessagesUsingCursor>(nameof(CreateReadEventMessagesUsingCursorRequest));
                    return;
                }

                var channel = await feature.ReadEventMessagesUsingCursor(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel, $"{nameof(IReadEventMessagesUsingCursor.ReadEventMessagesUsingCursor)} should not return null.");

                var events = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.IsTrue(events.Count() <= request.PageSize, $"{events.Count()} events were returned, but the page size is {request.PageSize}.");
            });
        }

        #endregion

        #region [ IWriteEventMessages ]

        /// <summary>
        /// Gets the items to write for the <see cref="WriteEventMessagesShouldSucceed"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   The collection of <see cref="WriteEventMessageItem"/> objects to use.
        /// </returns>
        protected virtual IEnumerable<WriteEventMessageItem> CreateWriteEventMessageItems(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that <see cref="IWriteEventMessages.WriteEventMessages"/> returns a 
        /// result for every item that is written.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateWriteEventMessageItems"/>
        [TestMethod]
        public virtual Task WriteEventMessagesShouldSucceed() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IWriteEventMessages>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IWriteEventMessages>();
                    return;
                }

                var writeItems = CreateWriteEventMessageItems(TestContext);
                if (writeItems == null) {
                    AssertInconclusiveDueToMissingTestInput<IWriteEventMessages>(nameof(CreateWriteEventMessageItems));
                    return;
                }

                var inChannel = Channel.CreateUnbounded<WriteEventMessageItem>();
                foreach (var item in writeItems) {
                    item.CorrelationId = Guid.NewGuid().ToString();
                    inChannel.Writer.TryWrite(item);
                }
                inChannel.Writer.TryComplete();

                var channel = await feature.WriteEventMessages(context, inChannel.Reader, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel);

                var writeResults = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.AreEqual(writeItems.Count(), writeResults.Count(), "Incorrect number of write results received.");

                var expectedCorrelationIds = new HashSet<string>(writeItems.Select(x => x.CorrelationId!));

                foreach (var writeResult in writeResults) {
                    Assert.IsNotNull(writeResult, "Null write result was returned.");
                    Assert.IsNotNull(writeResult.CorrelationId, "Write result correlation ID should not be null because a correlation ID was specified on all items that were written.");
                    Assert.IsTrue(expectedCorrelationIds.Remove(writeResult.CorrelationId!), $"Write result returned an unknown correlation ID: {writeResult.CorrelationId}");
                    Assert.AreNotEqual(WriteStatus.Fail, writeResult.Status, "Write status indicates failure.");
                }

                Assert.AreEqual(0, expectedCorrelationIds.Count, $"Write results were not returned for the following correlation IDs: {string.Join(", ", expectedCorrelationIds)}");
            });
        }

        #endregion

        #region [ IAssetModelBrowse ]

        /// <summary>
        /// Gets the request to use in the <see cref="BrowseAssetModelNodesRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   A new <see cref="BrowseAssetModelNodesRequest"/> object.
        /// </returns>
        protected virtual BrowseAssetModelNodesRequest CreateBrowseAssetModelNodesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Gets the request to use in the <see cref="GetAssetModelNodesRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   A new <see cref="BrowseAssetModelNodesRequest"/> object.
        /// </returns>
        protected virtual GetAssetModelNodesRequest CreateGetAssetModelNodesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that a call to <see cref="IAssetModelBrowse.BrowseAssetModelNodes"/> returns results.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateBrowseAssetModelNodesRequest"/>
        [TestMethod]
        public virtual Task BrowseAssetModelNodesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IAssetModelBrowse>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IAssetModelBrowse>();
                    return;
                }

                var request = CreateBrowseAssetModelNodesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IAssetModelBrowse>(nameof(CreateBrowseAssetModelNodesRequest));
                    return;
                }

                var channel = await feature.BrowseAssetModelNodes(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel, $"{nameof(IAssetModelBrowse.BrowseAssetModelNodes)} should not return null.");

                var nodes = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.IsTrue(nodes.Any(), "Request should return one or more nodes.");
                Assert.IsTrue(nodes.All(x => x != null), "Null nodes should not be returned.");

                Assert.IsTrue(nodes.Count() <= request.PageSize, $"{nodes.Count()} nodes were returned, but the page size is {request.PageSize}.");
            });
        }


        /// <summary>
        /// Verifies that a call to <see cref="IAssetModelBrowse.GetAssetModelNodes"/> returns results.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateGetAssetModelNodesRequest"/>
        [TestMethod]
        public virtual Task GetAssetModelNodesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IAssetModelBrowse>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IAssetModelBrowse>();
                    return;
                }

                var request = CreateGetAssetModelNodesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IAssetModelBrowse>(nameof(CreateGetAssetModelNodesRequest));
                    return;
                }

                var channel = await feature.GetAssetModelNodes(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel, $"{nameof(IAssetModelBrowse.GetAssetModelNodes)} should not return null.");

                var nodes = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.IsTrue(nodes.Any(), "Request should return one or more nodes.");

                var allNodeIds = new HashSet<string>(request.Nodes);
                var remainingNodeIds = new HashSet<string>(request.Nodes);

                foreach (var node in nodes) {
                    Assert.IsNotNull(node, "Null nodes should not be returned.");
                    Assert.IsTrue(allNodeIds.Contains(node.Id), $"Received node with ID '{node.Id}', but this node was not requested.");
                    Assert.IsTrue(remainingNodeIds.Remove(node.Id), $"Expected nodes list does not contain ID '{node.Id}'.");
                }

                Assert.AreEqual(0, remainingNodeIds.Count, $"Nodes were not received for the following node IDs: {string.Join(", ", remainingNodeIds)}");
            });
        }

        #endregion

        #region [ IAssetModelSearch ]

        /// <summary>
        /// Gets the request to use in the <see cref="FindAssetModelNodesRequestShouldReturnResults"/> 
        /// test.
        /// </summary>
        /// <param name="context">
        ///   The test context.
        /// </param>
        /// <returns>
        ///   A new <see cref="FindAssetModelNodesRequest"/> object.
        /// </returns>
        protected virtual FindAssetModelNodesRequest CreateFindAssetModelNodesRequest(TestContext context) {
            return null!;
        }


        /// <summary>
        /// Verifies that a call to <see cref="IAssetModelSearch.FindAssetModelNodes"/> returns results.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        /// <seealso cref="CreateFindAssetModelNodesRequest"/>
        [TestMethod]
        public virtual Task FindAssetModelNodesRequestShouldReturnResults() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var feature = adapter.Features.Get<IAssetModelSearch>();
                if (feature == null) {
                    AssertFeatureNotImplemented<IAssetModelSearch>();
                    return;
                }

                var request = CreateFindAssetModelNodesRequest(TestContext);
                if (request == null) {
                    AssertInconclusiveDueToMissingTestInput<IAssetModelSearch>(nameof(CreateFindAssetModelNodesRequest));
                    return;
                }

                var channel = await feature.FindAssetModelNodes(context, request, ct).ConfigureAwait(false);
                Assert.IsNotNull(channel, $"{nameof(IAssetModelSearch.FindAssetModelNodes)} should not return null.");

                var nodes = await ReadAllAsync(channel, ct).ConfigureAwait(false);
                Assert.IsTrue(nodes.Any(), "Request should return one or more nodes.");
                Assert.IsTrue(nodes.All(x => x != null), "Null nodes should not be returned.");

                Assert.IsTrue(nodes.Count() <= request.PageSize, $"{nodes.Count()} nodes were returned, but the page size is {request.PageSize}.");
            });
        }

        #endregion

        #region [ Extensions ]

        /// <summary>
        /// Ensures that, for every extension feature implemented by the adapter, the feature 
        /// returns a descriptor and a set of available operations.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public Task ExtensionFeaturesShouldReturnDescriptors() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var extensionUris = adapter.Features.Keys.Where(x => x.IsChildOf(WellKnownFeatures.Extensions.ExtensionFeatureBasePath)).ToArray();
                if (extensionUris.Length == 0) {
                    Assert.Inconclusive("Adapter does not implement any extension features.");
                    return;
                }

                foreach (var extensionUri in extensionUris) {
                    var feature = adapter.GetExtensionFeature(extensionUri);
                    Assert.IsNotNull(feature, $"Unable to resolve extension feature: {extensionUri}");

                    var descriptor = await feature.GetDescriptor(context, extensionUri, ct).ConfigureAwait(false);
                    Assert.IsNotNull(descriptor, $"Feature descriptor for {extensionUri} was null.");
                    Assert.AreEqual(extensionUri, descriptor!.Uri, "Descriptor URI mismatch.");

                    var operations = await feature.GetOperations(context, extensionUri, ct).ConfigureAwait(false);
                    Assert.IsNotNull(operations, $"{nameof(IAdapterExtensionFeature.GetOperations)} for feature {extensionUri} returned null.");
                    if (operations.Any()) {
                        Assert.IsTrue(operations.All(x => x != null), $"One or more operations for feature {extensionUri} were null.");
                    }
                }
            });
        }

        #endregion

        #region [ Miscellaneous Other Tests ]

        /// <summary>
        /// This test ensures that background tasks that are registered with an adapter's 
        /// <see cref="IBackgroundTaskServiceProvider.BackgroundTaskService"/> are cancelled when 
        /// the adapter is stopped.
        /// </summary>
        /// <returns>
        ///   A <see cref="Task"/> that will run the test.
        /// </returns>
        [TestMethod]
        public Task BackgroundTaskShouldCancelWhenAdapterIsStopped() {
            return RunAdapterTest(async (adapter, context, ct) => {
                var tcs = new TaskCompletionSource<bool>();

                adapter.BackgroundTaskService.QueueBackgroundWorkItem(new IntelligentPlant.BackgroundTasks.BackgroundWorkItem(async ct2 => {
                    using (var compositeCtSource = CancellationTokenSource.CreateLinkedTokenSource(ct, ct2)) {
                        try {
                            await Task.Delay(-1, compositeCtSource.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { 
                            if (ct.IsCancellationRequested) {
                                tcs.TrySetCanceled(ct);
                            }
                        }
                        catch (Exception e) {
                            tcs.TrySetException(e);
                        }
                        finally {
                            tcs.TrySetResult(true);
                        }
                    }
                }));

                await adapter.StopAsync(ct).ConfigureAwait(false);
                await tcs.Task.ConfigureAwait(false);
            });
        }

        #endregion

    }
}
