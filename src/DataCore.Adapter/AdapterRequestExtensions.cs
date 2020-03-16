﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter {
    
    /// <summary>
    /// General-purpose extension methods for adapter requests.
    /// </summary>
    public static class AdapterRequestExtensions {

        /// <summary>
        /// Selects a page of items based on the paging settings in a request
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
        public static IEnumerable<T> SelectPage<T>(this IOrderedEnumerable<T> items, Common.IPageableAdapterRequest request) {
            if (items == null) {
                return null;
            }

            if (request == null) {
                return items;
            }

            return items.Skip(request.PageSize * (request.Page - 1)).Take(request.PageSize);
        }

    }
}