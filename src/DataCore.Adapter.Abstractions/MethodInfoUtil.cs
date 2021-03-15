using System;
using System.Linq;
using System.Reflection;

namespace DataCore.Adapter {

    /// <summary>
    /// Defines static methods for working with <see cref="MethodInfo"/> objects.
    /// </summary>
    public static class MethodInfoUtil {

        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo(Delegate func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo(Action func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T">
        ///   The parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T>(Action<T> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2>(Action<T1, T2> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3>(Action<T1, T2, T3> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4>(Action<T1, T2, T3, T4> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The eighth parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The eighth parameter type.
        /// </typeparam>
        /// <typeparam name="T9">
        ///   The ninth parameter type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T>(Func<T> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2>(Func<T1, T2> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7> func) => func?.Method;


        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8> func) => func?.Method;
        

        /// <summary>
        /// Gets the <see cref="MethodInfo"/> for the specified delegate.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <typeparam name="T6">
        ///   The sixth parameter type.
        /// </typeparam>
        /// <typeparam name="T7">
        ///   The seventh parameter type.
        /// </typeparam>
        /// <typeparam name="T8">
        ///   The eighth parameter type.
        /// </typeparam>
        /// <typeparam name="T9">
        ///   The delegate return type.
        /// </typeparam>
        /// <param name="func">
        ///   The delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="MethodInfo"/> for the delegate, or <see langword="null"/> if no 
        ///   <see cref="MethodInfo"/> was found.
        /// </returns>
        public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9> func) => func?.Method;


        /// <summary>
        /// For the specified <paramref name="method"/>, attempts to find the equivalent method 
        /// declaration in an interface implemented by the method's implementing type.
        /// </summary>
        /// <param name="method">
        ///   The <see cref="MethodInfo"/> implementation to retrieve the interface declaration for.
        /// </param>
        /// <param name="interfaceMethod">
        ///   The equivalent <see cref="MethodInfo"/> as declared on the interface type that defines
        ///   the method.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if an equivalent method was found in an interface implemented 
        ///   by the <see cref="MemberInfo.ReflectedType"/> for the <paramref name="method"/>, or
        ///   <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetInterfaceMethodDeclaration(MethodInfo? method, out MethodInfo? interfaceMethod) {
            interfaceMethod = null;

            if (method == null) {
                return false;
            }
            
            // Get all of the extension feature interface mappings defined by the implementing type 
            // for the method.
            var interfaceMappings = method
                .ReflectedType!
                .GetTypeInfo()
                .ImplementedInterfaces
                .Select(x => method.ReflectedType!.GetInterfaceMap(x));

            foreach (var mapping in interfaceMappings) {
                var methodIndex = -1;

                // The target methods for the mapping contain the actual method implementations i.e. 
                // the method parameter.
                for (var i = 0; i < mapping.TargetMethods.Length; i++) {
                    var targetMethod = mapping.TargetMethods[i];
                    if (targetMethod != method) {
                        // Not the method we're looking for.
                        continue;
                    }

                    // We're found our method implementation; make a note of the index so that we 
                    // can get the equivalent method on the interface type.
                    methodIndex = i;
                    break;
                }

                if (methodIndex >= 0) {
                    // We found our method implementation in the current interface mapping. The 
                    // method definition on the interface will be at the same index as the method 
                    // definition was found at on the implementing class.
                    interfaceMethod = mapping.InterfaceMethods[methodIndex];
                    break;
                }
            }

            return interfaceMethod != null;
        }


    }
}
