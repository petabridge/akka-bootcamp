[<AutoOpen>]
module Messages

open System.Collections.Generic
open System.Windows.Forms.DataVisualization.Charting

type InitializeChart = 
| InitializeChart of initialSeries: Map<string, Series>
