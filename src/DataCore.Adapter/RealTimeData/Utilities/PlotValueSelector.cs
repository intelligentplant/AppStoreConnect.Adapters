using System.Collections.Generic;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Selects the significant values to return from a <see cref="TagValueBucket"/> for a plot 
    /// (best-fit) query being processed by <see cref="PlotHelper"/>.
    /// </summary>
    /// <param name="tag">
    ///   The tag that the plot values are being selected for.
    /// </param>
    /// <param name="bucket">
    ///   The <see cref="TagValueBucket"/> to select the values from.
    /// </param>
    /// <returns>
    ///   A collection of <see cref="PlotValue"/> instances representing the values selected from 
    ///   the <paramref name="bucket"/>.
    /// </returns>
    /// <seealso cref="PlotHelper.DefaultPlotValueSelector"/>
    public delegate IEnumerable<PlotValue> PlotValueSelector(TagSummary tag, TagValueBucket bucket);

}
