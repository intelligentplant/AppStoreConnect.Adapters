using System;

using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace DataCore.Adapter.Json.Schema {

    internal class DataTypeAttributeHandler : IAttributeHandler<System.ComponentModel.DataAnnotations.DataTypeAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DataAnnotations.DataTypeAttribute dataTypeAttr) {
                context.Intents.Add(new DataTypeFormatIntent(dataTypeAttr.DataType));
            }
        }

    }


    internal class DescriptionAttributeHandler : IAttributeHandler<System.ComponentModel.DescriptionAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DescriptionAttribute descriptionAttr) {
                context.Intents.Add(new DisplayIntent(null, descriptionAttr.Description));
            }
        }

    }


    internal class DisplayAttributeHandler : IAttributeHandler<System.ComponentModel.DataAnnotations.DisplayAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DataAnnotations.DisplayAttribute displayAttr) {
                context.Intents.Add(new DisplayIntent(displayAttr.GetName(), displayAttr.GetDescription()));
            }
        }

    }


    internal class DisplayNameAttributeHandler : IAttributeHandler<System.ComponentModel.DisplayNameAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DisplayNameAttribute displayNameAttr) {
                context.Intents.Add(new DisplayIntent(displayNameAttr.DisplayName, null));
            }
        }

    }


    internal class MaxLengthAttributeHandler : IAttributeHandler<System.ComponentModel.DataAnnotations.MaxLengthAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DataAnnotations.MaxLengthAttribute maxLengthAttr) {
                context.Intents.Add(new MaxLengthIntent(maxLengthAttr.Length));
            }
        }

    }


    internal class MinLengthAttributeHandler : IAttributeHandler<System.ComponentModel.DataAnnotations.MinLengthAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DataAnnotations.MinLengthAttribute minLengthAttr) {
                context.Intents.Add(new MinLengthIntent(minLengthAttr.Length));
            }
        }

    }


    internal class RangeAttributeHandler : IAttributeHandler<System.ComponentModel.DataAnnotations.RangeAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DataAnnotations.RangeAttribute rangeAttr) {
                context.Intents.Add(new RangeIntent(
                    rangeAttr.Minimum == null ? null : Convert.ToDecimal(rangeAttr.Minimum),
                    rangeAttr.Maximum == null ? null : Convert.ToDecimal(rangeAttr.Maximum)
                ));
            }
        }

    }


    internal class RegularExpressionAttributeHandler : IAttributeHandler<System.ComponentModel.DataAnnotations.RegularExpressionAttribute> {

        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (attribute is System.ComponentModel.DataAnnotations.RegularExpressionAttribute regexAttr) {
                context.Intents.Add(new PatternIntent(regexAttr.Pattern));
            }
        }

    }


    internal class RequiredAttributeHandler : IAttributeHandler<System.ComponentModel.DataAnnotations.RequiredAttribute> {
        
        public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute) {
            if (context is MemberGenerationContext memberGenerationContext) {
                memberGenerationContext.Attributes.Add(new RequiredAttribute());
            }
        }

    }

}
