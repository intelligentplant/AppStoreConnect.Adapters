using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MqttAdapter.TagHelpers {
    /// <summary>
    /// Modifies &lt;input&gt; and &lt;textarea&gt; elements with an <c>asp-placeholder-for</c> 
    /// attribute by setting the control's placeholder to be the display name or name of the 
    /// referenced model member.
    /// </summary>
    [HtmlTargetElement("input", Attributes = AttributeName)]
    [HtmlTargetElement("textarea", Attributes = AttributeName)]
    public class PlaceholderTagHelper : TagHelper {

        private const string AttributeName = "asp-placeholder-for";

        [HtmlAttributeName(AttributeName)]
        public ModelExpression For { get; set; } = default!;


        public override void Process(TagHelperContext context, TagHelperOutput output) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null) {
                throw new ArgumentNullException(nameof(output));
            }

            var placeholder = For.Metadata.DisplayName ?? For.Metadata.Name;
            if (!string.IsNullOrWhiteSpace(placeholder)) {
                output.Attributes.SetAttribute("placeholder", placeholder);
            }
        }
    }
}
