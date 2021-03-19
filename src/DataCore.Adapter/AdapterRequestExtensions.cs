using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter {

    /// <summary>
    /// General-purpose extension methods for adapter requests.
    /// </summary>
    public static class AdapterRequestExtensions {

        /// <summary>
        /// Selects a page of items based on the paging settings in a request.
        /// </summary>
        /// <param name="items">
        ///   The items to select from.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The selected items.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="items"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public static IEnumerable<T> SelectPage<T>(this IOrderedEnumerable<T> items, Common.IPageableAdapterRequest request) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            if (request == null) {
                return items;
            }

            return items.SelectPage(request.PageSize, request.Page);
        }


        /// <summary>
        /// Selects a page of items.
        /// </summary>
        /// <param name="items">
        ///   The items to select from.
        /// </param>
        /// <param name="pageSize">
        ///   The page size.
        /// </param>
        /// <param name="page">
        ///   The page number.
        /// </param>
        /// <returns>
        ///   The selected items.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="items"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   If <paramref name="pageSize"/> is less than one, a page size of one will be used.
        /// </para>
        /// 
        /// <para>
        ///   If <paramref name="page"/> is less than one, the first page of items will be selected.
        /// </para>
        /// 
        /// </remarks>
        public static IEnumerable<T> SelectPage<T>(this IOrderedEnumerable<T> items, int pageSize, int page) {
            if (items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            if (pageSize < 1) {
                pageSize = 1;
            }
            if (page < 1) {
                page = 1;
            }

            return items.Skip(pageSize * (page - 1)).Take(pageSize);
        }

    }
}
