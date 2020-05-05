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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
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
        ///   Looks up a localized string similar to Interpolates a value at each sample interval based on the raw values on either side of the sample time for the interval..
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
        ///   Looks up a localized string similar to At each interval in a time range, calculates the percentage of raw samples in that interval that have bad-quality status..
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
        ///   Looks up a localized string similar to At each interval in a time range, calculates the percentage of raw samples in that interval that have good-quality status..
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
        ///   Looks up a localized string similar to Quality status is always &quot;Good&quot;..
        /// </summary>
        internal static string DataFunction_Property_StatusCalculation_ValueGood {
            get {
                return ResourceManager.GetString("DataFunction_Property_StatusCalculation_ValueGood", resourceCulture);
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
        ///   Looks up a localized string similar to The {0} feature is not implemented by the adapter..
        /// </summary>
        internal static string Error_MissingAdapterFeature {
            get {
                return ResourceManager.GetString("Error_MissingAdapterFeature", resourceCulture);
            }
        }
    }
}
