using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Provides lookups from data type ID to <see cref="Type"/>, and from <see cref="Type"/> to 
    /// data type ID.
    /// </summary>
    public static class TypeLibrary {

        /// <summary>
        /// Base URI for types in this assembly.
        /// </summary>
        private const string BaseUri = "asc:core/types/";

        /// <summary>
        /// Holds lookups from type to type ID.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Uri> s_typeToIdLookup = new ConcurrentDictionary<Type, Uri>();

        /// <summary>
        /// Holds lookups from type ID to type.
        /// </summary>
        private static readonly ConcurrentDictionary<Uri, Type> s_idToTypeLookup = new ConcurrentDictionary<Uri, Type>();


        /// <summary>
        /// Class initializer.
        /// </summary>
        static TypeLibrary() {
            // Add built-in types.
            foreach (var type in new[] {
                typeof(bool),
                typeof(byte),
                typeof(DateTime),
                typeof(double),
                typeof(float),
                typeof(int),
                typeof(long),
                typeof(sbyte),
                typeof(short),
                typeof(TimeSpan),
                typeof(uint),
                typeof(ulong),
                typeof(ushort),
                typeof(Uri)
            }) {
                TryAdd(type, new Uri(string.Concat(BaseUri, type.Name, "/")));
            }

            // Automatically register types in this assembly.
            AddTypes(typeof(TypeLibrary).Assembly);
        }
        
        
        /// <summary>
        /// Gets the type ID from a <see cref="Type"/> that is annotated with a <see cref="DataTypeIdAttribute"/>.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   The type ID.
        /// </returns>
        private static Uri? GetTypeIdFromDataTypeIdAttribute(Type type) {
            var attr = type.GetCustomAttribute<DataTypeIdAttribute>();
            return attr?.DataTypeId;
        }


        /// <summary>
        /// Tries to add a type registration.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="typeId">
        ///   The type ID.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type was successfully registered, or <see langword="false"/> 
        ///   if the type was already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        public static bool TryAdd(Type type, Uri typeId) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!typeId.IsAbsoluteUri) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(typeId));
            }

            typeId = typeId.EnsurePathHasTrailingSlash();

            if (!s_typeToIdLookup.TryAdd(type, typeId)) {
                return false;
            }
            s_idToTypeLookup[typeId] = type;

            return true;
        }


        /// <summary>
        /// Tries to add a type registration.
        /// </summary>
        /// <typeparam name="T">
        ///   The type.
        /// </typeparam>
        /// <param name="typeId">
        ///   The type ID.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type was successfully registered, or <see langword="false"/> 
        ///   if the type was already registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        public static bool TryAdd<T>(Uri typeId) {
            return TryAdd(typeof(T), typeId);
        }


        /// <summary>
        /// Tries to add a registration for a type that is annotated with a <see cref="DataTypeIdAttribute"/>.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type was successfully registered, or <see langword="false"/> 
        ///   if the type was already registered or was not annotated with a <see cref="DataTypeIdAttribute"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryAdd(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }
            if (type == typeof(EncodedObject)) {
                // Don't allow an EncodedObject to directly contain another EncodedObject!
                return false;
            }

            if (type.Assembly == typeof(TypeLibrary).Assembly) {
                // Types from this assembly get an auto-generated ID.
                return TryAdd(type, new Uri(string.Concat(BaseUri, type.FullName, "/")));
            }

            var typeId = GetTypeIdFromDataTypeIdAttribute(type);
            if (typeId == null) {
                return false;
            }

            return TryAdd(type, typeId);
        }


        /// <summary>
        /// Tries to add a registration for a type that is annotated with a <see cref="DataTypeIdAttribute"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The type.
        /// </typeparam>
        /// <returns>
        ///   <see langword="true"/> if the type was successfully registered, or <see langword="false"/> 
        ///   if the type was already registered or is not annotated with a <see cref="DataTypeIdAttribute"/>.
        /// </returns>
        public static bool TryAdd<T>() {
            return TryAdd(typeof(T));
        }


        /// <summary>
        /// Tries to remove a type registration.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type was removed, or <see langword="false"/> if the 
        ///   type was not registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryRemove(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (!s_typeToIdLookup.TryRemove(type, out var id)) {
                return false;
            }

            s_idToTypeLookup.TryRemove(id, out var _);
            return true;
        }


        /// <summary>
        /// Tries to remove a type registration.
        /// </summary>
        /// <typeparam name="T">
        ///   The type.
        /// </typeparam>
        /// <returns>
        ///   <see langword="true"/> if the type was removed, or <see langword="false"/> if the 
        ///   type was not registered.
        /// </returns>
        public static bool TryRemove<T>() {
            return TryRemove(typeof(T));
        }


        /// <summary>
        /// Tries to remove a type registration.
        /// </summary>
        /// <param name="typeId">
        ///   The type ID.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type was removed, or <see langword="false"/> if the 
        ///   type was not registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        public static bool TryRemove(Uri typeId) {
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!typeId.IsAbsoluteUri) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(typeId));
            }

            typeId = typeId.EnsurePathHasTrailingSlash();
            if (!s_idToTypeLookup.TryRemove(typeId, out var type)) {
                return false;
            }

            s_typeToIdLookup.TryRemove(type, out var _);
            return true;
        }


        /// <summary>
        /// Gets all of the registered types.
        /// </summary>
        /// <returns>
        ///   A collection of <see cref="KeyValuePair{TKey, TValue}"/> instances where the key is 
        ///   the type ID and the value is the type.
        /// </returns>
        public static IEnumerable<KeyValuePair<Uri, Type>> GetRegisteredTypes() {
            return s_idToTypeLookup.ToArray();
        }


        /// <summary>
        /// Tries to get the type ID for the specified type.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <param name="typeId">
        ///   The type URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type ID was found, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryGetTypeId(Type type, out Uri? typeId) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type));
            }

            if (s_typeToIdLookup.TryGetValue(type, out typeId)) {
                return true;
            }

            typeId = GetTypeIdFromDataTypeIdAttribute(type);
            if (typeId != null) {
                // Register this type ID for faster lookup in the future.
                TryAdd(type, typeId);
            }

            return typeId != null;
        }


        /// <summary>
        /// Tries to get the type ID for the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type.
        /// </typeparam>
        /// <param name="typeId">
        ///   The type URI.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type ID was found, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryGetTypeId<T>(out Uri? typeId) {
            return TryGetTypeId(typeof(T), out typeId);
        }


        /// <summary>
        /// Gets the type ID for the specified type.
        /// </summary>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   The type ID, or <see langword="null"/> if the type ID is unknown.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public static Uri? GetTypeId(Type type) {
            return TryGetTypeId(type, out var typeId) ? typeId : null;
        }


        /// <summary>
        /// Gets the type ID for the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type.
        /// </typeparam>
        /// <returns>
        ///   The type ID, or <see langword="null"/> if the type ID is unknown.
        /// </returns>
        public static Uri? GetTypeId<T>() {
            return TryGetTypeId<T>(out var typeId) ? typeId : null;
        }


        /// <summary>
        /// Tries to get the type for the specified type ID.
        /// </summary>
        /// <param name="typeId">
        ///   The type URI.
        /// </param>
        /// <param name="type">
        ///   The type.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the type was found, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="typeId"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="typeId"/> is not an absolute URI.
        /// </exception>
        public static bool TryGetType(Uri typeId, out Type type) {
            if (typeId == null) {
                throw new ArgumentNullException(nameof(typeId));
            }
            if (!typeId.IsAbsoluteUri) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(typeId));
            }

            typeId = typeId.EnsurePathHasTrailingSlash();
            return s_idToTypeLookup.TryGetValue(typeId, out type);
        }


        /// <summary>
        /// Registers types from the specified assembly.
        /// </summary>
        /// <param name="assembly">
        ///   The assembly.
        /// </param>
        /// <returns>
        ///   The number of types that were registered.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="assembly"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   Types from the assembly will be registered if they are non-abstract types that are 
        ///   visible outside the assembly.
        /// </remarks>
        public static int AddTypes(Assembly assembly) {
            if (assembly == null) {
                throw new ArgumentNullException(nameof(assembly));
            }

            var result = 0;

            foreach (var type in assembly.GetExportedTypes().Where(x => x.IsPublic && !x.IsAbstract && !x.IsGenericTypeDefinition && !x.IsInterface)) {
                if (TryAdd(type)) {
                    ++result;
                }
            }

            return 0;
        }

    }
}
