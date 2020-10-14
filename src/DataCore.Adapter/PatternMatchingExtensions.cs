using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods related to pattern matching.
    /// </summary>
    public static class PatternMatchingExtensions {

        /// <summary>
        /// Determines if the string matches the specified regular expression.
        /// </summary>
        /// <param name="s">
        ///   The string to test.
        /// </param>
        /// <param name="expression">
        ///   The regular expression to match.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string matches the expression, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="expression"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   If <paramref name="s"/> is <see langword="null"/>, the method will always return 
        ///   <see langword="false"/>.
        /// </remarks>
        public static bool Like(this string? s, Regex expression) {
            if (expression == null) {
                throw new ArgumentNullException(nameof(expression));
            }

            if (s == null) {
                return expression.Match(string.Empty).Success;
            }

            return expression.Match(s).Success;
        }


        /// <summary>
        /// Determines if the string matches the specified wildcard pattern.
        /// </summary>
        /// <param name="s">
        ///   The string to test.
        /// </param>
        /// <param name="pattern">
        ///   The wildcard pattern to match.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the string matches the wildcard pattern, or 
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pattern"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   If <paramref name="s"/> is <see langword="null"/>, the method will always return 
        ///   <see langword="false"/>.
        /// </para>
        /// 
        /// <para>
        ///   When specifying the wildcard pattern, <c>*</c> is interpreted as a multi-character 
        ///   wildcard; <c>?</c> is interpreted as a single-character wildcard.
        /// </para>
        /// 
        /// <para>
        ///   The pattern matching is case-insensitive.
        /// </para>
        /// 
        /// </remarks>
        /// <example>
        ///   The following example demonstrates how to use the method:
        /// 
        ///   <code>
        ///     string myString = "Hello, world!";
        ///     bool like1 = myString.Like("He*o, wor?d!"); // returns true
        ///     bool like2 = myString.Like("He?o, wor?d!"); // returns false
        ///   </code>
        /// </example>
        public static bool Like(this string? s, string pattern) {
            if (pattern == null) {
                throw new ArgumentNullException(nameof(pattern));
            }

            // Construct a regex that can be used to search the string using the specified pattern 
            // as its base.
            // 
            // * and ? are special cases that will modify the final regex behaviour:
            //
            // * = 0+ characters (i.e. ".*" in regex-speak)
            // ? = 1 character (i.e. "." in regex-speak)

#if NETSTANDARD
            pattern = Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".");
#else
            pattern = Regex.Escape(pattern)
                .Replace(@"\*", ".*", StringComparison.Ordinal)
                .Replace(@"\?", ".", StringComparison.Ordinal);
#endif

            return s.Like(new Regex(pattern, RegexOptions.IgnoreCase));
        }

    }
}
