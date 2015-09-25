[<AutoOpen>]
module Messages

open System.Collections.Generic
open System.Windows.Forms.DataVisualization.Charting
open Akka.Actor

type CounterType =
| Cpu = 1
| Memory = 2
| Disk = 3

type ChartMessage = 
| InitializeChart of initialSeries: Map<string, Series>
| AddSeries of series: Series
| RemoveSeries of seriesName: string
| Metric of series: string * counterValue: float

type CounterMessage = 
| GatherMetrics
| SubscribeCounter of counter: CounterType * subscriber: ActorRef
| UnsubscribeCounter of counter: CounterType * subscriber: ActorRef

type CoordinationMessage =
| Watch of counter: CounterType
| Unwatch of counter: CounterType

type ButtonMessage =
| Toggle