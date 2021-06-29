using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataCore.Adapter.Services {
    /// <summary>
    /// Extensions for <see cref="IKeyValueStore"/>.
    /// </summary>
    public static class KeyValueStoreExtensions {

        #region CreateScopedStore

        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="prefix"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, byte[] prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }
            if (prefix == null) {
                throw new ArgumentNullException(nameof(prefix));
            }

            return new ScopedKeyValueStore(prefix, store);
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="prefix"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a UTF-8 byte array.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, string prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }
            if (prefix == null) {
                throw new ArgumentNullException(nameof(prefix));
            }

            return store.CreateScopedStore(Encoding.UTF8.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(ulong)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, ulong prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(long)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, long prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(uint)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, uint prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(int)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, int prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(ushort)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, ushort prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(short)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, short prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(double)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, double prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }


        /// <summary>
        /// Creates a new scoped <see cref="IKeyValueStore"/> that automatically applies a prefix 
        /// to keys used in operations on this store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the scoped store.
        /// </param>
        /// <returns>
        ///   A new <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Use this method to create a wrapper for an existing <see cref="IKeyValueStore"/> that 
        ///   will automatically prefix keys with a scoped namespace.
        /// </para>
        /// 
        /// <para>
        ///   The <paramref name="prefix"/> will be converted to a byte array using 
        ///   <see cref="BitConverter.GetBytes(float)"/>.
        /// </para>
        /// 
        /// </remarks>
        public static IKeyValueStore CreateScopedStore(this IKeyValueStore store, float prefix) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return store.CreateScopedStore(BitConverter.GetBytes(prefix));
        }

        #endregion

        #region WriteAsync

        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a UTF-8 byte array.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, string key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            return await store.WriteAsync(Encoding.UTF8.GetBytes(key), value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(ulong)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, ulong key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(long)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, long key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(uint)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, uint key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }



        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(int)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, int key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(ushort)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, ushort key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(short)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, short key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(double)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, double key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a value to the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(float)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(this IKeyValueStore store, float key, TValue? value) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.WriteAsync(BitConverter.GetBytes(key), value).ConfigureAwait(false);
        }

        #endregion

        #region ReadAsync

        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a UTF-8 byte array.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, string key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            return await store.ReadAsync<TValue>(Encoding.UTF8.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(ulong)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, ulong key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(long)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, long key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(uint)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, uint key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(int)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, int key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(ushort)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, ushort key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(short)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, short key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(double)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, double key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads a value from the store.
        /// </summary>
        /// <typeparam name="TValue">
        ///   The value type.
        /// </typeparam>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="KeyValueStoreReadResult{T}"/> 
        ///   containing the operation status and value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(float)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(this IKeyValueStore store, float key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.ReadAsync<TValue>(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }

        #endregion

        #region DeleteAsync

        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a UTF-8 byte array.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, string key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            return await store.DeleteAsync(Encoding.UTF8.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(ulong)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, ulong key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(long)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, long key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(uint)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, uint key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(int)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, int key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(ushort)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, ushort key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(short)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, short key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(double)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, double key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a value from the store.
        /// </summary>
        /// <param name="store">
        ///   The <see cref="IKeyValueStore"/>.
        /// </param>
        /// <param name="key">
        ///   The key for the value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the status of the operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="store"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   <paramref name="key"/> will be converted to a byte array using <see cref="BitConverter.GetBytes(float)"/>.
        /// </remarks>
        public static async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(this IKeyValueStore store, float key) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            return await store.DeleteAsync(BitConverter.GetBytes(key)).ConfigureAwait(false);
        }

        #endregion

        #region GetKeys

        /// <summary>
        /// Gets the keys that are defined in the store, converted from <see cref="T:byte[]"/> to 
        /// <see cref="string"/>.
        /// </summary>
        /// <returns>
        ///   The keys, converted to strings.
        /// </returns>
        /// <remarks>
        ///   Each key is converted to a string by calling <see cref="Encoding.GetString(byte[])"/> 
        ///   on <see cref="Encoding.UTF8"/>. If an exception is thrown during this conversion, the 
        ///   key will be converted to a string using <see cref="BitConverter.ToString(byte[])"/> 
        ///   instead.
        /// </remarks>
        public static IEnumerable<string> GetKeysAsStrings(this IKeyValueStore store) {
            if (store == null) {
                throw new ArgumentNullException(nameof(store));
            }

            foreach (var key in store.GetKeys()) {
                string result;
                try {
                    result = Encoding.UTF8.GetString(key);
                }
                catch {
                    result = BitConverter.ToString(key);
                }
                yield return result;
            }
        }

        #endregion

    }
}
