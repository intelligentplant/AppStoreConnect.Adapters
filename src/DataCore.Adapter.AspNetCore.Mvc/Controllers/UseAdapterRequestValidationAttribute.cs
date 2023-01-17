using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// Specifies if the controller should enable or disable adapter-level request validation for 
    /// its call context.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   All adapters that inherit from <see cref="AdapterCore"/> wrap their features in classes 
    ///   derived from <see cref="AdapterFeatureWrapper{TFeature}"/> that will validate request 
    ///   objects passed to the feature's methods.
    /// </para>
    /// 
    /// <para>
    ///   When a request object is received as route parameter, it has already been validated by 
    ///   the MVC framework before it reaches the adapter, so there is no need to re-validate. 
    /// </para>
    /// 
    /// <para>
    ///   If this filter is applied on a controller rather than an individual method, it may be 
    ///   required to re-enable validation. The most common use case for this is for HTTP GET 
    ///   routes that accept query string parameters to construct a request object that is then 
    ///   dispatched to the handler for the equivalent HTTP POST route. An example of this is in 
    ///   <see cref="TagSearchController"/>, where an HTTP GET route allows for tag searches where 
    ///   filters are specified as query string parameters, but the request itself is processed by 
    ///   the HTTP POST route that accepts a <see cref="Tags.FindTagsRequest"/> in the request 
    ///   body.
    /// </para>
    /// 
    /// <para>
    ///   Re-enabling adapter-level validation can be done by annotating the method in question 
    ///   with its own <see cref="UseAdapterRequestValidationAttribute"/>.
    /// </para>
    /// 
    /// </remarks>
    internal class UseAdapterRequestValidationAttribute : ActionFilterAttribute {

        /// <summary>
        /// Specifies if the calls to adapter features by the action require adapter-level request 
        /// validation.
        /// </summary>
        public bool RequireValidation { get; }


        /// <summary>
        /// Creates a new <see cref="UseAdapterRequestValidationAttribute"/> instance.
        /// </summary>
        /// <param name="requireValidation">
        ///   Specifies if the calls to adapter features by the action require adapter-level 
        ///   request validation.
        /// </param>
        public UseAdapterRequestValidationAttribute(bool requireValidation) {
            RequireValidation = requireValidation;
        }


        /// <inheritdoc/>
        public override void OnActionExecuting(ActionExecutingContext context) {
            base.OnActionExecuting(context);
            if (!RequireValidation && context.ModelState.IsValid && !context.HttpContext.Items.ContainsKey(AdapterCallContextExtensions.ValidateRequestsItemName)) {
                context.HttpContext.Items[AdapterCallContextExtensions.ValidateRequestsItemName] = false;
            }
        }

    }
}
