using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace ChartApp
{
    /// <summary>
    /// Helper class for creating random data for chart plots
    /// </summary>
    public static class ChartDataHelper
    {
        public static Series RandomSeries(string seriesName, SeriesChartType type = SeriesChartType.Line, int points = 100)
        {
            var series = new Series(seriesName) {ChartType = type};
            foreach (var i in Enumerable.Range(0, points))
            {
                series.Points.Add(new DataPoint(i, Math.PI*Math.Sin(i) + Math.Tan(i/4.5)));
            }
            series.BorderWidth = 3;
            return series;
        }
    }
}
