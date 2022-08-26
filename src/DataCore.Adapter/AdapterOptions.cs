using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for runtime options for adapters deriving from <see cref="AdapterBase{TAdapterOptions}"/>.
    /// </summary>
    public class AdapterOptions {

        /// <summary>
        /// The adapter name. If <see langword="null"/> or white space, the adapter ID will be 
        /// used as the name.
        /// </summary>
        [MaxLength(
            AdapterConstants.MaxNameLength, 
            ErrorMessageResourceName = nameof(AdapterOptionsResources.AdapterOptions_Name_MaxLength_Error), 
            ErrorMessageResourceType = typeof(AdapterOptionsResources))
        ]
        [Display(
            ResourceType = typeof(AdapterOptionsResources), 
            Name = nameof(AdapterOptionsResources.AdapterOptions_Name_DisplayName),
            Description = nameof(AdapterOptionsResources.AdapterOptions_Name_Description),
            Order = 0
        )]
        public string? Name { get; set; }

        /// <summary>
        /// The adapter description.
        /// </summary>
        [MaxLength(
            AdapterConstants.MaxDescriptionLength,
            ErrorMessageResourceName = nameof(AdapterOptionsResources.AdapterOptions_Description_MaxLength_Error),
            ErrorMessageResourceType = typeof(AdapterOptionsResources))
        ]
        [Display(
            ResourceType = typeof(AdapterOptionsResources),
            Name = nameof(AdapterOptionsResources.AdapterOptions_Description_DisplayName),
            Description = nameof(AdapterOptionsResources.AdapterOptions_Description_Description),
            Order = 1
        )]
        public string? Description { get; set; }

        /// <summary>
        /// A flag indicating if the adapter is enabled or not.
        /// </summary>
        [Display(
            ResourceType = typeof(AdapterOptionsResources),
            Name = nameof(AdapterOptionsResources.AdapterOptions_IsEnabled_DisplayName),
            Description = nameof(AdapterOptionsResources.AdapterOptions_IsEnabled_Description),
            Order = 2
        )]
        [DefaultValue(true)]
        public bool IsEnabled { get; set; } = true;

    }
}
