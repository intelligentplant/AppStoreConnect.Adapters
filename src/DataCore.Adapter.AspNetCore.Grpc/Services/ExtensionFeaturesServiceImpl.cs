using System;
using System.Linq;
using System.Security;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Extensions;
using DataCore.Adapter.Extensions;


using Grpc.Core;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="ExtensionFeaturesService.ExtensionFeaturesServiceBase"/>.
    /// </summary>
    public class ExtensionFeaturesServiceImpl : ExtensionFeaturesService.ExtensionFeaturesServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The service for registering background task operations.
        /// </summary>
        private readonly IBackgroundTaskService _backgroundTaskService;


        /// <summary>
        /// Creates a new <see cref="ExtensionFeaturesServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The service for registering background task operations.
        /// </param>
        public ExtensionFeaturesServiceImpl(IAdapterAccessor adapterAccessor, IBackgroundTaskService backgroundTaskService) {
            _adapterAccessor = adapterAccessor;
            _backgroundTaskService = backgroundTaskService;
        }


        /// <inheritdoc/>
        public override async Task<FeatureDescriptor> GetDescriptor(GetExtensionDescriptorRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);

            if (!request.FeatureUri.TryCreateUriWithTrailingSlash(out var featureUri)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(adapterCallContext?.CultureInfo, Resources.Error_UnsupportedInterface, request.FeatureUri)));
            }

            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndExtensionFeature(adapterCallContext, _adapterAccessor, adapterId, featureUri!, cancellationToken).ConfigureAwait(false);

            using (Telemetry.ActivitySource.StartGetDescriptorActivity(adapter.Adapter.Descriptor.Id, featureUri)) {
                try {
                    var result = await adapter.Feature.GetDescriptor(adapterCallContext, featureUri, cancellationToken).ConfigureAwait(false);
                    return result == null
                        ? null!
                        : result.ToGrpcFeatureDescriptor();
                }
                catch (SecurityException) {
                    throw Util.CreatePermissionDeniedException();
                }
            }
        }


        /// <summary>
        /// Gets the available operations for an extension feature.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="context">
        ///   The server call context.
        /// </param>
        /// <returns>
        ///   A <see cref="GetExtensionOperationsResponse"/> describing the result of the operation.
        /// </returns>
        public override async Task<GetExtensionOperationsResponse> GetOperations(GetExtensionOperationsRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);

            if (!request.FeatureUri.TryCreateUriWithTrailingSlash(out var featureUri)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(adapterCallContext?.CultureInfo, Resources.Error_UnsupportedInterface, request.FeatureUri)));
            }

            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndExtensionFeature(adapterCallContext, _adapterAccessor, adapterId, featureUri!, cancellationToken).ConfigureAwait(false);

            using (Telemetry.ActivitySource.StartGetOperationsActivity(adapter.Adapter.Descriptor.Id, featureUri)) {
                try {
                    var result = await adapter.Feature.GetOperations(adapterCallContext, featureUri, cancellationToken).ConfigureAwait(false);
                    var response = new GetExtensionOperationsResponse();
                    if (result != null) {
                        foreach (var item in result) {
                            if (item == null) {
                                continue;
                            }
                            response.Operations.Add(item.ToGrpcExtensionOperatorDescriptor());
                        }
                    }
                    return response;
                }
                catch (SecurityException) {
                    throw Util.CreatePermissionDeniedException();

                }
            }
        }


        /// <summary>
        /// Invokes an adapter extension feature.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="context">
        ///   The server call context.
        /// </param>
        /// <returns>
        ///   An <see cref="InvokeExtensionResponse"/> describing the result of the operation.
        /// </returns>
        public override async Task<InvokeExtensionResponse> InvokeExtension(InvokeExtensionRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);

            if (!request.OperationId.TryCreateUriWithTrailingSlash(out var operationId)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(adapterCallContext?.CultureInfo, Resources.Error_UnsupportedInterface, request.OperationId)));
            }

            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId!, out var featureUri, out var error)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, error));
            }

            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndExtensionFeature(adapterCallContext, _adapterAccessor, adapterId, featureUri, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Extensions.InvocationRequest() {
                OperationId = operationId!,
                Arguments = request.Arguments.Select(x => x.ToAdapterVariant()).ToArray()
            };
            Util.ValidateObject(adapterRequest);

            using (Telemetry.ActivitySource.StartInvokeActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                try {
                    var result = await adapter.Feature.Invoke(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);

                    var response = new InvokeExtensionResponse();
                    response.Results.AddRange(result.Results.Select(x => x.ToGrpcVariant()));

                    return response;
                }
                catch (SecurityException) {
                    throw Util.CreatePermissionDeniedException();
                }
            }
        }


        /// <summary>
        /// Invokes a streaming adapter extension feature.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="responseStream">
        ///   The response stream to write results to.
        /// </param>
        /// <param name="context">
        ///   The server call context.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will process the request.
        /// </returns>
        public override async Task InvokeStreamingExtension(InvokeExtensionRequest request, IServerStreamWriter<InvokeExtensionResponse> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);

            if (!request.OperationId.TryCreateUriWithTrailingSlash(out var operationId)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(adapterCallContext?.CultureInfo, Resources.Error_UnsupportedInterface, request.OperationId)));
            }

            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId!, out var featureUri, out var error)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, error));
            }

            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndExtensionFeature(adapterCallContext, _adapterAccessor, adapterId, featureUri, cancellationToken).ConfigureAwait(false);
            var adapterRequest = new Extensions.InvocationRequest() {
                OperationId = operationId!,
                Arguments = request.Arguments.Select(x => x.ToAdapterVariant()).ToArray()
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartStreamActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                try {
                    var result = await adapter.Feature.Stream(adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
                    long outputItems = 0;
                    while (!cancellationToken.IsCancellationRequested) {
                        var val = await result.ReadAsync(cancellationToken).ConfigureAwait(false);

                        var response = new InvokeExtensionResponse();
                        response.Results.AddRange(val.Results.Select(x => x.ToGrpcVariant()));

                        await responseStream.WriteAsync(response).ConfigureAwait(false);
                        activity.SetResponseItemCountTag(++outputItems);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (SecurityException) {
                    throw Util.CreatePermissionDeniedException();
                }
            }
        }


        /// <summary>
        /// Invokes a duplex streaming adapter extension feature.
        /// </summary>
        /// <param name="requestStream">
        ///   The request stream to read from.
        /// </param>
        /// <param name="responseStream">
        ///   The response stream to write results to.
        /// </param>
        /// <param name="context">
        ///   The server call context.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will process the request.
        /// </returns>
        public override async Task InvokeDuplexStreamingExtension(IAsyncStreamReader<InvokeExtensionRequest> requestStream, IServerStreamWriter<InvokeExtensionResponse> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var cancellationToken = context.CancellationToken;

            // Wait for first request stream item.
            if (!await requestStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                return;
            }

            // Now initiate the call.

            var request = requestStream.Current;

            if (!request.OperationId.TryCreateUriWithTrailingSlash(out var operationId)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(adapterCallContext?.CultureInfo, Resources.Error_UnsupportedInterface, request.OperationId)));
            }

            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationId!, out var featureUri, out var error)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, error));
            }

            var adapterId = request.AdapterId;
            
            var adapter = await Util.ResolveAdapterAndExtensionFeature(adapterCallContext, _adapterAccessor, adapterId, featureUri, cancellationToken).ConfigureAwait(false);
            var adapterRequest = new Extensions.InvocationRequest() {
                OperationId = operationId!,
                Arguments = request.Arguments.Select(x => x.ToAdapterVariant()).ToArray()
            };
            Util.ValidateObject(adapterRequest);

            using (var activity = Telemetry.ActivitySource.StartDuplexStreamActivity(adapter.Adapter.Descriptor.Id, adapterRequest)) {
                try {
                    long inputItems = 1;
                    activity.SetRequestItemCountTag(inputItems);

                    var inputChannel = Channel.CreateUnbounded<InvocationStreamItem>(new UnboundedChannelOptions() {
                        SingleReader = true,
                        SingleWriter = true
                    });

                    var result = await adapter.Feature.DuplexStream(adapterCallContext, adapterRequest, inputChannel, cancellationToken).ConfigureAwait(false);

                    // Run a background operation to process the remaining request stream items.
                    _backgroundTaskService.QueueBackgroundWorkItem(async ct => {
                        try {
                            while (await requestStream.MoveNext(ct).ConfigureAwait(false)) {
                                await inputChannel.Writer.WriteAsync(new InvocationStreamItem() {
                                    Arguments = requestStream.Current.Arguments.Select(x => x.ToAdapterVariant()).ToArray()
                                }, ct).ConfigureAwait(false);
                                activity.SetRequestItemCountTag(++inputItems);
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception e) {
                            inputChannel.Writer.TryComplete(e);
                        }
                        finally {
                            inputChannel.Writer.TryComplete();
                        }
                    }, null, true, cancellationToken);

                    long outputItems = 0;
                    while (!cancellationToken.IsCancellationRequested) {
                        var val = await result.ReadAsync(cancellationToken).ConfigureAwait(false);

                        var response = new InvokeExtensionResponse();
                        response.Results.AddRange(val.Results.Select(x => x.ToGrpcVariant()));

                        await responseStream.WriteAsync(response).ConfigureAwait(false);
                        activity.SetResponseItemCountTag(++outputItems);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (SecurityException) {
                    throw Util.CreatePermissionDeniedException();
                }
            }
        }

    }
}
