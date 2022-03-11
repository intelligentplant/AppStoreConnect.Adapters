using System;
using System.Linq;

using Json.Schema.Generation;

namespace DataCore.Adapter.Json.Schema {

    /// <summary>
    /// <see cref="IAttributeHandler"/> for attributes in the <see cref="System.ComponentModel"/> 
    /// and <see cref="System.ComponentModel.DataAnnotations"/> namespaces.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   The following attributes are supported:
    /// </para>
    /// 
    /// <list type="bullet">
    ///   <item>
    ///     <see cref="System.ComponentModel.DataAnnotations.DataTypeAttribute"/>
    ///   </item>
    ///   <item>
    ///     <see cref="System.ComponentModel.DescriptionAttribute"/>
    ///   </item>
    ///   <item>
    ///     <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>
    ///   </item>
    ///   <item>
    ///     <see cref="System.ComponentModel.DataAnnotations.MaxLengthAttribute"/>
    ///   </item>
    ///   <item>
    ///     <see cref="System.ComponentModel.DataAnnotations.MinLengthAttribute"/>
    ///   </item>
    ///   <item>
    ///     <see cref="System.ComponentModel.DataAnnotations.RangeAttribute"/>
    ///   </item>
    ///   <item>
    ///     <see cref="System.ComponentModel.DataAnnotations.RequiredAttribute"/>
    ///   </item>
    /// </list>
    /// 
    /// </remarks>
    public class DataAnnotationsAttributeHandler : IAttributeHandler {

        /// <inheritdoc/>
        public void AddConstraints(SchemaGeneratorContext context) {
            var dataTypeAttr = context.Attributes.OfType<System.ComponentModel.DataAnnotations.DataTypeAttribute>().FirstOrDefault();
            if (dataTypeAttr != null) {
                context.Intents.Add(new DataTypeFormatIntent(dataTypeAttr.DataType));
            }

            var descriptionAttr = context.Attributes.OfType<System.ComponentModel.DescriptionAttribute>().FirstOrDefault();
            if (descriptionAttr != null) {
                context.Intents.Add(new DisplayIntent(null, descriptionAttr.Description));
            }

            var displayAttr = context.Attributes.OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault();
            if (displayAttr != null) {
                context.Intents.Add(new DisplayIntent(displayAttr.GetName(), displayAttr.GetDescription()));
            }

            var maxLengthAttr = context.Attributes.OfType<System.ComponentModel.DataAnnotations.MaxLengthAttribute>().FirstOrDefault();
            if (maxLengthAttr != null) {
                context.Intents.Add(new MaxLengthIntent(maxLengthAttr.Length));
            }

            var minLengthAttr = context.Attributes.OfType<System.ComponentModel.DataAnnotations.MinLengthAttribute>().FirstOrDefault();
            if (minLengthAttr != null) {
                context.Intents.Add(new MinLengthIntent(minLengthAttr.Length));
            }

            var rangeAttr = context.Attributes.OfType<System.ComponentModel.DataAnnotations.RangeAttribute>().FirstOrDefault();
            if (rangeAttr != null) {
                context.Intents.Add(new RangeIntent(
                    rangeAttr.Minimum == null ? null : Convert.ToDecimal(rangeAttr.Minimum),
                    rangeAttr.Maximum == null ? null : Convert.ToDecimal(rangeAttr.Maximum)
                ));
            }

            var requiredAttr = context.Attributes.OfType<System.ComponentModel.DataAnnotations.RequiredAttribute>().FirstOrDefault();
            if (requiredAttr != null) {
                // required is handled differently by the schema generator. By adding a
                // RequiredAttribute to the context, the underlying ObjectSchemaGenerator
                // will mark this member as required.
                context.Attributes.Add(new RequiredAttribute());
            }
        }

    }
}
