﻿
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// <see cref="JsonSerializerContext"/> implementation for all adapter model types.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(AssetModel.AssetModelNode))]
    [JsonSerializable(typeof(AssetModel.BrowseAssetModelNodesRequest))]
    [JsonSerializable(typeof(AssetModel.DataReference))]
    [JsonSerializable(typeof(AssetModel.FindAssetModelNodesRequest))]
    [JsonSerializable(typeof(AssetModel.GetAssetModelNodesRequest))]
    [JsonSerializable(typeof(AssetModel.NodeType))]
    [JsonSerializable(typeof(Common.AdapterDescriptor))]
    [JsonSerializable(typeof(Common.AdapterDescriptorExtended))]
    [JsonSerializable(typeof(Common.AdapterProperty))]
    [JsonSerializable(typeof(Common.AdapterRequest))]
    [JsonSerializable(typeof(Common.AdapterTypeDescriptor))]
    [JsonSerializable(typeof(Common.ApiDescriptor))]
    [JsonSerializable(typeof(Common.FeatureDescriptor))]
    [JsonSerializable(typeof(Common.FindAdaptersRequest))]
    [JsonSerializable(typeof(Common.HostInfo))]
    [JsonSerializable(typeof(Common.PageableAdapterRequest))]
    [JsonSerializable(typeof(Common.SubscriptionUpdateAction))]
    [JsonSerializable(typeof(Common.Variant))]
    [JsonSerializable(typeof(Common.VariantType))]
    [JsonSerializable(typeof(Common.VendorInfo))]
    [JsonSerializable(typeof(Common.WriteOperationResult))]
    [JsonSerializable(typeof(Common.WriteStatus))]
    [JsonSerializable(typeof(Diagnostics.ConfigurationChange))]
    [JsonSerializable(typeof(Diagnostics.ConfigurationChangesSubscriptionRequest))]
    [JsonSerializable(typeof(Diagnostics.ConfigurationChangeType))]
    [JsonSerializable(typeof(Diagnostics.HealthCheckResult))]
    [JsonSerializable(typeof(Diagnostics.HealthStatus))]
    [JsonSerializable(typeof(Events.CreateEventMessageSubscriptionRequest))]
    [JsonSerializable(typeof(Events.CreateEventMessageTopicSubscriptionRequest))]
    [JsonSerializable(typeof(Events.EventMessage))]
    [JsonSerializable(typeof(Events.EventMessageSubscriptionType))]
    [JsonSerializable(typeof(Events.EventMessageSubscriptionUpdate))]
    [JsonSerializable(typeof(Events.EventMessageWithCursorPosition))]
    [JsonSerializable(typeof(Events.EventPriority))]
    [JsonSerializable(typeof(Events.EventReadDirection))]
    [JsonSerializable(typeof(Events.ReadEventMessagesForTimeRangeRequest))]
    [JsonSerializable(typeof(Events.ReadEventMessagesUsingCursorRequest))]
    [JsonSerializable(typeof(Events.WriteEventMessageItem))]
    [JsonSerializable(typeof(Events.WriteEventMessageResult))]
    [JsonSerializable(typeof(Events.WriteEventMessagesRequest))]
    [JsonSerializable(typeof(Events.WriteEventMessagesRequestExtended))]
    [JsonSerializable(typeof(Extensions.CustomFunctionDescriptor))]
    [JsonSerializable(typeof(Extensions.CustomFunctionDescriptorExtended))]
    [JsonSerializable(typeof(Extensions.CustomFunctionInvocationRequest))]
    [JsonSerializable(typeof(Extensions.CustomFunctionInvocationResponse))]
    [JsonSerializable(typeof(Extensions.GetCustomFunctionRequest))]
    [JsonSerializable(typeof(Extensions.GetCustomFunctionsRequest))]
    [JsonSerializable(typeof(RealTimeData.AnnotationType))]
    [JsonSerializable(typeof(RealTimeData.CreateAnnotationRequest))]
    [JsonSerializable(typeof(RealTimeData.CreateSnapshotTagValueSubscriptionRequest))]
    [JsonSerializable(typeof(RealTimeData.DataFunctionDescriptor))]
    [JsonSerializable(typeof(RealTimeData.DataFunctionSampleTimeType))]
    [JsonSerializable(typeof(RealTimeData.DataFunctionStatusType))]
    [JsonSerializable(typeof(RealTimeData.DeleteAnnotationRequest))]
    [JsonSerializable(typeof(RealTimeData.GetSupportedDataFunctionsRequest))]
    [JsonSerializable(typeof(RealTimeData.ProcessedTagValueQueryResult))]
    [JsonSerializable(typeof(RealTimeData.RawDataBoundaryType))]
    [JsonSerializable(typeof(RealTimeData.ReadAnnotationRequest))]
    [JsonSerializable(typeof(RealTimeData.ReadAnnotationsRequest))]
    [JsonSerializable(typeof(RealTimeData.ReadPlotTagValuesRequest))]
    [JsonSerializable(typeof(RealTimeData.ReadProcessedTagValuesRequest))]
    [JsonSerializable(typeof(RealTimeData.ReadRawTagValuesRequest))]
    [JsonSerializable(typeof(RealTimeData.ReadSnapshotTagValuesRequest))]
    [JsonSerializable(typeof(RealTimeData.ReadTagValuesAtTimesRequest))]
    [JsonSerializable(typeof(RealTimeData.TagValue))]
    [JsonSerializable(typeof(RealTimeData.TagValueAnnotation))]
    [JsonSerializable(typeof(RealTimeData.TagValueAnnotationExtended))]
    [JsonSerializable(typeof(RealTimeData.TagValueAnnotationQueryResult))]
    [JsonSerializable(typeof(RealTimeData.TagValueExtended))]
    [JsonSerializable(typeof(RealTimeData.TagValueQueryResult))]
    [JsonSerializable(typeof(RealTimeData.TagValueStatus))]
    [JsonSerializable(typeof(RealTimeData.TagValueSubscriptionUpdate))]
    [JsonSerializable(typeof(RealTimeData.UpdateAnnotationRequest))]
    [JsonSerializable(typeof(RealTimeData.WriteTagValueAnnotationResult))]
    [JsonSerializable(typeof(RealTimeData.WriteTagValueItem))]
    [JsonSerializable(typeof(RealTimeData.WriteTagValueResult))]
    [JsonSerializable(typeof(RealTimeData.WriteTagValuesRequest))]
    [JsonSerializable(typeof(RealTimeData.WriteTagValuesRequestExtended))]
    [JsonSerializable(typeof(Tags.DigitalState))]
    [JsonSerializable(typeof(Tags.DigitalStateSet))]
    [JsonSerializable(typeof(Tags.FindTagsRequest))]
    [JsonSerializable(typeof(Tags.GetTagPropertiesRequest))]
    [JsonSerializable(typeof(Tags.GetTagsRequest))]
    [JsonSerializable(typeof(Tags.TagDefinition))]
    [JsonSerializable(typeof(Tags.TagDefinitionFields))]
    [JsonSerializable(typeof(Tags.TagIdentifier))]
    [JsonSerializable(typeof(Tags.TagSummary))]
    public partial class AdapterJsonContext : JsonSerializerContext { }

}