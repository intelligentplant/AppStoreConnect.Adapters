using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods related to pattern matching.
    /// </summary>
    public static class PatternMatchingExtensions {

        /// <summary>
        /// Escaped versions of characters that have special meanings in regular expressions unless escaped.
        /// </summary>
        private static readonly string[] s_regexSpecialCharacterEscapes = {
            @"\\",
            @"\.",
            @"\$",
            @"\^",
            @"\{",
            @"\[",
            @"\(",
            @"\|",
            @"\)",
            @"\*",
            @"\+",
            @"\?"
        };


        /// <summary>
        /// Converts the specified string into a regular expression pattern that will match the 
        /// string.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <returns>
        /// A regex pattern that will match the string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="s"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   When generating the regular expression, characters in <paramref name="s"/> that have 
        ///   special meaning in regular expressions (e.g. '$') will be replaced with escaped versions 
        ///   of themselves (e.g. '\$').
        /// </remarks>
        public static string ToRegexPattern(this string s) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }
            return s_regexSpecialCharacterEscapes.Aggregate(s, (current, specialCharacter) => Regex.Replace(current, specialCharacter, specialCharacter));
        }


        /// <summary>
        /// Converts the specified string into a regular expression that will match the string.
        /// </summary>
        /// <param name="s">
        ///   The string.
        /// </param>
        /// <param name="options">
        ///   The options to use when creating the regular expression.
        /// </param>
        /// <returns>
        /// A <see cref="Regex"/> that will match the string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="s"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   When generating the regular expression, characters in <paramref name="s"/> that have 
        ///   special meaning in regular expressions (e.g. '$') will be replaced with escaped versions 
        ///   of themselves (e.g. '\$').
        /// </remarks>
        public static Regex ToRegex(this string s, RegexOptions options) {
            if (s == null) {
                throw new ArgumentNullException(nameof(s));
            }

            var pattern = s.ToRegexPattern();
            return new Regex(pattern, options);
        }


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
        ///   When specifying the wildcard patter, <c>*</c> is interpreted as a multi-character 
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

            // Construct a regex that can be used to search the string using the specified pattern as its base.
            //
            // The following characters must be escaped so that their presence doesn't modify the generated regex
            // . $ ^ { [ ( | ) + \
            //
            // * and ? are special cases that will modify the regex behaviour:
            //
            // * = 0+ characters (i.e. ".*?" in regex-speak)
            // ? = 1 character (i.e. "." in regex-speak)

            // Put '\' first so that it doesn't affect the subsequent special cases when we process it.
            var specialCases = new[] { @"\", ".", "$", "^", "{", "[", "(", "|", ")", "+" };

#if NETSTANDARD2_0
            pattern = specialCases.Aggregate(pattern, (current, t) => current.Replace(t, @"\" + t));
            pattern = pattern.Replace('?', '.');
            pattern = pattern.Replace("*", ".*?");
#else
            pattern = specialCases.Aggregate(pattern, (current, t) => current.Replace(t, @"\" + t, StringComparison.Ordinal));
            pattern = pattern.Replace('?', '.');
            pattern = pattern.Replace("*", ".*?", StringComparison.Ordinal);
#endif

            return s.Like(new Regex(pattern, RegexOptions.IgnoreCase));
        }

    }
}
