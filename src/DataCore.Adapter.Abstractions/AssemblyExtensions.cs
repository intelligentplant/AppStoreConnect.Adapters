using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DataCore.Adapter {

    /// <summary>
    /// Extension methods for <see cref="Assembly"/>.
    /// </summary>
    public static class AssemblyExtensions {

        /// <summary>
        /// Gets the informational version for the <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">
        ///   The <see cref="Assembly"/>.
        /// </param>
        /// <returns>
        ///   The informational version for the assembly, or <see langword="null"/> if version 
        ///   information cannot be found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="assembly"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        /// The return value will be selected from the following (in order of precedence):
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       The assembly's <see cref="AssemblyInformationalVersionAttribute"/>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       The assembly's <see cref="AssemblyFileVersionAttribute"/>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     The <see cref="AssemblyName.Version"/> for the assembly's <see cref="AssemblyName"/>.
        ///   </item>
        /// </list>
        /// 
        /// <para>
        ///   If a value cannot be inferred from any of the above options, the return value is 
        ///   <see langword="null"/>.
        /// </para>
        /// 
        /// </remarks>
        public static string? GetInformationalVersion(this Assembly assembly) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }

            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
                ?? assembly.GetName().Version?.ToString();
        }

    }
}
