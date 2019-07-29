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
        ///   Looks up a localized string similar to An adapter cannot be started when it is already running..
        /// </summary>
        internal static string Error_AdapterIsAlreadyStarted {
            get {
                return ResourceManager.GetString("Error_AdapterIsAlreadyStarted", resourceCulture);
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
        ///   Looks up a localized string similar to The bucket size for plot calculations must be greater than zero..
        /// </summary>
        internal static string Error_BucketSizeMustBeGreaterThanZero {
            get {
                return ResourceManager.GetString("Error_BucketSizeMustBeGreaterThanZero", resourceCulture);
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
        ///   Looks up a localized string similar to Object does not implement feature {0}..
        /// </summary>
        internal static string Error_NotAFeatureImplementation {
            get {
                return ResourceManager.GetString("Error_NotAFeatureImplementation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while starting adapter {AdapterId}..
        /// </summary>
        internal static string Log_AdapterStartupError {
            get {
                return ResourceManager.GetString("Log_AdapterStartupError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while stopping adapter {AdapterId}..
        /// </summary>
        internal static string Log_AdapterStopError {
            get {
                return ResourceManager.GetString("Log_AdapterStopError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopping adapter {AdapterId} (disposing: true)..
        /// </summary>
        internal static string Log_DisposingAdapter {
            get {
                return ResourceManager.GetString("Log_DisposingAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred in the event subscription manager publish loop..
        /// </summary>
        internal static string Log_ErrorInEventSubscriptionManagerPublishLoop {
            get {
                return ResourceManager.GetString("Log_ErrorInEventSubscriptionManagerPublishLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while the snapshot subscription manager was polling for new values..
        /// </summary>
        internal static string Log_ErrorInSnapshotPollingUpdateLoop {
            get {
                return ResourceManager.GetString("Log_ErrorInSnapshotPollingUpdateLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred in the snapshot subscription manager publish loop..
        /// </summary>
        internal static string Log_ErrorInSnapshotSubscriptionManagerPublishLoop {
            get {
                return ResourceManager.GetString("Log_ErrorInSnapshotSubscriptionManagerPublishLoop", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while disposing of an event message subscription..
        /// </summary>
        internal static string Log_ErrorWhileDisposingOfEventMessageSubscription {
            get {
                return ResourceManager.GetString("Log_ErrorWhileDisposingOfEventMessageSubscription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while disposing of a snapshot subscription..
        /// </summary>
        internal static string Log_ErrorWhileDisposingOfSnapshotSubscription {
            get {
                return ResourceManager.GetString("Log_ErrorWhileDisposingOfSnapshotSubscription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Started adapter {AdapterId}..
        /// </summary>
        internal static string Log_StartedAdapter {
            get {
                return ResourceManager.GetString("Log_StartedAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting adapter {AdapterId}..
        /// </summary>
        internal static string Log_StartingAdapter {
            get {
                return ResourceManager.GetString("Log_StartingAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopped adapter {AdapterId}..
        /// </summary>
        internal static string Log_StoppedAdapter {
            get {
                return ResourceManager.GetString("Log_StoppedAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stopping adapter {AdapterId} (disposing: false)..
        /// </summary>
        internal static string Log_StoppingAdapter {
            get {
                return ResourceManager.GetString("Log_StoppingAdapter", resourceCulture);
            }
        }
    }
}
