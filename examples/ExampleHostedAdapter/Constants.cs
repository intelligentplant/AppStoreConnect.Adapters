namespace ExampleHostedAdapter {

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
        public const string AdapterId = "e445a468-19ee-456c-9aac-e26288475a45";

        /// <summary>
        /// The path to the adapter settings JSON file.
        /// </summary>
        public const string AdapterSettingsFilePath = "adaptersettings.json";

    }

}
