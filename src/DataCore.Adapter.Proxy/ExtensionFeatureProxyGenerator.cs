using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

using Castle.DynamicProxy;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Proxy {

    /// <summary>
    /// Generates dynamic implementations of unknown extension adapter features.
    /// </summary>
    public static class ExtensionFeatureProxyGenerator {

        /// <summary>
        /// Castle DynamicProxy generator instance.
        /// </summary>
        private static readonly ProxyGenerator s_proxyGenerator = new ProxyGenerator();

        /// <summary>
        /// Lookup from feature URI to dynamically generated feature interface.
        /// </summary>
        private static readonly ConcurrentDictionary<Uri, Type> s_featureInterfaceLookup = new ConcurrentDictionary<Uri, Type>();

        /// <summary>
        /// Module builder for generating dynamic interfaces for feature URIs.
        /// </summary>
        private static readonly ModuleBuilder s_moduleBuilder;

        /// <summary>
        /// <see cref="AdapterFeatureAttribute(string)"/> constructor reference.
        /// </summary>
        private static readonly ConstructorInfo s_adapterExtensionFeatureAttributeConstructor;

        /// <summary>
        /// Namespace to use for dynamically generated feature interfaces.
        /// </summary>
        private const string DynamicTypeNamespace = "DataCore.Adapter.Proxy.DynamicExtensions";



        /// <summary>
        /// Class initialiser.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Complex initialisation required")]
        static ExtensionFeatureProxyGenerator() {
            var assemblyName = new AssemblyName(typeof(ExtensionFeatureProxyGenerator).Assembly.GetName().Name + ".DynamicExtensions");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            s_moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicExtensionsModule");
            s_adapterExtensionFeatureAttributeConstructor = typeof(ExtensionFeatureAttribute).GetConstructor(new[] { typeof(string) })!;
        }


        /// <summary>
        /// Builds a dynamic <see cref="Type"/> representing the extension feature with the 
        /// specified URI.
        /// </summary>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <returns>
        ///   The feature <see cref="Type"/>
        /// </returns>
        private static Type BuildExtensionFeatureType(Uri featureUri) {
            var typeName = featureUri.ToString();
            var fullyQualifiedTypeName = string.Concat(DynamicTypeNamespace, ".", typeName);

            var existing = s_moduleBuilder.GetType(fullyQualifiedTypeName, true);
            if (existing != null) {
                // We've already generated a type for this feature URI.
                return existing;
            }

            var typeBuilder = s_moduleBuilder.DefineType(
                fullyQualifiedTypeName,
                TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public
            );
            typeBuilder.AddInterfaceImplementation(typeof(IAdapterExtensionFeature));
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                s_adapterExtensionFeatureAttributeConstructor,
                new object[] { featureUri.ToString() }
            ));

            return typeBuilder.CreateType()!;
        }


        /// <summary>
        /// Creates a dynamic proxy feature implementation for the specified proxy adapter.
        /// </summary>
        /// <typeparam name="TProxy">
        ///   The type of the proxy adapter.
        /// </typeparam>
        /// <typeparam name="TImpl">
        ///   The <see cref="ExtensionFeatureFactory{TProxy}"/> implementation to derive the proxy 
        ///   feature from.
        /// </typeparam>
        /// <param name="proxy">
        ///   The proxy adapter that the feature implementation is being generated for.
        /// </param>
        /// <param name="featureUri">
        ///   The extension feature URI to generate a proxy for.
        /// </param>
        /// <returns>
        ///   An <see cref="IAdapterExtensionFeature"/> instance that implements the dynamically 
        ///   generated interface for the <paramref name="featureUri"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="featureUri"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="featureUri"/> is not an absolute URI.
        /// </exception>
        public static IAdapterExtensionFeature CreateExtensionFeatureProxy<TProxy, TImpl>(
            TProxy proxy,
            Uri featureUri
        ) where TProxy : AdapterBase, IAdapterProxy where TImpl : ExtensionFeatureProxyBase<TProxy> {
            if (proxy == null) {
                throw new ArgumentNullException(nameof(proxy));
            }
            if (featureUri == null) {
                throw new ArgumentNullException(nameof(featureUri));
            }

            featureUri = UriExtensions.EnsurePathHasTrailingSlash(featureUri);
            var featureType = s_featureInterfaceLookup.GetOrAdd(featureUri, BuildExtensionFeatureType);

            return (IAdapterExtensionFeature) s_proxyGenerator.CreateClassProxy(
                typeof(TImpl),
                new[] { featureType },
                ProxyGenerationOptions.Default,
                new object[] { proxy }
            );
        }

    }
}
