﻿using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Helper class for constructing <see cref="TagDefinition"/> objects using a fluent interface.
    /// </summary>
    public sealed class TagDefinitionBuilder : AdapterEntityBuilder<TagDefinition> {

        /// <summary>
        /// The tag ID.
        /// </summary>
        private string? _id;

        /// <summary>
        /// The tag name.
        /// </summary>
        private string? _name;

        /// <summary>
        /// The tag description.
        /// </summary>
        private string? _description;

        /// <summary>
        /// The tag units.
        /// </summary>
        private string? _units;

        /// <summary>
        /// The tag data type.
        /// </summary>
        private VariantType _dataType;

        /// <summary>
        /// The digital states for the tag.
        /// </summary>
        private readonly List<DigitalState> _states = new List<DigitalState>();

        /// <summary>
        /// The adapter features that can be used to read from or write to the tag.
        /// </summary>
        private readonly HashSet<Uri> _supportedFeatures = new HashSet<Uri>(new UriComparer());

        /// <summary>
        /// The tag labels.
        /// </summary>
        private readonly List<string> _labels = new List<string>();


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object. You must call <see cref="WithId"/> 
        /// and <see cref="WithName"/> to set the tag ID and name respectively before calling <see cref="Build"/>.
        /// </summary>
        public TagDefinitionBuilder() { }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object that is configured to use the 
        /// specified tag ID and tag name.
        /// </summary>
        /// <param name="id">
        ///   The tag ID.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinitionBuilder"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagDefinitionBuilder (string id, string name) {
            WithId(id);
            WithName(name);
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object that is configured to use the 
        /// specified value as both the tag ID and name.
        /// </summary>
        /// <param name="identifier">
        ///   The identifier to use as both the ID and name of the tag.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinitionBuilder"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="identifier"/> is <see langword="null"/>.
        /// </exception>
        public TagDefinitionBuilder(string identifier) {
            WithId(identifier);
            WithName(identifier);
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object that is initialised from an 
        /// existing tag definition.
        /// </summary>
        /// <param name="existing">
        ///   The existing tag definition.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        public TagDefinitionBuilder(TagDefinition existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            WithId(existing.Id);
            WithName(existing.Name);
            WithDescription(existing.Description);
            WithUnits(existing.Units);
            WithDataType(existing.DataType);
            WithDigitalStates(existing.States);
            WithSupportedFeatures(existing.SupportedFeatures);
            this.WithProperties(existing.Properties);
            WithLabels(existing.Labels);
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object. You must call <see cref="WithId"/> 
        /// and <see cref="WithName"/> to set the tag ID and name respectively before calling <see cref="Build"/>.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagDefinitionBuilder"/> object.
        /// </returns>
        [Obsolete("This method will be removed in a future release. Use TagDefinitionBuilder() instead.", false)]
        public static TagDefinitionBuilder Create() {
            return new TagDefinitionBuilder();
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object that is configured to use the 
        /// specified tag ID and tag name.
        /// </summary>
        /// <param name="id">
        ///   The tag ID.
        /// </param>
        /// <param name="name">
        ///   The tag name.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinitionBuilder"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        [Obsolete("This method will be removed in a future release. Use TagDefinitionBuilder(string, string) instead.", false)]
        public static TagDefinitionBuilder Create(string id, string name) {
            return new TagDefinitionBuilder()
                .WithId(id)
                .WithName(name);
        }


        /// <summary>
        /// Creates a new <see cref="TagDefinitionBuilder"/> object that is initialised from an 
        /// existing tag definition.
        /// </summary>
        /// <param name="existing">
        ///   The existing tag definition.
        /// </param>
        /// <returns>
        ///   A new <see cref="TagDefinitionBuilder"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="existing"/> is <see langword="null"/>.
        /// </exception>
        [Obsolete("This method will be removed in a future release. Use TagDefinitionBuilder(TagDefinition) instead.", false)]
        public static TagDefinitionBuilder CreateFromExisting(TagDefinition existing) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }

            return new TagDefinitionBuilder(existing);
        }


        /// <summary>
        /// Creates a <see cref="TagDefinition"/> using the configured settings.
        /// </summary>
        /// <returns>
        ///   A new <see cref="TagDefinition"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   The tag ID has not yet been set.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The tag name has not yet been set.
        /// </exception>
        public override TagDefinition Build() {
            return new TagDefinition(_id!, _name!, _description, _units, _dataType, _states, _supportedFeatures, GetProperties(), _labels);
        }


        /// <summary>
        /// Updates the ID for the tag.
        /// </summary>
        /// <param name="id">
        ///   The ID.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public TagDefinitionBuilder WithId(string id) {
            _id = id ?? throw new ArgumentNullException(nameof(id));
            return this;
        }


        /// <summary>
        /// Updates the name for the tag.
        /// </summary>
        /// <param name="name">
        ///   The name.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        public TagDefinitionBuilder WithName(string name) {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            return this;
        }


        /// <summary>
        /// Updates the description for the tag.
        /// </summary>
        /// <param name="description">
        ///   The description.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDescription(string? description) {
            _description = description;
            return this;
        }


        /// <summary>
        /// Updates the units for the tag.
        /// </summary>
        /// <param name="units">
        ///   The units.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithUnits(string? units) {
            _units = units;
            return this;
        }


        /// <summary>
        /// Updates the data type for the tag.
        /// </summary>
        /// <param name="dataType">
        ///   The data type.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDataType(VariantType dataType) {
            _dataType = dataType;
            return this;
        }


        /// <summary>
        /// Adds digital states to the tag.
        /// </summary>
        /// <param name="states">
        ///   The digital states.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDigitalStates(params DigitalState[] states) {
            return WithDigitalStates((IEnumerable<DigitalState>) states);
        }


        /// <summary>
        /// Adds digital states to the tag.
        /// </summary>
        /// <param name="states">
        ///   The digital states.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDigitalStates(IEnumerable<DigitalState>? states) {
            if (states != null) {
                _states.AddRange(states.Where(x => x != null).Select(x => new DigitalState(x.Name, x.Value)));
            }
            return this;
        }


        /// <summary>
        /// Adds digital states to the tag from a <see cref="DigitalStateSet"/>.
        /// </summary>
        /// <param name="stateSet">
        ///   The state set containing the digital state definitions to add.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithDigitalStates(DigitalStateSet? stateSet) {
            return WithDigitalStates(stateSet?.States);
        }


        /// <summary>
        /// Removes all digital states from the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder ClearDigitalStates() {
            _states.Clear();
            return this;
        }


        /// <summary>
        /// Adds supported features to the tag.
        /// </summary>
        /// <param name="uris">
        ///   The feature URIs.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportedFeatures(params Uri[] uris) {
            return WithSupportedFeatures((IEnumerable<Uri>) uris);
        }


        /// <summary>
        /// Adds supported features to the tag.
        /// </summary>
        /// <param name="uris">
        ///   The feature URIs.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportedFeatures(IEnumerable<Uri>? uris) {
            if (uris != null) {
                foreach (var uri in uris) {
                    if (uri != null && (uri.IsStandardFeatureUri() || uri.IsExtensionFeatureUri())) {
                        _supportedFeatures.Add(uri.EnsurePathHasTrailingSlash());
                    }
                }
            }

            return this;
        }


        /// <summary>
        /// Adds supported features to the tag.
        /// </summary>
        /// <param name="uriStrings">
        ///   The feature URIs.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportedFeatures(params string[] uriStrings) {
            return WithSupportedFeatures((IEnumerable<string>) uriStrings);
        }


        /// <summary>
        /// Adds supported features to the tag.
        /// </summary>
        /// <param name="uriStrings">
        ///   The feature URIs.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportedFeatures(IEnumerable<string>? uriStrings) {
            if (uriStrings != null) {
                foreach (var uriString in uriStrings) {
                    if (Uri.TryCreate(uriString, UriKind.Absolute, out var uri)) {
                        WithSupportedFeatures(uri);
                    }
                }
            }

            return this;
        }


        /// <summary>
        /// Adds a supported feature to the tag.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="hashFragment">
        ///   The hash fragment to add to the URI of the feature type. For example, if the <see cref="RealTimeData.IReadProcessedTagValues"/> 
        ///   feature is available on the adapter, but the tag only supports a subset of the 
        ///   available data functions, you can add the feature once for each supported data 
        ///   function and specify the ID of the data function as the hash fragment each time.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportedFeature<TFeature>(string? hashFragment = null) where TFeature : IAdapterFeature {
            foreach (var uri in typeof(TFeature).GetAdapterFeatureUris()) {
                var featureUri = string.IsNullOrWhiteSpace(hashFragment)
                    ? uri.EnsurePathHasTrailingSlash()
                    : new Uri(string.Concat(uri.ToString(), "#", Uri.EscapeDataString(hashFragment))).EnsurePathHasTrailingSlash();
                _supportedFeatures.Add(featureUri);
            }

            return this;
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.ReadAnnotations"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsReadAnnotations() {
            return WithSupportedFeature<RealTimeData.IReadTagValueAnnotations>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.ReadPlotTagValues"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsReadPlotValues() {
            return WithSupportedFeature<RealTimeData.IReadPlotTagValues>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <param name="dataFunctions">
        ///   The data functions supported by the tag. In addition to adding <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> 
        ///   to the supported features for the tag, an additional entry will be added for each 
        ///   item in the <paramref name="dataFunctions"/> that appends the data function ID to 
        ///   the base <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> URI as a 
        ///   hash fragment.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsReadProcessedValues(params RealTimeData.DataFunctionDescriptor[] dataFunctions) {
            return WithSupportsReadProcessedValues((IEnumerable<RealTimeData.DataFunctionDescriptor>) dataFunctions);
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <param name="dataFunctions">
        ///   The data functions supported by the tag. In addition to adding <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> 
        ///   to the supported features for the tag, an additional entry will be added for each 
        ///   item in the <paramref name="dataFunctions"/> that appends the data function ID to 
        ///   the base <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> URI as a 
        ///   hash fragment.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsReadProcessedValues(IEnumerable<RealTimeData.DataFunctionDescriptor>? dataFunctions) {
            WithSupportedFeature<RealTimeData.IReadProcessedTagValues>();

            if (dataFunctions != null) {
                foreach (var dataFunction in dataFunctions) {
                    if (dataFunction == null) {
                        continue;
                    }
                    WithSupportedFeature<RealTimeData.IReadProcessedTagValues>(dataFunction.Id);
                }
            }

            return this;
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.ReadRawTagValues"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsReadRawValues() {
            return WithSupportedFeature<RealTimeData.IReadRawTagValues>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.ReadSnapshotTagValues"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsReadSnapshotValues() {
            return WithSupportedFeature<RealTimeData.IReadSnapshotTagValues>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.ReadTagValuesAtTimes"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsReadValuesAtTimes() {
            return WithSupportedFeature<RealTimeData.IReadTagValuesAtTimes>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.SnapshotTagValuePush"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsSnapshotValuePush() {
            return WithSupportedFeature<RealTimeData.ISnapshotTagValuePush>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.WriteAnnotations"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsWriteAnnotations() {
            return WithSupportedFeature<RealTimeData.IWriteTagValueAnnotations>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.WriteHistoricalTagValues"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsWriteHistoricalValues() {
            return WithSupportedFeature<RealTimeData.IWriteHistoricalTagValues>();
        }


        /// <summary>
        /// Adds <see cref="WellKnownFeatures.RealTimeData.WriteSnapshotTagValues"/> to the supported 
        /// features for the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithSupportsWriteSnapshotValues() {
            return WithSupportedFeature<RealTimeData.IWriteSnapshotTagValues>();
        }


        /// <summary>
        /// Adds supported features to the tag based on the features implemented by the specified 
        /// adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions that can be used with the tag, if the <paramref name="adapter"/> 
        ///   supports the <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> 
        ///   feature. Each specified data function will result in an additional entry in the 
        ///   supported features list for the tag, with the function ID appended to the 
        ///   <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> URI as the hash 
        ///   fragment.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        /// <remarks>
        /// The following feature URIs will be added to the tag, if implemented by the <paramref name="adapter"/>:
        ///   
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadAnnotations"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadPlotTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadRawTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadSnapshotTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadTagValuesAtTimes"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.SnapshotTagValuePush"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.WriteAnnotations"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.WriteHistoricalTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.WriteSnapshotTagValues"/></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public TagDefinitionBuilder WithSupportedFeatures(IAdapter adapter, params RealTimeData.DataFunctionDescriptor[] dataFunctions) {
            return WithSupportedFeatures(adapter, (IEnumerable<RealTimeData.DataFunctionDescriptor>) dataFunctions);
        }


        /// <summary>
        /// Adds supported features to the tag based on the features implemented by the specified 
        /// adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="dataFunctions">
        ///   The data functions that can be used with the tag, if the <paramref name="adapter"/> 
        ///   supports the <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> 
        ///   feature. Each specified data function will result in an additional entry in the 
        ///   supported features list for the tag, with the function ID appended to the 
        ///   <see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/> URI as the hash 
        ///   fragment.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        /// <remarks>
        /// The following feature URIs will be added to the tag, if implemented by the <paramref name="adapter"/>:
        ///   
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadAnnotations"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadPlotTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadProcessedTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadRawTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadSnapshotTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.ReadTagValuesAtTimes"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.SnapshotTagValuePush"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.WriteAnnotations"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.WriteHistoricalTagValues"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="WellKnownFeatures.RealTimeData.WriteSnapshotTagValues"/></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public TagDefinitionBuilder WithSupportedFeatures(IAdapter adapter, IEnumerable<RealTimeData.DataFunctionDescriptor>? dataFunctions) {
            if (adapter != null) {
                if (adapter.HasFeature<RealTimeData.IReadTagValueAnnotations>()) {
                    WithSupportsReadAnnotations();
                }
                if (adapter.HasFeature<RealTimeData.IReadPlotTagValues>()) {
                    WithSupportsReadPlotValues();
                }
                if (adapter.HasFeature<RealTimeData.IReadProcessedTagValues>()) {
                    WithSupportsReadProcessedValues(dataFunctions);
                }
                if (adapter.HasFeature<RealTimeData.IReadRawTagValues>()) {
                    WithSupportsReadRawValues();
                }
                if (adapter.HasFeature<RealTimeData.IReadSnapshotTagValues>()) {
                    WithSupportsReadSnapshotValues();
                }
                if (adapter.HasFeature<RealTimeData.ISnapshotTagValuePush>()) {
                    WithSupportsSnapshotValuePush();
                }
                if (adapter.HasFeature<RealTimeData.IWriteTagValueAnnotations>()) {
                    WithSupportsWriteAnnotations();
                }
                if (adapter.HasFeature<RealTimeData.IWriteHistoricalTagValues>()) {
                    WithSupportsWriteHistoricalValues();
                }
                if (adapter.HasFeature<RealTimeData.IWriteSnapshotTagValues>()) {
                    WithSupportsWriteSnapshotValues();
                }
            }

            return this;
        }


        /// <summary>
        /// Removes all supported features from the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder ClearSupportedFeatures() {
            _supportedFeatures.Clear();
            return this;
        }


        /// <summary>
        /// Adds labels to the tag.
        /// </summary>
        /// <param name="labels">
        ///   The labels.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithLabels(params string[] labels) {
            return WithLabels((IEnumerable<string>) labels);
        }


        /// <summary>
        /// Adds labels to the tag.
        /// </summary>
        /// <param name="labels">
        ///   The labels.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder WithLabels(IEnumerable<string>? labels) {
            if (labels != null) {
                _labels.AddRange(labels.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            return this;
        }


        /// <summary>
        /// Removes all labels from the tag.
        /// </summary>
        /// <returns>
        ///   The updated <see cref="TagDefinitionBuilder"/>.
        /// </returns>
        public TagDefinitionBuilder ClearLabels() {
            _labels.Clear();
            return this;
        }


        /// <summary>
        /// <see cref="IEqualityComparer{T}"/> for <see cref="Uri"/> objects that checks for 
        /// equality on the full URI, including parts such as the hash. This is to allow the 
        /// same adapter feature URI to be added multiple times with a different hash (see 
        /// <see cref="WithSupportedFeature{TFeature}(string?)"/>).
        /// </summary>
        private class UriComparer : EqualityComparer<Uri> {

            /// <inheritdoc/>
            public override bool Equals(Uri x, Uri y) {
                if (x == null && y == null) {
                    return true;
                }
                if (x == null || y == null) {
                    return false;
                }

                return string.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            /// <inheritdoc/>
            public override int GetHashCode(Uri obj) {
                return obj?.GetHashCode() ?? 0;
            }

        }

    }
}
