﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataCore.Adapter {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DataCore.Adapter.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;Anonymous&gt;.
        /// </summary>
        internal static string AdapterSubscription_AnonymousUserName {
            get {
                return ResourceManager.GetString("AdapterSubscription_AnonymousUserName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Average value calculated over sample interval..
        /// </summary>
        internal static string DataFunction_Avg_Description {
            get {
                return ResourceManager.GetString("DataFunction_Avg_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Average.
        /// </summary>
        internal static string DataFunction_Avg_Name {
            get {
                return ResourceManager.GetString("DataFunction_Avg_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The number of good-quality raw samples that have been recorded for the tag at each sample interval..
        /// </summary>
        internal static string DataFunction_Count_Description {
            get {
                return ResourceManager.GetString("DataFunction_Count_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Count.
        /// </summary>
        internal static string DataFunction_Count_Name {
            get {
                return ResourceManager.GetString("DataFunction_Count_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The signed difference between the earliest good-quality value and latest good-quality value in each sample interval..
        /// </summary>
        internal static string DataFunction_Delta_Description {
            get {
                return ResourceManager.GetString("DataFunction_Delta_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Delta.
        /// </summary>
        internal static string DataFunction_Delta_Name {
            get {
                return ResourceManager.GetString("DataFunction_Delta_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Uses linear interpolation to compute a value at each sample interval based on the raw values on either side of the sample time for the interval. Step interpolation will be used for non-floating-point tags..
        /// </summary>
        internal static string DataFunction_Interp_Description {
            get {
                return ResourceManager.GetString("DataFunction_Interp_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Interpolated.
        /// </summary>
        internal static string DataFunction_Interp_Name {
            get {
                return ResourceManager.GetString("DataFunction_Interp_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the maximum value occurred at..
        /// </summary>
        internal static string DataFunction_Max_Description {
            get {
                return ResourceManager.GetString("DataFunction_Max_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum.
        /// </summary>
        internal static string DataFunction_Max_Name {
            get {
                return ResourceManager.GetString("DataFunction_Max_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Minimum good-quality value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the minimum value occurred at..
        /// </summary>
        internal static string DataFunction_Min_Description {
            get {
                return ResourceManager.GetString("DataFunction_Min_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Minimum.
        /// </summary>
        internal static string DataFunction_Min_Name {
            get {
                return ResourceManager.GetString("DataFunction_Min_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to At each interval in a time range, calculates the percentage of time in the interval that the tag value had a bad-quality status..
        /// </summary>
        internal static string DataFunction_PercentBad_Description {
            get {
                return ResourceManager.GetString("DataFunction_PercentBad_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Percent Bad.
        /// </summary>
        internal static string DataFunction_PercentBad_Name {
            get {
                return ResourceManager.GetString("DataFunction_PercentBad_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to At each interval in a time range, calculates the percentage of time in the interval that the tag value had a good-quality status..
        /// </summary>
        internal static string DataFunction_PercentGood_Description {
            get {
                return ResourceManager.GetString("DataFunction_PercentGood_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Percent Good.
        /// </summary>
        internal static string DataFunction_PercentGood_Name {
            get {
                return ResourceManager.GetString("DataFunction_PercentGood_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Status Calculation.
        /// </summary>
        internal static string DataFunction_Property_StatusCalculation {
            get {
                return ResourceManager.GetString("DataFunction_Property_StatusCalculation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The method used to calculate the quality status for the function..
        /// </summary>
        internal static string DataFunction_Property_StatusCalculation_Description {
            get {
                return ResourceManager.GetString("DataFunction_Property_StatusCalculation_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;Uncertain&quot; if any non-good quality or NaN values were present in the calculation interval, or if the value was calculated from a partial data set. Otherwise, the quality is &quot;Good&quot;..
        /// </summary>
        internal static string DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodOrNaNSkipped {
            get {
                return ResourceManager.GetString("DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodOrNaNSkipped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;Uncertain&quot; if any non-good quality values were skipped, or &quot;Good&quot; otherwise..
        /// </summary>
        internal static string DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped {
            get {
                return ResourceManager.GetString("DataFunction_Property_StatusCalculation_ValueGoodUnlessNonGoodSkipped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Worst-case status of the samples used in the calculation..
        /// </summary>
        internal static string DataFunction_Property_StatusCalculation_ValueWorstCase {
            get {
                return ResourceManager.GetString("DataFunction_Property_StatusCalculation_ValueWorstCase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Timestamp Calculation.
        /// </summary>
        internal static string DataFunction_Property_TimestampCalculation {
            get {
                return ResourceManager.GetString("DataFunction_Property_TimestampCalculation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The method used to calculate the timestamp for the function..
        /// </summary>
        internal static string DataFunction_Property_TimestampCalculation_Description {
            get {
                return ResourceManager.GetString("DataFunction_Property_TimestampCalculation_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Timestamp of maximum value.
        /// </summary>
        internal static string DataFunction_Property_TimestampCalculation_ValueMaximum {
            get {
                return ResourceManager.GetString("DataFunction_Property_TimestampCalculation_ValueMaximum", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Timestamp of minimum value.
        /// </summary>
        internal static string DataFunction_Property_TimestampCalculation_ValueMinimum {
            get {
                return ResourceManager.GetString("DataFunction_Property_TimestampCalculation_ValueMinimum", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The absolute difference between the minimum good-quality value and maximum good-quality value in each sample interval..
        /// </summary>
        internal static string DataFunction_Range_Description {
            get {
                return ResourceManager.GetString("DataFunction_Range_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Range.
        /// </summary>
        internal static string DataFunction_Range_Name {
            get {
                return ResourceManager.GetString("DataFunction_Range_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The standard deviation of all good-quality values in each sample interval.
        /// </summary>
        internal static string DataFunction_StandardDeviation_Description {
            get {
                return ResourceManager.GetString("DataFunction_StandardDeviation_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Standard Deviation.
        /// </summary>
        internal static string DataFunction_StandardDeviation_Name {
            get {
                return ResourceManager.GetString("DataFunction_StandardDeviation_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Interpolates a value at each sample interval by repeating the value of the raw sample at or immediately before the sample time..
        /// </summary>
        internal static string DataFunction_StepInterp_Description {
            get {
                return ResourceManager.GetString("DataFunction_StepInterp_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Step Interpolated.
        /// </summary>
        internal static string DataFunction_StepInterp_Name {
            get {
                return ResourceManager.GetString("DataFunction_StepInterp_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Time-weighted average value calculated over sample interval..
        /// </summary>
        internal static string DataFunction_TimeAvg_Description {
            get {
                return ResourceManager.GetString("DataFunction_TimeAvg_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Time Average.
        /// </summary>
        internal static string DataFunction_TimeAvg_Name {
            get {
                return ResourceManager.GetString("DataFunction_TimeAvg_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The variance of all good-quality values in each sample interval.
        /// </summary>
        internal static string DataFunction_Variance_Description {
            get {
                return ResourceManager.GetString("DataFunction_Variance_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Variance.
        /// </summary>
        internal static string DataFunction_Variance_Name {
            get {
                return ResourceManager.GetString("DataFunction_Variance_Name", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum adapter description length is {0}..
        /// </summary>
        internal static string Error_AdapterDescriptionIsTooLong {
            get {
                return ResourceManager.GetString("Error_AdapterDescriptionIsTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify an adapter ID..
        /// </summary>
        internal static string Error_AdapterIdIsRequired {
            get {
                return ResourceManager.GetString("Error_AdapterIdIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum adapter ID length is {0}..
        /// </summary>
        internal static string Error_AdapterIdIsTooLong {
            get {
                return ResourceManager.GetString("Error_AdapterIdIsTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An adapter cannot be started when it is already running..
        /// </summary>
        internal static string Error_AdapterIsAlreadyStarted {
            get {
                return ResourceManager.GetString("Error_AdapterIsAlreadyStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter &apos;{0}&apos; is not compatible with &apos;{1}&apos;..
        /// </summary>
        internal static string Error_AdapterIsNotCompatibleWithHelperClass {
            get {
                return ResourceManager.GetString("Error_AdapterIsNotCompatibleWithHelperClass", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter has not been started..
        /// </summary>
        internal static string Error_AdapterIsNotStarted {
            get {
                return ResourceManager.GetString("Error_AdapterIsNotStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is currently stopping and cannot be started until this operation completes..
        /// </summary>
        internal static string Error_AdapterIsStopping {
            get {
                return ResourceManager.GetString("Error_AdapterIsStopping", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum adapter name length is {0}..
        /// </summary>
        internal static string Error_AdapterNameIsTooLong {
            get {
                return ResourceManager.GetString("Error_AdapterNameIsTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The bucket size for plot calculations must be greater than zero..
        /// </summary>
        internal static string Error_BucketSizeMustBeGreaterThanZero {
            get {
                return ResourceManager.GetString("Error_BucketSizeMustBeGreaterThanZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to resolve any requested subscription topics..
        /// </summary>
        internal static string Error_CannotResolveAnySubscriptionTopics {
            get {
                return ResourceManager.GetString("Error_CannotResolveAnySubscriptionTopics", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A custom function with ID &apos;{0}&apos; is already registered..
        /// </summary>
        internal static string Error_CustomFunctionIsAlreadyRegistered {
            get {
                return ResourceManager.GetString("Error_CustomFunctionIsAlreadyRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This feature has already been registered..
        /// </summary>
        internal static string Error_FeatureIsAlreadyRegistered {
            get {
                return ResourceManager.GetString("Error_FeatureIsAlreadyRegistered", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Feature is unavailable..
        /// </summary>
        internal static string Error_FeatureUnavailable {
            get {
                return ResourceManager.GetString("Error_FeatureUnavailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid base date: {0}.
        /// </summary>
        internal static string Error_InvalidBaseDate {
            get {
                return ResourceManager.GetString("Error_InvalidBaseDate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified URI is not a valid extension feature operation URI..
        /// </summary>
        internal static string Error_InvalidExtensionFeatureOperationUri {
            get {
                return ResourceManager.GetString("Error_InvalidExtensionFeatureOperationUri", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The tag configuration is invalid..
        /// </summary>
        internal static string Error_InvalidTagConfiguration {
            get {
                return ResourceManager.GetString("Error_InvalidTagConfiguration", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to locate options for adapter ID &apos;{0}&apos; from the supplied IOptions&lt;T&gt; or IOptionsMonitor&lt;T&gt;..
        /// </summary>
        internal static string Error_NoOptionsFoundForAdapter {
            get {
                return ResourceManager.GetString("Error_NoOptionsFoundForAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Object does not implement feature &apos;{0}&apos;..
        /// </summary>
        internal static string Error_NotAFeatureImplementation {
            get {
                return ResourceManager.GetString("Error_NotAFeatureImplementation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid adapter feature type. Standard features must be interfaces that extend &apos;{1}&apos; and are annotated with &apos;{2}&apos;. Non-standard features must be interfaces or classes that extend &apos;{3}&apos; and are annotated with &apos;{4}&apos;..
        /// </summary>
        internal static string Error_NotAnAdapterFeature {
            get {
                return ResourceManager.GetString("Error_NotAnAdapterFeature", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must specify an extension feature interface type (i.e. an interface derived from {0} and annotated with {1})..
        /// </summary>
        internal static string Error_NotAnExtensionFeatureInterface {
            get {
                return ResourceManager.GetString("Error_NotAnExtensionFeatureInterface", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Caller is not authorised to invoke custom function &apos;{0}&apos;..
        /// </summary>
        internal static string Error_NotAuthorisedToInvokeFunction {
            get {
                return ResourceManager.GetString("Error_NotAuthorisedToInvokeFunction", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Too many subscriptions..
        /// </summary>
        internal static string Error_TooManySubscriptions {
            get {
                return ResourceManager.GetString("Error_TooManySubscriptions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Custom function ID &apos;{0}&apos; could not be resolved..
        /// </summary>
        internal static string Error_UnknownCustomFunctionId {
            get {
                return ResourceManager.GetString("Error_UnknownCustomFunctionId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is running with degraded health status..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Degraded {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Degraded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while performing adapter health checks..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Error {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is running with healthy status..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Healthy {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Healthy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is not running..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_NotStarted {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_NotStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The adapter is running with unhealthy status..
        /// </summary>
        internal static string HealthChecks_CompositeResultDescription_Unhealthy {
            get {
                return ResourceManager.GetString("HealthChecks_CompositeResultDescription_Unhealthy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Active Subscriber Count.
        /// </summary>
        internal static string HealthChecks_Data_ActiveSubscriberCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_ActiveSubscriberCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connection ID.
        /// </summary>
        internal static string HealthChecks_Data_ConnectionId {
            get {
                return ResourceManager.GetString("HealthChecks_Data_ConnectionId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Correlation ID.
        /// </summary>
        internal static string HealthChecks_Data_CorrelationId {
            get {
                return ResourceManager.GetString("HealthChecks_Data_CorrelationId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Node Count.
        /// </summary>
        internal static string HealthChecks_Data_NodeCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_NodeCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Passive Subscriber Count.
        /// </summary>
        internal static string HealthChecks_Data_PassiveSubscriberCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_PassiveSubscriberCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Subscriber Count.
        /// </summary>
        internal static string HealthChecks_Data_SubscriberCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_SubscriberCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Tag Count.
        /// </summary>
        internal static string HealthChecks_Data_TagCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_TagCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Topic Count.
        /// </summary>
        internal static string HealthChecks_Data_TopicCount {
            get {
                return ResourceManager.GetString("HealthChecks_Data_TopicCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User.
        /// </summary>
        internal static string HealthChecks_Data_UserName {
            get {
                return ResourceManager.GetString("HealthChecks_Data_UserName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Last Emit Time.
        /// </summary>
        internal static string HealthChecks_Data_UtcLastEmit {
            get {
                return ResourceManager.GetString("HealthChecks_Data_UtcLastEmit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Feature Health: {0}.
        /// </summary>
        internal static string HealthChecks_DisplayName_FeatureHealth {
            get {
                return ResourceManager.GetString("HealthChecks_DisplayName_FeatureHealth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter Health.
        /// </summary>
        internal static string HealthChecks_DisplayName_OverallAdapterHealth {
            get {
                return ResourceManager.GetString("HealthChecks_DisplayName_OverallAdapterHealth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ERROR.
        /// </summary>
        internal static string TagValue_ProcessedValue_Error {
            get {
                return ResourceManager.GetString("TagValue_ProcessedValue_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No data available in the bucket..
        /// </summary>
        internal static string TagValue_ProcessedValue_NoData {
            get {
                return ResourceManager.GetString("TagValue_ProcessedValue_NoData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No good-quality data available in the bucket..
        /// </summary>
        internal static string TagValue_ProcessedValue_NoGoodData {
            get {
                return ResourceManager.GetString("TagValue_ProcessedValue_NoGoodData", resourceCulture);
            }
        }
    }
}
