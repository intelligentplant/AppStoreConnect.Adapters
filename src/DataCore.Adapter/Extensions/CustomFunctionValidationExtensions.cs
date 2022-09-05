using System;
using System.Text.Json;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Extensions related to validation of <see cref="CustomFunctionInvocationRequest"/> and 
    /// <see cref="CustomFunctionInvocationResponse"/>.
    /// </summary>
    public static class CustomFunctionValidationExtensions {

        /// <summary>
        /// Tries to validate the body of the <see cref="CustomFunctionInvocationRequest"/>.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="functionDescriptor">
        ///   The descriptor for the custom function.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <param name="validationResults">
        ///   The validation results for the request body.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the body of the <paramref name="request"/> is valid, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="functionDescriptor"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryValidateBody(this CustomFunctionInvocationRequest request, CustomFunctionDescriptorExtended functionDescriptor, JsonSerializerOptions? jsonOptions, out JsonElement validationResults) { 
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (functionDescriptor == null) {
                throw new ArgumentNullException(nameof(functionDescriptor));
            }

            return CustomFunctions.TryValidate(request.Body, functionDescriptor.RequestSchema, jsonOptions, out validationResults);
        }


        /// <summary>
        /// Tries to validate the body of the <see cref="CustomFunctionInvocationResponse"/>.
        /// </summary>
        /// <param name="response">
        ///   The response.
        /// </param>
        /// <param name="functionDescriptor">
        ///   The descriptor for the custom function.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <param name="validationResults">
        ///   The validation results for the response body.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the body of the <paramref name="response"/> is valid, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="response"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="functionDescriptor"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryValidateBody(this CustomFunctionInvocationResponse response, CustomFunctionDescriptorExtended functionDescriptor, JsonSerializerOptions? jsonOptions, out JsonElement validationResults) {
            if (response == null) {
                throw new ArgumentNullException(nameof(response));
            }
            if (functionDescriptor == null) {
                throw new ArgumentNullException(nameof(functionDescriptor));
            }

            return CustomFunctions.TryValidate(response.Body, functionDescriptor.ResponseSchema, jsonOptions, out validationResults);
        }

    }
}
