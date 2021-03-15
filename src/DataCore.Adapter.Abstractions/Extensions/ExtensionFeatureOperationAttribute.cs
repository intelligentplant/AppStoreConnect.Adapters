using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Use the <see cref="ExtensionFeatureOperationAttribute"/> to define metadata associated 
    /// with an extension feature method. This metadata can be used when constructing an 
    /// <see cref="ExtensionFeatureOperationDescriptor"/> associated with the method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ExtensionFeatureOperationAttribute : Attribute {

        /// <summary>
        /// A factory method that can be used to retrieve metadata to use in an operation descriptor.
        /// </summary>
        PartialOperationDescriptorFactory _factory;


        /// <summary>
        /// Creates a new <see cref="ExtensionFeatureOperationAttribute"/> that will create a 
        /// <see cref="PartialOperationDescriptorFactory"/> to retrieve operation metadata using 
        /// the supplied type and method name.
        /// </summary>
        /// <param name="providerType">
        ///   The type defining the <see cref="PartialOperationDescriptorFactory"/> delegate.
        /// </param>
        /// <param name="methodName">
        ///   The name of the method to use. The method must be <see langword="static"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="providerType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="methodName"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   A <see langword="static"/> method with the supplied method name cannot be found, or 
        ///   the method does not match the required <see cref="PartialOperationDescriptorFactory"/> 
        ///   delegate signature.
        /// </exception>
        public ExtensionFeatureOperationAttribute(Type providerType, string methodName) {
            if (providerType == null) {
                throw new ArgumentNullException(nameof(providerType));
            }
            if (string.IsNullOrWhiteSpace(methodName)) {
                throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(methodName));
            }

            var factory = providerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(x => {
                if (!string.Equals(x.Name, methodName, StringComparison.Ordinal)) {
                    return false;
                }

                if (x.ReturnType != typeof(ExtensionFeatureOperationDescriptorPartial)) {
                    return false;
                }

                if (x.GetParameters().Any()) {
                    return false;
                }

                return true;
            });

            if (factory == null) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_UnableToResolveMethod, methodName, providerType.FullName), nameof(providerType));
            }

            _factory = (PartialOperationDescriptorFactory) factory.CreateDelegate(typeof(PartialOperationDescriptorFactory), null);
        }


        /// <summary>
        /// Creates a new <see cref="ExtensionFeatureOperationDescriptorPartial"/> using the 
        /// attribute's <see cref="PartialOperationDescriptorFactory"/> delegate.
        /// </summary>
        /// <returns>
        ///   A new <see cref="ExtensionFeatureOperationDescriptorPartial"/> object.
        /// </returns>
        public ExtensionFeatureOperationDescriptorPartial CreateDescriptor() {
            return _factory.Invoke();
        }


        /// <summary>
        /// Creates an operation descriptor for the specified <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">
        ///   The method. If the method has been annotated with an <see cref="ExtensionFeatureOperationAttribute"/>, 
        ///   the attribute will be used to populate the operation descriptor metadata.
        /// </param>
        /// <returns>
        ///   A new <see cref="ExtensionFeatureOperationDescriptor"/> object, or <see langword="null"/> 
        ///   if <paramref name="methodInfo"/> is <see langword="null"/> or has not been annotated 
        ///   with <see cref="ExtensionFeatureOperationAttribute"/>.
        /// </returns>
        public static ExtensionFeatureOperationDescriptorPartial? CreateDescriptor(MethodInfo? methodInfo) {
            if (methodInfo == null) {
                return null;
            }

            var attr = methodInfo.GetCustomAttribute<ExtensionFeatureOperationAttribute>();
            if (attr == null && MethodInfoUtil.TryGetInterfaceMethodDeclaration(methodInfo, out var interfaceMethod)) {
                attr = interfaceMethod.GetCustomAttribute<ExtensionFeatureOperationAttribute>();
            }

            var result = attr?.CreateDescriptor() ?? new ExtensionFeatureOperationDescriptorPartial();

            if (string.IsNullOrWhiteSpace(result.Name)) {
                result.Name = methodInfo.Name;
            }

            return result;
        }

    }


    /// <summary>
    /// Delegate that can return a partial descriptor for an extension feature operation.
    /// </summary>
    /// <returns>
    ///   A new <see cref="ExtensionFeatureOperationDescriptorPartial"/> instance.
    /// </returns>
    public delegate ExtensionFeatureOperationDescriptorPartial PartialOperationDescriptorFactory();

}
