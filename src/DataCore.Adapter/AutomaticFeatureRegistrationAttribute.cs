using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Adapters that are derived from <see cref="AdapterBase{TAdapterOptions}"/> can be annotated 
    /// with this attribute to control if adapter features implemented directly by the adapter are 
    /// automatically added to the adapter's <see cref="IAdapter.Features"/> collection by default.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   The default behaviour of <see cref="AdapterBase{TAdapterOptions}"/> is to automatically 
    ///   register all adapter features that are implemented on the adapter class. This behaviour 
    ///   is inherited by derived types unless the derived type is annotated with its own 
    ///   <see cref="AutomaticFeatureRegistrationAttribute"/> annotation that changes the 
    ///   inherited behaviour. For example, if adapter class <c>AdapterA</c> is derived from 
    ///   <see cref="AdapterBase{TAdapterOptions}"/> and is annotated with an <see cref="AutomaticFeatureRegistrationAttribute"/> 
    ///   that disables automatic feature registration, adapters derived from <c>AdapterA</c> will 
    ///   also have automatic feature registration disabled by default.
    /// </para>
    /// 
    /// <para>
    ///   If automatic feature registration is disabled, it is the responsibility of the adapter 
    ///   to add appropriate features to its <see cref="IAdapter.Features"/> collection.
    /// </para>
    /// 
    /// <para>
    ///   The <see cref="Diagnostics.IHealthCheck"/> feature provided by <see cref="AdapterBase{TAdapterOptions}"/> 
    ///   will always be registered, regardless of whether or not automatic feature registration 
    ///   is enabled.
    /// </para>
    /// 
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AutomaticFeatureRegistrationAttribute : Attribute {
    
        /// <summary>
        /// Specifies if automatic feature registration is enabled for the adapter.
        /// </summary>
        public bool IsEnabled { get; }


        /// <summary>
        /// Creates a new <see cref="AutomaticFeatureRegistrationAttribute"/> object.
        /// </summary>
        /// <param name="enabled">
        ///   <see langword="true"/> to enable automatic feature registration for the adapter, or 
        ///   <see langword="false"/> if the adapter will manually register all features.
        /// </param>
        /// <remarks>
        ///   The <see cref="Diagnostics.IHealthCheck"/> feature provided by <see cref="AdapterBase{TAdapterOptions}"/> 
        ///   will always be registered, regardless of whether or not automatic feature 
        ///   registration is enabled.
        /// </remarks>
        public AutomaticFeatureRegistrationAttribute(bool enabled = true) {
            IsEnabled = enabled;
        }
    
    }
}
