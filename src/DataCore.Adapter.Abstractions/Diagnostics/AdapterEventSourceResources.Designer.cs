﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataCore.Adapter.Diagnostics {
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
    internal class AdapterEventSourceResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal AdapterEventSourceResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("DataCore.Adapter.Diagnostics.AdapterEventSourceResources", typeof(AdapterEventSourceResources).Assembly);
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
        ///   Looks up a localized string similar to Adapter &apos;{0}&apos; was disposed..
        /// </summary>
        internal static string event_AdapterDisposed {
            get {
                return ResourceManager.GetString("event_AdapterDisposed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation completed on adapter &apos;{0}&apos;: {1}. Elapsed time: {2} ms.
        /// </summary>
        internal static string event_AdapterOperationCompleted {
            get {
                return ResourceManager.GetString("event_AdapterOperationCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation faulted on adapter &apos;{0}&apos;: {1}. Elapsed time: {2} ms. Error: {3}.
        /// </summary>
        internal static string event_AdapterOperationFaulted {
            get {
                return ResourceManager.GetString("event_AdapterOperationFaulted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation started on adapter &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string event_AdapterOperationStarted {
            get {
                return ResourceManager.GetString("event_AdapterOperationStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter &apos;{0}&apos; was started..
        /// </summary>
        internal static string event_AdapterStarted {
            get {
                return ResourceManager.GetString("event_AdapterStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter &apos;{0}&apos; was stopped..
        /// </summary>
        internal static string event_AdapterStopped {
            get {
                return ResourceManager.GetString("event_AdapterStopped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation received a client streaming item on adapter &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string event_AdapterStreamItemIn {
            get {
                return ResourceManager.GetString("event_AdapterStreamItemIn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Operation emitted a server streaming item on adapter &apos;{0}&apos;: {1}.
        /// </summary>
        internal static string event_AdapterStreamItemOut {
            get {
                return ResourceManager.GetString("event_AdapterStreamItemOut", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Adapter &apos;{0}&apos; was updated..
        /// </summary>
        internal static string event_AdapterUpdated {
            get {
                return ResourceManager.GetString("event_AdapterUpdated", resourceCulture);
            }
        }
    }
}
