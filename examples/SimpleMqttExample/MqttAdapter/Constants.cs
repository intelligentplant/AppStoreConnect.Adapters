namespace MqttAdapter {

    /// <summary>
    /// Global application constants.
    /// </summary>
    internal static class Constants {

        /// <summary>
        /// The ID of the hosted adapter.
        /// </summary>
        /// <remarks>
        ///   The adapter ID is used in all API calls made to the adapter host. If you change the 
        ///   value here, remember to change it in App Store Connect as well!
        /// </remarks>
        public const string AdapterId = "$default";

        /// <summary>
        /// The path to the adapter settings JSON file.
        /// </summary>
        public const string AdapterSettingsFilePath = "adaptersettings.json";

    }

}
