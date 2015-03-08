[<AutoOpen>]
module Messages

open System.Collections.Generic
open System.Windows.Forms.DataVisualization.Charting

type ChartMessages = 
| InitializeChart of initialSeries: Map<string, Series>
| AddSeries of series: Series