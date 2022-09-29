namespace ExampleHostedAdapter {

    /// <summary>
    /// <see cref="StringComparer"/> implementation that is used to sort adapter feature 
    /// categories in the project's Razor pages to that features in the "Extensions" category are 
    /// ordered last.
    /// </summary>
    internal class FeatureCategoryComparer : StringComparer {

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static FeatureCategoryComparer Instance => new FeatureCategoryComparer();


        /// <summary>
        /// Creates a new <see cref="FeatureCategoryComparer"/> instance.
        /// </summary>
        private FeatureCategoryComparer() { }

        /// <inheritdoc/>
        public override int Compare(string? x, string? y) => Equals(x, y)
            ? 0
            : OrdinalIgnoreCase.Equals(x, DataCore.Adapter.AbstractionsResources.Category_Extensions)
                ? 1
                : OrdinalIgnoreCase.Equals(y, DataCore.Adapter.AbstractionsResources.Category_Extensions)
                    ? -1
                    : OrdinalIgnoreCase.Compare(x, y);    


        /// <inheritdoc/>
        public override bool Equals(string? x, string? y) => OrdinalIgnoreCase.Equals(x, y);


        /// <inheritdoc/>
        public override int GetHashCode(string obj) => OrdinalIgnoreCase.GetHashCode(obj);

    }
}
