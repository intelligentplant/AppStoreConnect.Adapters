using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// Utility class for extracting tag values from JSON.
    /// </summary>
    public static class JsonTagValueExtractor {

        /// <summary>
        /// Matches JSON property name references in tag name templates.
        /// </summary>
        private static readonly Regex s_tagNameTemplateMatcher = new Regex(@"\{(?<property>[^\}]+?)\}", RegexOptions.Singleline);


        /// <summary>
        /// Parses the specified JSON string and extracts tag values from the parsed object.
        /// </summary>
        /// <param name="json">
        ///   The JSON string. This must be either a JSON object, or an array of JSON objects.
        /// </param>
        /// <param name="options">
        ///   The options for the extraction.
        /// </param>
        /// <param name="serializerOptions">
        ///   The JSON serializer options to use when deserializing the <paramref name="json"/> 
        ///   string.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{TagValueQueryResult}"/> that will emit the parsed tag 
        ///   values.
        /// </returns>
        public static IEnumerable<TagValueQueryResult> GetTagValues(string json, JsonTagValueExtractorOptions? options = null, JsonSerializerOptions? serializerOptions = null) {
            var element = JsonSerializer.Deserialize<JsonElement>(json, serializerOptions);
            return GetTagValues(element, options);
        }


        /// <summary>
        /// Extracts tag values from the specified root JSON element.
        /// </summary>
        /// <param name="element">
        ///   The root JSON element. This must be a JSON object or an array of JSON objects.
        /// </param>
        /// <param name="options">
        ///   The options for extraction.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{TagValueQueryResult}"/> that will emit the parsed tag 
        ///   values.
        /// </returns>
        /// <seealso cref="JsonTagValueExtractorOptions"/>
        public static IEnumerable<TagValueQueryResult> GetTagValues(JsonElement element, JsonTagValueExtractorOptions? options = null) {
            options ??= new JsonTagValueExtractorOptions();

            if (element.ValueKind == JsonValueKind.Array) {
                foreach (var item in element.EnumerateArray()) {
                    foreach (var value in GetTagValues(item, options)) {
                        yield return value;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Object) {
                foreach (var value in ExtractTagValuesCore(element, options)) {
                    yield return value;
                }
            }
        }


        /// <summary>
        /// Internal starting point for extracting tag values from a JSON element.
        /// </summary>
        /// <param name="element">
        ///   The root JSON element.
        /// </param>
        /// <param name="options">
        ///   The options for extraction.
        /// </param>
        /// <returns>
        ///   The parsed tag values.
        /// </returns>
        private static IEnumerable<TagValueQueryResult> ExtractTagValuesCore(JsonElement element, JsonTagValueExtractorOptions options) {
            DateTimeOffset sampleTime;

            if (!TryGetTimestamp(element, options, out var timestampPropName, out sampleTime)) {
                sampleTime = DateTimeOffset.UtcNow;
            }

            return ExtractTagValuesCore(
                element, 
                sampleTime, 
                null, 
                element, 
                options.Template, 
                options.TemplateReplacements,
                options.ExcludeProperties == null
                    ? timestampPropName == null 
                        ? Array.Empty<string>() 
                        : new[] { timestampPropName }
                    : timestampPropName == null 
                        ? options.ExcludeProperties.ToArray() 
                        : options.ExcludeProperties.Concat(new[] { timestampPropName }).ToArray(),
                options.IncludeProperties?.ToArray() ?? Array.Empty<string>(),
                options.Recursive,
                options.PathSeparator
            );
        }


        /// <summary>
        /// Extracts tag values from a JSON element.
        /// </summary>
        /// <param name="nearestParentObject">
        ///   The closest JSON object to the <paramref name="value"/> that is being processed in 
        ///   this iteration.
        /// </param>
        /// <param name="sampleTime">
        ///   The timestamp to use for extracted tag values.
        /// </param>
        /// <param name="currentPropertyName">
        ///   
        /// </param>
        /// <param name="value">
        ///   The JSON value to process in this iteration.
        /// </param>
        /// <param name="tagNameTemplate">
        ///   The tag name template to use for the generated values.
        /// </param>
        /// <param name="templateReplacements">
        ///   The default replacements for <paramref name="tagNameTemplate"/>.
        /// </param>
        /// <param name="excludeProperties">
        ///   The JSON properties to skip.
        /// </param>
        /// <param name="includeProperties">
        ///   The JSON properties to explicitly ignore. If an empty array is specified, all 
        ///   properties will be included unless they are in the <paramref name="excludeProperties"/> 
        ///   list.
        /// </param>
        /// <param name="recursive">
        ///   Specifies if recursive mode is enabled.
        /// </param>
        /// <param name="pathSeparator">
        ///   The recorsive path separator to use.
        /// </param>
        /// <returns>
        ///   The extracted tag values.
        /// </returns>
        private static IEnumerable<TagValueQueryResult> ExtractTagValuesCore(
            JsonElement nearestParentObject,
            DateTimeOffset sampleTime, 
            string? currentPropertyName, 
            JsonElement value, 
            string tagNameTemplate,
            IDictionary<string, string>? templateReplacements,
            string[] excludeProperties,
            string[] includeProperties,
            bool recursive,
            string? pathSeparator
        ) {
            if (currentPropertyName != null) {
                // currentPropertyName should only be null for top-level objects!
                if (excludeProperties.Length > 0 && excludeProperties.Contains(currentPropertyName, StringComparer.Ordinal)) {
                    // Property is explicitly excluded.
                    yield break;
                }
                if (includeProperties.Length > 0 && !includeProperties.Contains(currentPropertyName, StringComparer.Ordinal)) {
                    // Property is not explicitly included.
                    yield break;
                }
            }

            if (recursive) {
                if (string.IsNullOrWhiteSpace(pathSeparator)) {
                    pathSeparator = "/";
                };

                switch (value.ValueKind) {
                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        var updatedTagNameTemplate = BuildTagNameFromTemplate(
                            nearestParentObject,
                            currentPropertyName,
                            tagNameTemplate,
                            templateReplacements
                        );

                        if (currentPropertyName != null) {
                            // Any instances of "{$prop}" will have been replaced. We will append
                            // the path separator and "{$prop}" to the end of the template so that
                            // each item on the object or array gets a distinct tag name.
                            updatedTagNameTemplate = updatedTagNameTemplate.EndsWith(pathSeparator, StringComparison.Ordinal)
                                ? string.Concat(updatedTagNameTemplate, "{$prop}")
                                : string.Concat(updatedTagNameTemplate, pathSeparator, "{$prop}");
                        }

                        if (value.ValueKind == JsonValueKind.Object) {
                            foreach (var item in value.EnumerateObject()) {
                                // value becomes the new nearest parent object
                                foreach (var val in ExtractTagValuesCore(
                                    value, 
                                    sampleTime, 
                                    item.Name, 
                                    item.Value, 
                                    updatedTagNameTemplate,
                                    templateReplacements,
                                    excludeProperties, 
                                    includeProperties, 
                                    recursive, 
                                    pathSeparator
                                )) {
                                    yield return val;
                                }
                            }
                        }
                        else {
                            var index = -1;
                            foreach (var item in value.EnumerateArray()) {
                                ++index;
                                foreach (var val in ExtractTagValuesCore(
                                    // If this array item is an object, it becomes the new nearest
                                    // parent object for the recursive call.
                                    item.ValueKind == JsonValueKind.Object ? item : nearestParentObject, 
                                    sampleTime, 
                                    index.ToString(), 
                                    item, 
                                    updatedTagNameTemplate,
                                    templateReplacements,
                                    excludeProperties, 
                                    includeProperties, 
                                    recursive, 
                                    pathSeparator
                                )) {
                                    yield return val;
                                }
                            }
                        }
                        
                        break;
                    default:
                        var tagName = BuildTagNameFromTemplate(nearestParentObject, currentPropertyName, tagNameTemplate, templateReplacements);
                        yield return BuildTagValueFromJsonValue(sampleTime, tagName, value);
                        break;
                }
            }
            else {
                if (value.Equals(nearestParentObject)) {
                    foreach (var item in value.EnumerateObject()) {
                        foreach (var val in ExtractTagValuesCore(nearestParentObject, sampleTime, item.Name, item.Value, tagNameTemplate, templateReplacements, excludeProperties, includeProperties, false, pathSeparator)) {
                            yield return val;
                        }
                    }
                }
                else {
                    var tagName = BuildTagNameFromTemplate(nearestParentObject, currentPropertyName, tagNameTemplate, templateReplacements);
                    yield return BuildTagValueFromJsonValue(sampleTime, tagName, value);
                }
            }
        }


        /// <summary>
        /// Tries to extract the timestamp for the tag values from the specified JSON object.
        /// </summary>
        /// <param name="element">
        ///   The JSON object.
        /// </param>
        /// <param name="options">
        ///   The extraction options.
        /// </param>
        /// <param name="name">
        ///   The name of the property that was identified as the timestamp property.
        /// </param>
        /// <param name="value">
        ///   The timestamp that was extracted.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the timestamp was successfully extracted, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        private static bool TryGetTimestamp(JsonElement element, JsonTagValueExtractorOptions options, out string? name, out DateTimeOffset value) {
            name = null;
            value = default;

            if (element.ValueKind != JsonValueKind.Object) {
                return false;
            }

            if (options.TimestampProperty != null) {
                if (element.TryGetProperty(options.TimestampProperty, out var prop)) {
                    name = options.TimestampProperty;
                    value = prop.GetDateTimeOffset();
                    return true;
                }
                return false;
            }

            foreach (var propName in new[] { "time", "Time", "TIME", "timestamp", "Timestamp", "TimeStamp", "TIMESTAMP" }) {
                if (element.TryGetProperty(propName, out var prop) && prop.TryGetDateTimeOffset(out var val)) {
                    name = propName;
                    value = val;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Builds a tag name from the specified template.
        /// </summary>
        /// <param name="parentObject">
        ///   The parent object to query for template replacement properties.
        /// </param>
        /// <param name="currentPropertyName">
        ///   The name of the property that the tag name is being generated for (i.e. the 
        ///   replacement value for the <c>{$prop}</c> placeholder).
        /// </param>
        /// <param name="template">
        ///   The tag name template.
        /// </param>
        /// <param name="defaultReplacements">
        ///   The default placeholder replacement values to use, if a referenced property does not 
        ///   exist on <paramref name="parentObject"/>.
        /// </param>
        /// <returns>
        ///   The generated tag name.
        /// </returns>
        private static string BuildTagNameFromTemplate(JsonElement parentObject, string? currentPropertyName, string template, IDictionary<string, string>? defaultReplacements) { 
            return s_tagNameTemplateMatcher.Replace(template, m => {
                var pName = m.Groups["property"].Value;

                if (string.Equals(pName, "$prop")) {
                    return currentPropertyName ?? m.Value;
                }

                if (parentObject.ValueKind == JsonValueKind.Object && parentObject.TryGetProperty(pName, out var prop)) {
                    if (prop.ValueKind == JsonValueKind.String) {
                        return prop.GetString();
                    }
                    else {
                        return prop.GetRawText();
                    }
                }

                if (defaultReplacements != null && defaultReplacements.TryGetValue(pName, out var replacement)) {
                    return replacement;
                }

                // No replacement available.
                return m.Value;
            });
        }


        /// <summary>
        /// Builds a tag value from the specified JSON value.
        /// </summary>
        /// <param name="sampleTime">
        ///   The sample time for the value.
        /// </param>
        /// <param name="tagName">
        ///   The tag name to use.
        /// </param>
        /// <param name="value">
        ///   The JSON value for the tag value.
        /// </param>
        /// <returns>
        ///   The generated <see cref="TagValueQueryResult"/>.
        /// </returns>
        private static TagValueQueryResult BuildTagValueFromJsonValue(
            DateTimeOffset sampleTime, 
            string tagName,
            JsonElement value
        ) {
            Variant val;

            switch (value.ValueKind) {
                case JsonValueKind.Number:
                    val = value.GetDouble();
                    break;
                case JsonValueKind.String:
                    val = value.GetString();
                    break;
                case JsonValueKind.True:
                    val = true;
                    break;
                case JsonValueKind.False:
                    val = false;
                    break;
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    val = value.GetRawText();
                    break;
                default:
                    val = Variant.Null;
                    break;
            }

            var sample = new TagValueExtended(sampleTime.UtcDateTime, val, TagValueStatus.Good, null, null, null, null);
            return new TagValueQueryResult(tagName, tagName, sample);
        }

    }


    /// <summary>
    /// Options for <see cref="JsonTagValueExtractor.GetTagValues(JsonElement, JsonTagValueExtractorOptions?)"/> 
    /// and <see cref="JsonTagValueExtractor.GetTagValues(string, JsonTagValueExtractorOptions?, JsonSerializerOptions?)"/>.
    /// </summary>
    public class JsonTagValueExtractorOptions {

        /// <summary>
        /// The template to use when generating tag names for extracted values.
        /// </summary>
        /// <remarks>
        ///   
        /// <para>
        ///   Templates can contain placholders, in the format <c>{property_name}</c>, where 
        ///   <c>property_name</c> is the name of a property on the JSON object that is being 
        ///   processed. The placeholder for the current property name being processed is 
        ///   <c>{$prop}</c>.
        /// </para>
        /// 
        /// <para>
        ///   For example, consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "deviceId": 1,
        ///   "temperature": 21.7,
        ///   "pressure": 1001.2
        /// }
        /// </code>
        /// 
        /// <para>
        ///   Given a <see cref="Template"/> value of <c>devices/{deviceId}/{$prop}</c>, the tag 
        ///   name generated for the <c>pressure</c> property will be <c>devices/1/pressure</c>.
        /// </para>
        /// 
        /// <para>
        ///   Use the <see cref="ExcludeProperties"/> property to ignore JSON properties that are 
        ///   not required or are used only for metadata purposes.
        /// </para>
        /// 
        /// </remarks>
        public string Template { get; set; } = "{$prop}";

        /// <summary>
        /// A dictionary of default <see cref="Template"/> replacements to use if a referenced 
        /// property is not present in the JSON object.
        /// </summary>
        /// <remarks>
        ///   Don't include the <c>{</c> and <c>}</c> in the dictionary keys (i.e. use <c>deviceId</c> 
        ///   and not <c>{deviceId}</c>).
        /// </remarks>
        public IDictionary<string, string>? TemplateReplacements;

        /// <summary>
        /// The property name that contains the timestamp to use for the extracted tag values.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   If no <see cref="TimestampProperty"/> is specified, the extractor will use one of 
        ///   the following properties (in order), if the property value can be converted to a 
        ///   <see cref="DateTimeOffset"/>:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>time</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>Time</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>TIME</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>timestamp</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>Timestamp</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>TimeStamp</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>TIMESTAMP</c></description>
        ///   </item>
        /// </list>
        /// 
        /// <para>
        ///   If no timestamp property can be found, <see cref="DateTimeOffset.UtcNow"/> will be 
        ///   used as the sample time.
        /// </para>
        /// 
        /// <para>
        ///   Note that the property that is identified as the timestamp property will be 
        ///   implicitly included in the <see cref="ExcludeProperties"/> list.
        /// </para>
        /// 
        /// </remarks>
        public string? TimestampProperty { get; set; }

        /// <summary>
        /// When a non-<see langword="null"/>, non-empty value is specified, JSON properties will 
        /// be skipped unless they are in this list.
        /// </summary>
        public IEnumerable<string>? IncludeProperties { get; set; }

        /// <summary>
        /// When a non-<see langword="null"/>, non-empty value is specified, JSON properties will 
        /// be skipped if they are in this list.
        /// </summary>
        public IEnumerable<string>? ExcludeProperties { get; set; }

        /// <summary>
        /// When <see langword="true"/>, JSON properties that contain other objects or arrays will 
        /// be processed recursively, instead of treating the properties as string values.
        /// </summary>
        /// <remarks>
        /// 
        /// <para>
        ///   <see cref="PathSeparator"/> is used to separate hierarchy levels when recursively 
        ///   processing objects.
        /// </para>
        /// 
        /// <para>
        ///   Consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "deviceId": 1,
        ///   "measurements": {
        ///     "temperature": 21.7,
        ///     "pressure": 1001.2
        ///   }
        /// }
        /// </code>
        /// 
        /// <para>
        ///   Given a tag name template of <c>devices/{deviceId}/{$prop}</c>, <see cref="ExcludeProperties"/> 
        ///   configured to include <c>deviceId</c>, recursive processing enabled, and a path 
        ///   separator of <c>/</c>, values for the following tags will be emitted:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>devices/1/measurements/temperature</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>devices/1/measurements/pressure</c></description>
        ///   </item>
        /// </list>
        /// 
        /// <para>
        ///   When processing an array rather than an object, the array index will be used as part 
        ///   of the tag name. For example, consider the following JSON:
        /// </para>
        /// 
        /// <code lang="JSON">
        /// {
        ///   "deviceId": 1,
        ///   "measurements": [
        ///     21.7,
        ///     1001.2
        ///   ]
        /// }
        /// </code>
        /// 
        /// <para>
        ///   Using the same options as the previous example, values for the following tags will 
        ///   be emitted:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>devices/1/measurements/0</c></description>
        ///   </item>
        ///   <item>
        ///     <description><c>devices/1/measurements/1</c></description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public bool Recursive { get; set; }

        /// <summary>
        /// When <see cref="Recursive"/> is <see langword="true"/>, <see cref="PathSeparator"/> is 
        /// used to separate hierarchy levels when processing nested objects and arrays.
        /// </summary>
        public string PathSeparator { get; set; } = "/";

    }

}
