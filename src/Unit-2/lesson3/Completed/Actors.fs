namespace ChartApp

open System.Collections.Generic
open System.Windows.Forms.DataVisualization.Charting
open Akka.Actor
open Akka.FSharp
open System
open System.Diagnostics
open System.Drawing

[<AutoOpen>]
module Messages =
    
    type ChartMessage = 
        | InitializeChart of initialSeries: Map<string, Series>
        | AddSeries of series: Series
        | RemoveSeries of seriesName: string
        | Metric of series: string * counterValue: float

    type CounterType =
        | Cpu = 1
        | Memory = 2
        | Disk = 3

    type CounterMessage =
        | GatherMetrics
        | SubscribeCounter of subscriber: IActorRef
        | UnsubscribeCounter of subscriber: IActorRef

    type CoordinationMessage =
        | Watch of counter: CounterType
        | Unwatch of counter: CounterType

    type ButtonMessage = Toggle

/// Actors used to intialize chart data
[<AutoOpen>]
module Actors =

    let chartingActor (chart: Chart) (mailbox: Actor<_>) =

        let maxPoints = 250

        let setChartBoundaries (mapping: Map<string, Series>, numberOfPoints: int) =
            let allPoints =
                mapping
                |> Map.toList
                |> Seq.collect (fun (_, series) -> series.Points)
                |> (fun points -> HashSet<DataPoint>(points))

            if Seq.length allPoints > 2 then
                let yValues = allPoints |> Seq.collect (fun p -> p.YValues) |> Seq.toList
                chart.ChartAreas.[0].AxisX.Maximum <- float numberOfPoints
                chart.ChartAreas.[0].AxisX.Minimum <- (float numberOfPoints - float maxPoints)
                chart.ChartAreas.[0].AxisY.Maximum <- if List.length yValues > 0 then Math.Ceiling(List.max yValues) else 1.
                chart.ChartAreas.[0].AxisY.Minimum <- if List.length yValues > 0 then Math.Floor(List.min yValues) else 0.
            else
                ()

        let rec charting(mapping: Map<string, Series>, numberOfPoints: int) = actor {
            let! message = mailbox.Receive ()

            match message with  
            | InitializeChart series ->
                chart.Series.Clear ()
                chart.ChartAreas.[0].AxisX.IntervalType <- DateTimeIntervalType.Number
                chart.ChartAreas.[0].AxisY.IntervalType <- DateTimeIntervalType.Number
                series |> Map.iter (fun k v ->
                    v.Name <- k
                    chart.Series.Add v)
                return! charting(series, numberOfPoints)

            | AddSeries series when not <| String.IsNullOrEmpty series.Name && not <| (mapping |> Map.containsKey series.Name) ->
                let newMapping = mapping.Add (series.Name, series)
                chart.Series.Add series
                setChartBoundaries (newMapping, numberOfPoints)
                return! charting (newMapping, numberOfPoints)

            | RemoveSeries seriesName when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName ->
                chart.Series.Remove mapping.[seriesName] |> ignore
                let newMapping = mapping.Remove seriesName
                setChartBoundaries (newMapping, numberOfPoints)
                return! charting (newMapping, numberOfPoints)

            | Metric (seriesName, counterValue) when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName ->
                let newNoOfPts = numberOfPoints + 1
                let series = mapping.[seriesName]
                series.Points.AddXY (numberOfPoints, counterValue) |> ignore
                while (series.Points.Count > maxPoints) do series.Points.RemoveAt 0
                setChartBoundaries (mapping, newNoOfPts)
                return! charting (mapping, newNoOfPts)
        }
        charting (Map.empty<string, Series>, 0)


    let performanceCounterActor (seriesName: string) (perfCounterGenerator: unit -> PerformanceCounter) (mailbox: Actor<_>) =
        let counter = perfCounterGenerator ()
        let cancelled = mailbox.Context.System.Scheduler.ScheduleTellRepeatedlyCancelable (
                            TimeSpan.FromMilliseconds 250.,
                            TimeSpan.FromMilliseconds 250.,
                            mailbox.Self,
                            GatherMetrics,
                            ActorRefs.NoSender)

        mailbox.Defer (fun _ ->
            cancelled.Cancel ()
            counter.Dispose () |> ignore)

        let rec loop subscriptions = actor {
            let! message = mailbox.Receive ()

            match box message :?> CounterMessage with
            | GatherMetrics ->
                let msg = Metric(seriesName, counter.NextValue () |> float)
                subscriptions |> Seq.iter (fun subscriber -> subscriber <! msg)
                return! loop subscriptions
            | SubscribeCounter sub ->
                let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun actor -> actor <> sub)
                return! loop (sub::subscriptionsWithoutSubscriber)
            | UnsubscribeCounter sub ->
                let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun actor -> actor <> sub)
                return! loop subscriptionsWithoutSubscriber
        }
        loop []


    let performanceCounterCoordinatorActor chartingActor (mailbox: Actor<_>) =
        let counterGenerators = Map.ofList [CounterType.Cpu, fun _ -> new PerformanceCounter("Processor", "% Processor Time", "_Total", true)
                                            CounterType.Memory, fun _ -> new PerformanceCounter("Memory", "% Committed Bytes In Use", true)
                                            CounterType.Disk, fun _ -> new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)]

        let counterSeries = Map.ofList [CounterType.Cpu, fun _ -> new Series(string CounterType.Cpu, ChartType = SeriesChartType.SplineArea, Color = Color.DarkGreen)
                                        CounterType.Memory, fun _ -> new Series(string CounterType.Memory, ChartType = SeriesChartType.FastLine, Color = Color.MediumBlue)
                                        CounterType.Disk, fun _ -> new Series(string CounterType.Disk, ChartType = SeriesChartType.SplineArea, Color = Color.DarkRed)]

        let rec loop (counterActors: Map<CounterType, IActorRef>) = actor {
            let! message = mailbox.Receive ()

            match message with
            | Watch counter when counterActors |> Map.containsKey counter |> not ->
                let counterName = string counter
                let actor = spawn mailbox.Context (sprintf "counterActor-%s" counterName) (performanceCounterActor counterName counterGenerators.[counter])
                let newCounterActors = counterActors.Add (counter, actor)
                chartingActor <! AddSeries(counterSeries.[counter] ())
                newCounterActors.[counter] <! SubscribeCounter(chartingActor)
                return! loop newCounterActors
            | Watch counter ->
                chartingActor <! AddSeries(counterSeries.[counter] ())
                counterActors.[counter] <! SubscribeCounter(chartingActor)
            | Unwatch counter when counterActors |> Map.containsKey counter ->
                chartingActor <! RemoveSeries((counterSeries.[counter] ()).Name)
                counterActors.[counter] <! UnsubscribeCounter(chartingActor)
            
            return! loop counterActors
        }
        loop Map.empty


    let buttonToggleActor coordinatorActor (button: System.Windows.Forms.Button) counterType isToggled (mailbox: Actor<_>) =
        let flipToggle isOn =
            let isToggledOn = not isOn
            button.Text <- sprintf "%s (%s)" (counterType.ToString().ToUpperInvariant()) (if isToggledOn then "ON" else "OFF")
            isToggledOn

        let rec loop isToogledOn = actor {
            let! message = mailbox.Receive ()

            match message with
            | Toggle when isToogledOn -> coordinatorActor <! Unwatch(counterType)
            | Toggle when not isToogledOn -> coordinatorActor <! Watch(counterType)
            | m -> mailbox.Unhandled m

            return! loop (flipToggle isToogledOn)
        }
        loop isToggled