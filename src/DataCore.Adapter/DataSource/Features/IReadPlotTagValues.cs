﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.DataSource.Models;

namespace DataCore.Adapter.DataSource.Features {

    /// <summary>
    /// Feature for reading visualization-friendly tag values from an adapter.
    /// </summary>
    /// <remarks>
    ///   The <see cref="Utilities.PlotHelper"/> class can assist with the calculation 
    ///   of values.
    /// </remarks>
    public interface IReadPlotTagValues : IAdapterFeature {

        /// <summary>
        /// Reads plot data from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IDataCoreContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The data query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The plot values for the requested tags.
        /// </returns>
        Task<IEnumerable<HistoricalTagValues>> ReadPlotTagValues(IDataCoreContext context, ReadPlotTagValuesRequest request, CancellationToken cancellationToken);

    }
}
