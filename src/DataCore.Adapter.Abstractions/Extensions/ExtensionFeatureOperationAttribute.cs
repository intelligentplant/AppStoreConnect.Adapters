using System;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Describes an extension feature operation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ExtensionFeatureOperationAttribute : Attribute {

        /// <summary>
        /// The localised display name.
        /// </summary>
        private readonly LocalizableString _name = new LocalizableString(nameof(Name));

        /// <summary>
        /// The localised description.
        /// </summary>
        private readonly LocalizableString _description = new LocalizableString(nameof(Description));

        /// <summary>
        /// The localised input parameter description.
        /// </summary>
        private readonly LocalizableString _inputParamDescription = new LocalizableString(nameof(InputParameterDescription));

        /// <summary>
        /// The localised output parameter description.
        /// </summary>
        private readonly LocalizableString _outputParamDescription = new LocalizableString(nameof(OutputParameterDescription));

        /// <summary>
        /// The resource type used to retrieved localised values for the display name, description,
        /// input parameter description, and output parameter description.
        /// </summary>
        private Type _resourceType;


        /// <summary>
        /// The type that contains the resources for the <see cref="Name"/> and <see cref="Description"/> properties.
        /// </summary>
        public Type ResourceType {
            get => _resourceType;
            set {
                if (_resourceType != value) {
                    _resourceType = value;

                    _name.ResourceType = value;
                    _description.ResourceType = value;
                }
            }
        }

        /// <summary>
        /// The display name for the operation.
        /// </summary>
        public string Name {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        /// The description for the operation.
        /// </summary>
        public string Description {
            get => _description.Value;
            set => _description.Value = value;
        }


        /// <summary>
        /// The description for the input parameter.
        /// </summary>
        public string InputParameterDescription {
            get => _inputParamDescription.Value;
            set => _inputParamDescription.Value = value;
        }


        /// <summary>
        /// The description for the output parameter.
        /// </summary>
        public string OutputParameterDescription {
            get => _outputParamDescription.Value;
            set => _outputParamDescription.Value = value;
        }


        /// <summary>
        /// Gets the display name for the operation. This can be either a literal string 
        /// specified by the <see cref="Name"/> property, or a localised string found when 
        /// <see cref="ResourceType"/> is specified and <see cref="Name"/> represents a resource 
        /// key within the resource type.
        /// </summary>
        /// <returns>
        ///   The display name for the operation.
        /// </returns>
        public string GetName() => _name.GetLocalizableValue();


        /// <summary>
        /// Gets the description for the operation. This can be either a literal string 
        /// specified by the <see cref="Description"/> property, or a localised string found when 
        /// <see cref="ResourceType"/> is specified and <see cref="Description"/> represents a 
        /// resource key within the resource type.
        /// </summary>
        /// <returns>
        ///   The description for the operation.
        /// </returns>
        public string GetDescription() => _description.GetLocalizableValue();


        /// <summary>
        /// Gets the input parameter description for the operation. This can be either a literal 
        /// string specified by the <see cref="InputParameterDescription"/> property, or a 
        /// localised string found when <see cref="ResourceType"/> is specified and 
        /// <see cref="InputParameterDescription"/> represents a resource key within the resource 
        /// type.
        /// </summary>
        /// <returns>
        ///   The input parameter description for the operation.
        /// </returns>
        public string GetInputParameterDescription() => _inputParamDescription.GetLocalizableValue();


        /// <summary>
        /// Gets the input parameter description for the operation. This can be either a literal 
        /// string specified by the <see cref="OutputParameterDescription"/> property, or a 
        /// localised string found when <see cref="ResourceType"/> is specified and 
        /// <see cref="OutputParameterDescription"/> represents a resource key within the resource 
        /// type.
        /// </summary>
        /// <returns>
        ///   The output parameter description for the operation.
        /// </returns>
        public string GetOutputParameterDescription() => _outputParamDescription.GetLocalizableValue();

    }
}
