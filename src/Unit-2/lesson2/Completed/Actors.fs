namespace ChartApp

open System.Collections.Generic
open System.Windows.Forms.DataVisualization.Charting
open Akka.Actor
open Akka.FSharp

[<AutoOpen>]
module Messages =
    type InitializeChart = 
    | InitializeChart of initialSeries: Map<string, Series>

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
    | SubscribeCounter of counter: CounterType * subscriber: IActorRef
    | UnsubscribeCounter of counter: CounterType * subscriber: IActorRef

    type CoordinationMessage =
    | Watch of counter: CounterType
    | Unwatch of counter: CounterType

    type ButtonMessage =
    | Toggle


/// Actors used to intialize chart data
[<AutoOpen>]
module Actors = 
    open System
    open System.Diagnostics
    open System.Drawing
    open Map

    let chartingActor (chart: Chart) message =
        match message with
        | InitializeChart series -> 
            chart.Series.Clear ()
            series |> Map.iter (fun k v -> 
                v.Name <- k
                chart.Series.Add(v))

    let performanceCounterActor (seriesName:string)  (performanceCounterGenerator:unit -> PerformanceCounter) (mailbox:Actor<_>) =
        let counter = performanceCounterGenerator ()
        let eventStream = mailbox.Context.System.EventStream
        let cancelled = mailbox.Context.System.Scheduler.ScheduleTellRepeatedly (TimeSpan.FromMilliseconds 250., 
                            TimeSpan.FromMilliseconds 250.,
                            mailbox.Self,
                            GatherMetrics)

        mailbox.Defer (fun () -> 
            counter.Dispose() |> ignore
        )

        let rec loop() = actor {
            let! message = mailbox.Receive ()
            match box message :?> CounterMessage with
            | GatherMetrics -> 
                let msg = Metric(seriesName, float <| counter.NextValue ())
                publish msg eventStream 
            | SubscribeCounter(c,s) -> subscribe typeof<CounterMessage> s eventStream  |> ignore 
            | UnsubscribeCounter(c,s) -> unsubscribe typeof<CounterMessage> s eventStream  |> ignore

            return! loop()
        }
        loop()

        
    let performanceCounterCoordinatorActor chartingActor =
        let counterGenerators = Map.ofList [CounterType.Cpu, fun () -> new PerformanceCounter("Processor", "% Processor Time", "_Total", true)
                                            CounterType.Memory, fun () -> new PerformanceCounter("Memory", "% Committed Bytes In Use", true)
                                            CounterType.Disk, fun () -> new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)]
    
        let counterSeries = Map.ofList [CounterType.Cpu, fun () -> new Series(CounterType.Cpu.ToString (), ChartType = SeriesChartType.SplineArea, Color = Color.DarkGreen)
                                        CounterType.Memory, fun () -> new Series(CounterType.Memory.ToString (), ChartType = SeriesChartType.FastLine, Color = Color.MediumBlue)
                                        CounterType.Disk, fun () -> new Series(CounterType.Disk.ToString (), ChartType = SeriesChartType.SplineArea, Color = Color.DarkRed)]

        let counterActors = ref Map.empty // 'mutable' can be used with F# v4.0 http://blogs.msdn.com/b/fsharpteam/archive/2014/11/12/announcing-a-preview-of-f-4-0-and-the-visual-f-tools-in-vs-2015.aspx 

        (fun (mailbox: Actor<_>) message -> 
            match message with
            | Watch counter when not (containsKey counter counterActors) -> // ((!counterActors).ContainsKey counter) -> 
                let counterName = counter.ToString ()
                let actor = spawnObj mailbox.Context (sprintf "counterActor-%s" counterName) (<@ fun () -> new PerformanceCounterActor(counterName, counterGenerators.[counter]) @>)
                counterActors := (!counterActors).Add (counter, actor)
                chartingActor <! AddSeries(counterSeries.[counter] ())
                (!counterActors).[counter] <! SubscribeCounter(counter, chartingActor)
            | Watch counter ->
                chartingActor <! AddSeries(counterSeries.[counter] ())
                (!counterActors).[counter] <! SubscribeCounter(counter, chartingActor)
            | Unwatch counter when (!counterActors).ContainsKey(counter) -> 
                chartingActor <! RemoveSeries((counterSeries.[counter] ()).Name)
                (!counterActors).[counter] <! UnsubscribeCounter(counter, chartingActor)
            | _ -> ())
