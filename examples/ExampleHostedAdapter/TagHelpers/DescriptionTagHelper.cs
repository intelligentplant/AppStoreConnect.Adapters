using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ExampleHostedAdapter.TagHelpers {

    /// <summary>
    /// Modifies &lt;span&gt; and &lt;p&gt; elements with an <c>asp-description-for</c> attribute 
    /// by setting the element's content to be the description for the referenced model member.
    /// </summary>
    [HtmlTargetElement("span", Attributes = AttributeName)]
    [HtmlTargetElement("p", Attributes = AttributeName)]
    public class DescriptionTagHelper : TagHelper {

        private const string AttributeName = "asp-description-for";

        [HtmlAttributeName(AttributeName)]
        public ModelExpression For { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null) {
                throw new ArgumentNullException(nameof(output));
            }

            if (!output.IsContentModified) {
                output.Content.SetContent(For.Metadata.Description);
            }
        }
    }
}
