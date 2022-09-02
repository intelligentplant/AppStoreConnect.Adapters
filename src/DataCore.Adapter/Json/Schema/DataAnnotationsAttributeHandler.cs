using System;

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
        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DataAnnotations.DataTypeAttribute dataTypeAttr) {
                context.Intents.Add(new DataTypeFormatIntent(dataTypeAttr.DataType));
            }

            if (attribute is System.ComponentModel.DescriptionAttribute descriptionAttr) {
                context.Intents.Add(new DisplayIntent(null, descriptionAttr.Description));
            }

            if (attribute is System.ComponentModel.DataAnnotations.DisplayAttribute displayAttr) {
                context.Intents.Add(new DisplayIntent(displayAttr.GetName(), displayAttr.GetDescription()));
            }

            if (attribute is System.ComponentModel.DataAnnotations.MaxLengthAttribute maxLengthAttr) {
                context.Intents.Add(new MaxLengthIntent(maxLengthAttr.Length));
            }

            if (attribute is System.ComponentModel.DataAnnotations.MinLengthAttribute minLengthAttr) {
                context.Intents.Add(new MinLengthIntent(minLengthAttr.Length));
            }

            if (attribute is System.ComponentModel.DataAnnotations.RangeAttribute rangeAttr) {
                context.Intents.Add(new RangeIntent(
                    rangeAttr.Minimum == null ? null : Convert.ToDecimal(rangeAttr.Minimum),
                    rangeAttr.Maximum == null ? null : Convert.ToDecimal(rangeAttr.Maximum)
                ));
            }

            if (attribute is System.ComponentModel.DataAnnotations.RequiredAttribute && context is MemberGenerationContext memberGenerationContext) {
                // required is handled differently by the schema generator. By adding a
                // RequiredAttribute to the context, the underlying generator
                // will mark this member as required.

                memberGenerationContext.Attributes.Add(new RequiredAttribute());
            }
        }
    }
}
