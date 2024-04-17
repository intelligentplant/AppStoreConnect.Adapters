﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataCore.Adapter.AspNetCore {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DataCore.Adapter.AspNetCore.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to A connection ID is required..
        /// </summary>
        internal static string Error_ConnectionIdRequired {
            get {
                return ResourceManager.GetString("Error_ConnectionIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An instance ID is required..
        /// </summary>
        internal static string Error_InstanceIdIsRequired {
            get {
                return ResourceManager.GetString("Error_InstanceIdIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A name is required..
        /// </summary>
        internal static string Error_NameIsRequired {
            get {
                return ResourceManager.GetString("Error_NameIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A subscription ID is required..
        /// </summary>
        internal static string Error_SubscriptionIdRequired {
            get {
                return ResourceManager.GetString("Error_SubscriptionIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A subscription topic is required..
        /// </summary>
        internal static string Error_SubscriptionTopicIsRequired {
            get {
                return ResourceManager.GetString("Error_SubscriptionTopicIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The instance ID for the adapter host. This is used to uniquely identify traces and metrics generated by this host in distributed telemetry systems..
        /// </summary>
        internal static string HostProperty_InstanceId_Description {
            get {
                return ResourceManager.GetString("HostProperty_InstanceId_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Indicates if the host is running inside a container.
        /// </summary>
        internal static string HostProperty_IsRunningInContainer_Description {
            get {
                return ResourceManager.GetString("HostProperty_IsRunningInContainer_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The host operating system.
        /// </summary>
        internal static string HostProperty_OperatingSystem_Description {
            get {
                return ResourceManager.GetString("HostProperty_OperatingSystem_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Options change detected for adapter ID &apos;{AdapterId}&apos;..
        /// </summary>
        internal static string Log_AdapterOptionsChangeDetected {
            get {
                return ResourceManager.GetString("Log_AdapterOptionsChangeDetected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while starting adapter &apos;{AdapterName}&apos; (ID: &apos;{AdapterId}&apos;)..
        /// </summary>
        internal static string Log_AdapterStartError {
            get {
                return ResourceManager.GetString("Log_AdapterStartError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while stopping adapter &apos;{AdapterName}&apos; (ID: &apos;{AdapterId}&apos;)..
        /// </summary>
        internal static string Log_AdapterStopError {
            get {
                return ResourceManager.GetString("Log_AdapterStopError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error in background task {WorkItem}..
        /// </summary>
        internal static string Log_ErrorInBackgroundTask {
            get {
                return ResourceManager.GetString("Log_ErrorInBackgroundTask", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Started adapter &apos;{AdapterName}&apos; (ID: &apos;{AdapterId}&apos;)..
        /// </summary>
        internal static string Log_StartedAdapter {
            get {
                return ResourceManager.GetString("Log_StartedAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting adapter &apos;{AdapterName}&apos; (ID: &apos;{AdapterId}&apos;)..
        /// </summary>
        internal static string Log_StartingAdapter {
            get {
                return ResourceManager.GetString("Log_StartingAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopped adapter &apos;{AdapterName}&apos; (ID: &apos;{AdapterId}&apos;)..
        /// </summary>
        internal static string Log_StoppedAdapter {
            get {
                return ResourceManager.GetString("Log_StoppedAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopping adapter &apos;{AdapterName}&apos; (ID: &apos;{AdapterId}&apos;)..
        /// </summary>
        internal static string Log_StoppingAdapter {
            get {
                return ResourceManager.GetString("Log_StoppingAdapter", resourceCulture);
            }
        }
    }
}
