using System.Text.Json;
using System.Threading;

using JsonSchema = Json.Schema;
using Json.Schema.Generation;
using Json.Schema;

namespace DataCore.Adapter.Json.Schema {

    /// <summary>
    /// Utility functions for JSON schema generation and validation.
    /// </summary>
    public static class JsonSchemaUtility {

        /// <summary>
        /// Flags if JSON schema extensions have been registered.
        /// </summary>
        private static int s_extensionsRegistered;


        /// <summary>
        /// Registers JSON schema extensions.
        /// </summary>
        internal static void RegisterExtensions() {
            if (Interlocked.CompareExchange(ref s_extensionsRegistered, 1, 0) != 0) {
                // Already registered.
                return;
            }

            GeneratorRegistry.Register(new TimeSpanSchemaGenerator());

            AttributeHandler.AddHandler<DataTypeAttributeHandler>();
            AttributeHandler.AddHandler<DescriptionAttributeHandler>();
            AttributeHandler.AddHandler<DisplayAttributeHandler>();
            AttributeHandler.AddHandler<DisplayNameAttributeHandler>();
            AttributeHandler.AddHandler<MinLengthAttributeHandler>();
            AttributeHandler.AddHandler<MaxLengthAttributeHandler>();
            AttributeHandler.AddHandler<RangeAttributeHandler>();
            AttributeHandler.AddHandler<RegularExpressionAttributeHandler>();
            AttributeHandler.AddHandler<RequiredAttributeHandler>();
        }


        /// <summary>
        /// Creates a JSON schema for the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to generate a JSON schema for.
        /// </typeparam>
        /// <returns>
        ///   The JSON schema, represented as a <see cref="JsonElement"/>.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   Schema generation is performed using <see cref="JsonSchema.JsonSchemaBuilder"/>. 
        ///   Attributes can be specified on properties to customise the generated schema. In 
        ///   addition to the attributes in the <see cref="JsonSchema.Generation"/> namespace, 
        ///   the following attributes from the <see cref="System.ComponentModel.DataAnnotations"/> 
        ///   namespace can also be used:
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
        ///     <see cref="System.ComponentModel.DisplayNameAttribute"/>
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
        ///     <see cref="System.ComponentModel.DataAnnotations.RegularExpressionAttribute"/>
        ///   </item>
        ///   <item>
        ///     <see cref="System.ComponentModel.DataAnnotations.RequiredAttribute"/>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public static JsonElement CreateJsonSchema<T>(JsonSerializerOptions? options = null) {
            RegisterExtensions();
            var builder = new JsonSchemaBuilder().FromType<T>(new SchemaGeneratorConfiguration() {
                PropertyNameResolver = member => options?.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name
            });

            return JsonSerializer.SerializeToElement(builder.Build(), options);
        }


        /// <summary>
        /// Tries to validate the specified JSON document against a schema.
        /// </summary>
        /// <param name="data">
        ///   The JSON data to validate.
        /// </param>
        /// <param name="schema">
        ///   The schema to validate the <paramref name="data"/> against.
        /// </param>
        /// <param name="jsonOptions">
        ///   The <see cref="JsonSerializerOptions"/> to use.
        /// </param>
        /// <param name="validationResults">
        ///   The validation results.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="data"/> was successfully validated 
        ///   against the <paramref name="schema"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryValidate(JsonElement data, JsonElement schema, JsonSerializerOptions? jsonOptions, out JsonElement validationResults) {
            var jsonSchema = JsonSchema.JsonSchema.FromText(JsonSerializer.Serialize(schema, jsonOptions));
            var result = jsonSchema.Evaluate(data, new EvaluationOptions() { 
                OutputFormat = OutputFormat.Hierarchical
            });

            validationResults = JsonSerializer.SerializeToElement(result, jsonOptions);
            return result.IsValid;
        }

    }
}
