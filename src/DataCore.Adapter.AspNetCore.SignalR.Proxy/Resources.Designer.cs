﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {
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
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DataCore.Adapter.AspNetCore.SignalR.Proxy.Resources", typeof(Resources).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Proxy that communicates with a remote adapter via ASP.NET Core SignalR..
        /// </summary>
        public static string AdapterMetadata_Description {
            get {
                return ResourceManager.GetString("AdapterMetadata_Description", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignalR Proxy.
        /// </summary>
        public static string AdapterMetadata_DisplayName {
            get {
                return ResourceManager.GetString("AdapterMetadata_DisplayName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter ID is required..
        /// </summary>
        public static string Error_AdapterIdIsRequired {
            get {
                return ResourceManager.GetString("Error_AdapterIdIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connection factory is required..
        /// </summary>
        public static string Error_ConnectionFactoryIsRequired {
            get {
                return ResourceManager.GetString("Error_ConnectionFactoryIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignalR Connection.
        /// </summary>
        public static string HealthCheck_DisplayName_Connection {
            get {
                return ResourceManager.GetString("HealthCheck_DisplayName_Connection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remote Adapter Health.
        /// </summary>
        public static string HealthCheck_DisplayName_RemoteAdapter {
            get {
                return ResourceManager.GetString("HealthCheck_DisplayName_RemoteAdapter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignalR hub connection status is {0}..
        /// </summary>
        public static string HealthCheck_ExtensionHubConnectionStatusDescription {
            get {
                return ResourceManager.GetString("HealthCheck_ExtensionHubConnectionStatusDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignalR hub connection status is {0}. See inner results for remote adapter health..
        /// </summary>
        public static string HealthCheck_HubConnectionStatusDescription {
            get {
                return ResourceManager.GetString("HealthCheck_HubConnectionStatusDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignalR hub connection status is {0}..
        /// </summary>
        public static string HealthCheck_HubConnectionStatusDescriptionNoInnerResults {
            get {
                return ResourceManager.GetString("HealthCheck_HubConnectionStatusDescriptionNoInnerResults", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The remote adapter does not implement health checks..
        /// </summary>
        public static string HealthCheck_RemoteAdapterHealthNotSupported {
            get {
                return ResourceManager.GetString("HealthCheck_RemoteAdapterHealthNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown.
        /// </summary>
        public static string HealthCheck_UnknownConnectionState {
            get {
                return ResourceManager.GetString("HealthCheck_UnknownConnectionState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SignalR Connection ({0}).
        /// </summary>
        public static string HeathCheck_DisplayName_ExtensionConnection {
            get {
                return ResourceManager.GetString("HeathCheck_DisplayName_ExtensionConnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while running events subscription..
        /// </summary>
        public static string Log_EventsSubscriptionError {
            get {
                return ResourceManager.GetString("Log_EventsSubscriptionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while registering an extension feature implementation: {0}.
        /// </summary>
        public static string Log_ExtensionFeatureRegistrationError {
            get {
                return ResourceManager.GetString("Log_ExtensionFeatureRegistrationError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No implementation available for extension feature: {0}.
        /// </summary>
        public static string Log_NoExtensionImplementationAvailable {
            get {
                return ResourceManager.GetString("Log_NoExtensionImplementationAvailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error while running snapshot tag value subscription..
        /// </summary>
        public static string Log_SnapshotTagValueSubscriptionError {
            get {
                return ResourceManager.GetString("Log_SnapshotTagValueSubscriptionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while disposing subscriptions..
        /// </summary>
        public static string Log_SubscriptionDisposeError {
            get {
                return ResourceManager.GetString("Log_SubscriptionDisposeError", resourceCulture);
            }
        }
    }
}
