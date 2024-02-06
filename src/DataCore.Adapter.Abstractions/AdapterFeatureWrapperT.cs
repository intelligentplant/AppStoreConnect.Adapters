using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {

    /// <summary>
    /// Strongly-typed wrapper for an adapter feature registered with an <see cref="AdapterCore"/> 
    /// instance.
    /// </summary>
    /// <typeparam name="TFeature">
    ///   The interface for the wrapped feature.
    /// </typeparam>
    public abstract partial class AdapterFeatureWrapper<TFeature> : AdapterFeatureWrapper where TFeature : IAdapterFeature {

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The <typeparamref name="TFeature"/> that is wrapped by the <see cref="AdapterFeatureWrapper{TFeature}"/>.
        /// </summary>
        internal new TFeature InnerFeature => (TFeature) base.InnerFeature;


        /// <summary>
        /// Creates a new <see cref="AdapterFeatureWrapper{TFeature}"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The <see cref="AdapterCore"/> for the feature.
        /// </param>
        /// <param name="innerFeature">
        ///   The inner feature to wrap.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="innerFeature"/> is <see langword="null"/>.
        /// </exception>
        internal AdapterFeatureWrapper(AdapterCore adapter, TFeature innerFeature) : base(adapter, innerFeature) { 
            _logger = adapter.LoggerFactory.CreateLogger<TFeature>();
        }


        /// <summary>
        /// Gets the ID for the given method on the <typeparamref name="TFeature"/> for use in 
        /// telemetry messages.
        /// </summary>
        /// <param name="operationName">
        ///   The name of the method being invoked.
        /// </param>
        /// <returns>
        ///   The operation ID to use.
        /// </returns>
        private string GetOperationId(string operationName) => string.Concat(typeof(TFeature).Name, "/", operationName);


        /// <summary>
        /// Starts an activity with the specified name.
        /// </summary>
        /// <param name="name">
        ///   The unqualified activity name.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        private Activity? StartActivity(string name) {
            return Adapter.StartActivity<TFeature>(name);
        }


        /// <summary>
        /// Generates telemetry that the specified operation has started.
        /// </summary>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        private void OnOperationStarted(string operationName) {
            Diagnostics.Telemetry.EventSource.AdapterOperationStarted(Adapter.Descriptor.Id, GetOperationId(operationName));
            LogOperationStarted();
        }


        /// <summary>
        /// Generates telemetry that the specified operation has completed.
        /// </summary>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <param name="elapsedMilliseconds">
        ///   The duration of the operation in milliseconds.
        /// </param>
        private void OnOperationCompleted(string operationName, double elapsedMilliseconds) {
            Diagnostics.Telemetry.EventSource.AdapterOperationCompleted(Adapter.Descriptor.Id, GetOperationId(operationName), elapsedMilliseconds);
            LogOperationCompleted(elapsedMilliseconds);
        }


        /// <summary>
        /// Generates telemetry that the specified operation has faulted.
        /// </summary>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <param name="elapsedMilliseconds">
        ///   The duration of the operation in milliseconds.
        /// </param>
        /// <param name="error">
        ///   The error that caused the fault.
        /// </param>
        private void OnOperationFaulted(string operationName, double elapsedMilliseconds, Exception error) {
            Diagnostics.Telemetry.EventSource.AdapterOperationFaulted(Adapter.Descriptor.Id, GetOperationId(operationName), elapsedMilliseconds, error.Message);
            LogOperationFaulted(error, elapsedMilliseconds);
        }


        /// <summary>
        /// Generates telemetry that an operation emitted a server streaming item.
        /// </summary>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        private void OnStreamItemOut(string operationName) {
            Diagnostics.Telemetry.EventSource.AdapterStreamItemOut(Adapter.Descriptor.Id, GetOperationId(operationName));
        }


        /// <summary>
        /// Generates telemetry that an operation consumed a client streaming item.
        /// </summary>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        private void OnStreamItemIn(string operationName) {
            Diagnostics.Telemetry.EventSource.AdapterStreamItemIn(Adapter.Descriptor.Id, GetOperationId(operationName));
        }


        /// <summary>
        /// Sets tags on the current activity based on a request object.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The request object.
        /// </typeparam>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        private void SetActivityTags<TRequest>(Activity? activity, TRequest request) {
            if (activity == null) {
                return;
            }

            if (request is Common.IPageableAdapterRequest pageable) {
                activity.SetTag("page_size", pageable.PageSize);
                activity.SetTag("page", pageable.Page);
            }

            if (request is Events.ReadHistoricalEventMessagesRequest readEventHistory) {
                activity.SetTag("direction", readEventHistory.Direction);

                if (request is Events.ReadEventMessagesForTimeRangeRequest readEventsForTimeRange) {
                    activity.SetTag("topic_count", readEventsForTimeRange.Topics?.Count() ?? 0);
                    activity.SetTag("query_start_time", readEventsForTimeRange.UtcStartTime);
                    activity.SetTag("query_end_time", readEventsForTimeRange.UtcEndTime);
                }
            }
            else if (request is RealTimeData.ReadTagDataRequest readTagData) {
                activity.SetTag("tag_count", readTagData.Tags.Count());

                if (request is RealTimeData.ReadHistoricalTagValuesRequest readHistory) {
                    activity.SetTag("query_start_time", readHistory.UtcStartTime);
                    activity.SetTag("query_end_time", readHistory.UtcEndTime);
                }
            }
        }


        /// <summary>
        /// Invokes a unary feature operation.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The operation request type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The operation response type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The operation request object.
        /// </param>
        /// <param name="callback">
        ///   The callback that will perform the operation once validation checks have been 
        ///   performed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <returns>
        ///   The invocation response.
        /// </returns>
        protected async Task<TOut> InvokeAsync<TRequest, TOut>(IAdapterCallContext context, TRequest request, Func<IAdapterCallContext, TRequest, CancellationToken, Task<TOut>> callback, CancellationToken cancellationToken, [CallerMemberName] string operationName = "") {
            Adapter.CheckDisposed();
            Adapter.CheckStarted();

            using var activity = StartActivity(operationName);
            using var loggerScope = BeginLoggerScope(operationName);

            var stopwatch = Diagnostics.ValueStopwatch.StartNew();
            OnOperationStarted(operationName);

            try {
                if (context.ShouldValidateRequests()) {
                    Adapter.ValidateInvocation(context, request!);
                }
                else {
                    Adapter.ValidateInvocation(context);
                }
            }
            catch (Exception e) {
                OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                throw;
            }

            SetActivityTags(activity, request);

            using (var ctSource = Adapter.CreateCancellationTokenSource(cancellationToken)) {
                try {
                    var result = await callback(context, request, ctSource.Token).ConfigureAwait(false);
                    OnOperationCompleted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds);
                    return result;
                }
                catch (Exception e) {
                    OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                    throw;
                }
            }
        }


        /// <summary>
        /// Invokes a unary feature operation that does not use a request object.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The operation response type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="callback">
        ///   The callback that will perform the operation once validation checks have been 
        ///   performed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <returns>
        ///   The invocation response.
        /// </returns>
        protected async Task<TOut> InvokeAsync<TOut>(IAdapterCallContext context, Func<IAdapterCallContext, CancellationToken, Task<TOut>> callback, CancellationToken cancellationToken, [CallerMemberName] string operationName = "") {
            Adapter.CheckDisposed();
            Adapter.CheckStarted();

            using var activity = StartActivity(operationName);
            using var loggerScope = BeginLoggerScope(operationName);

            var stopwatch = Diagnostics.ValueStopwatch.StartNew();
            OnOperationStarted(operationName);

            try {
                Adapter.ValidateInvocation(context);
            }
            catch (Exception e) {
                OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                throw;
            }

            using (var ctSource = Adapter.CreateCancellationTokenSource(cancellationToken)) {
                try {
                    var result = await callback(context, ctSource.Token).ConfigureAwait(false);
                    OnOperationCompleted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds);
                    return result;
                }
                catch (Exception e) {
                    OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                    throw;
                }
            }
        }


        /// <summary>
        /// Invokes a server streaming feature operation.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The operation request type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The operation response type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The operation request object.
        /// </param>
        /// <param name="callback">
        ///   The callback that will perform the operation once validation checks have been 
        ///   performed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <returns>
        ///   The streamed response items.
        /// </returns>
        protected async IAsyncEnumerable<TOut> ServerStreamAsync<TRequest, TOut>(IAdapterCallContext context, TRequest request, Func<IAdapterCallContext, TRequest, CancellationToken, IAsyncEnumerable<TOut>> callback,  [EnumeratorCancellation] CancellationToken cancellationToken, [CallerMemberName] string operationName = "") {
            Adapter.CheckDisposed();
            Adapter.CheckStarted();

            using var activity = StartActivity(operationName);

            var stopwatch = Diagnostics.ValueStopwatch.StartNew();

            using (BeginLoggerScope(operationName)) {
                OnOperationStarted(operationName);

                try {
                    if (context.ShouldValidateRequests()) {
                        Adapter.ValidateInvocation(context, request!);
                    }
                    else {
                        Adapter.ValidateInvocation(context);
                    }
                }
                catch (Exception e) {
                    OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                    throw;
                }
            }

            SetActivityTags(activity, request);

            using (var ctSource = Adapter.CreateCancellationTokenSource(cancellationToken))
            await using (var enumerator = callback(context, request, ctSource.Token).ConfigureAwait(false).GetAsyncEnumerator()) {
                while (true) {
                    try {
                        if (!await enumerator.MoveNextAsync()) {
                            break;
                        }
                    }
                    catch (OperationCanceledException e) {
                        if (cancellationToken.IsCancellationRequested) {
                            // Caller has cancelled the operation, so break and consider the
                            // operation complete.
                            break;
                        }

                        // Cancellation was not requested by the caller, so consider this a faulted
                        // operation.
                        using (BeginLoggerScope(operationName)) {
                            OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                        }
                        throw;
                    }
                    catch (Exception e) {
                        using (BeginLoggerScope(operationName)) {
                            OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                        }
                        throw;
                    }

                    OnStreamItemOut(operationName);
                    yield return enumerator.Current;
                }
            }

            using (BeginLoggerScope(operationName)) {
                OnOperationCompleted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds);
            }
        }


        /// <summary>
        /// Invokes a server streaming feature operation that does not use a request object.
        /// </summary>
        /// <typeparam name="TOut">
        ///   The operation response type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="callback">
        ///   The callback that will perform the operation once validation checks have been 
        ///   performed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <returns>
        ///   The streamed response items.
        /// </returns>
        protected async IAsyncEnumerable<TOut> ServerStreamAsync<TOut>(IAdapterCallContext context, Func<IAdapterCallContext, CancellationToken, IAsyncEnumerable<TOut>> callback, [EnumeratorCancellation] CancellationToken cancellationToken, [CallerMemberName] string operationName = "") {
            Adapter.CheckDisposed();
            Adapter.CheckStarted();

            using var activity = StartActivity(operationName);

            var stopwatch = Diagnostics.ValueStopwatch.StartNew();

            using (BeginLoggerScope(operationName)) {
                OnOperationStarted(operationName);
                try {
                    Adapter.ValidateInvocation(context);
                }
                catch (Exception e) {
                    OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                    throw;
                }
            }

            using (var ctSource = Adapter.CreateCancellationTokenSource(cancellationToken))
            await using (var enumerator = callback(context, ctSource.Token).ConfigureAwait(false).GetAsyncEnumerator()) {
                while (true) {
                    try {
                        if (!await enumerator.MoveNextAsync()) {
                            break;
                        }
                    }
                    catch (OperationCanceledException e) {
                        if (cancellationToken.IsCancellationRequested) {
                            // Caller has cancelled the operation, so break and consider the
                            // operation complete.
                            break;
                        }

                        // Cancellation was not requested by the caller, so consider this a faulted
                        // operation.
                        using (BeginLoggerScope(operationName)) {    
                            OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                        }
                        throw;
                    }
                    catch (Exception e) {
                        using (BeginLoggerScope(operationName)) {
                            OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                        }
                        throw;
                    }

                    OnStreamItemOut(operationName);
                    yield return enumerator.Current;
                }
            }

            using (BeginLoggerScope(operationName)) {
                OnOperationCompleted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds);
            }
        }


        /// <summary>
        /// Invokes a duplex streaming (client- and server-streaming) feature operation.
        /// </summary>
        /// <typeparam name="TRequest">
        ///   The operation request type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The server stream item type.
        /// </typeparam>
        /// <typeparam name="TIn">
        ///   The client stream item type.
        /// </typeparam>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The operation request object.
        /// </param>
        /// <param name="inStream">
        ///   The client stream.
        /// </param>
        /// <param name="callback">
        ///   The callback that will perform the operation once validation checks have been 
        ///   performed.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <returns>
        ///   The streamed response items.
        /// </returns>
        protected async IAsyncEnumerable<TOut> DuplexStreamAsync<TRequest, TIn, TOut>(IAdapterCallContext context, TRequest request, IAsyncEnumerable<TIn> inStream, Func<IAdapterCallContext, TRequest, IAsyncEnumerable<TIn>, CancellationToken, IAsyncEnumerable<TOut>> callback, [EnumeratorCancellation] CancellationToken cancellationToken, [CallerMemberName] string operationName = "") {
            Adapter.CheckDisposed();
            Adapter.CheckStarted();

            using var activity = StartActivity(operationName);

            var stopwatch = Diagnostics.ValueStopwatch.StartNew();

            using (BeginLoggerScope(operationName)) {
                OnOperationStarted(operationName);

                try {
                    if (context.ShouldValidateRequests()) {
                        Adapter.ValidateInvocation(context, request!, inStream);
                    }
                    else {
                        Adapter.ValidateInvocation(context, inStream);
                    }
                }
                catch (Exception e) {
                    OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                    throw;
                }
            }

            SetActivityTags(activity, request);

            using (var ctSource = Adapter.CreateCancellationTokenSource(cancellationToken))
            await using (var enumerator = callback(context, request, ProcessInputStream(inStream, operationName, ctSource.Token), ctSource.Token).ConfigureAwait(false).GetAsyncEnumerator()) {
                while (true) {
                    try {
                        if (!await enumerator.MoveNextAsync()) {
                            break;
                        }
                    }
                    catch (OperationCanceledException e) {
                        if (cancellationToken.IsCancellationRequested) {
                            // Caller has cancelled the operation, so break and consider the
                            // operation complete.
                            break;
                        }

                        // Cancellation was not requested by the caller, so consider this a faulted
                        // operation.
                        using (BeginLoggerScope(operationName)) {
                            OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                        }
                        throw;
                    }
                    catch (Exception e) {
                        using (BeginLoggerScope(operationName)) {
                            OnOperationFaulted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds, e);
                        }
                        throw;
                    }

                    OnStreamItemOut(operationName);
                    yield return enumerator.Current;
                }
            }

            using (BeginLoggerScope(operationName)) {
                OnOperationCompleted(operationName, stopwatch.GetElapsedTime().TotalMilliseconds);
            }
        }


        /// <summary>
        /// Emits the specified client stream and calls <see cref="OnStreamItemIn"/> for each 
        /// emitted item.
        /// </summary>
        /// <typeparam name="T">
        ///   The stream item type.
        /// </typeparam>
        /// <param name="inStream">
        ///   The client stream.
        /// </param>
        /// <param name="operationName">
        ///   The name of the operation that is processing the stream.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The streamed client items.
        /// </returns>
        private async IAsyncEnumerable<T> ProcessInputStream<T>(IAsyncEnumerable<T> inStream, string operationName, [EnumeratorCancellation] CancellationToken cancellationToken) { 
            await foreach (var item in inStream.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                yield return item;
                OnStreamItemIn(operationName);
            }
        }


        private IDisposable? BeginLoggerScope(string operationName) {
            return _logger.BeginScope(new Dictionary<string, object?>() {
                ["AdapterId"] = Adapter.Descriptor.Id,
                ["OperationName"] = operationName
            });
        }


        [LoggerMessage(1, LogLevel.Debug, "Operation started.")]
        partial void LogOperationStarted();

        [LoggerMessage(2, LogLevel.Debug, "Operation completed. Duration: {elapsed} ms")]
        partial void LogOperationCompleted(double elapsed);

        [LoggerMessage(3, LogLevel.Debug, "Operation faulted. Duration: {elapsed} ms")] // Debug because the exception is not suppressed in this class.
        partial void LogOperationFaulted(Exception e, double elapsed);

    }
}
