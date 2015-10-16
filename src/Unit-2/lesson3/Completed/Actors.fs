namespace ChartApp

open System.Linq
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
    | TogglePause

    type CounterMessage = 
    | GatherMetrics
    | SubscribeCounter of subscriber: IActorRef
    | UnsubscribeCounter of subscriber: IActorRef

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

    let chartingActor (chart: Chart) (pauseButton:System.Windows.Forms.Button) (mailbox:Actor<_>) =
        let maxPoints = 250
                
        let setPauseButtonText paused = pauseButton.Text <- if not paused then "PAUSE ||" else "RESUME ->"

        let setChartBoundaries (mapping:Map<string,Series>, noOfPts:int) =
            let allPoints = mapping |> Map.toList |> Seq.collect (fun (n, s) -> s.Points) |> (fun p -> HashSet<DataPoint>(p))
            if allPoints |> Seq.length > 2 then
                let yValues = allPoints |> Seq.collect (fun p -> p.YValues) |> Seq.toList
                chart.ChartAreas.[0].AxisX.Maximum <- float noOfPts
                chart.ChartAreas.[0].AxisX.Minimum <- (float noOfPts - float maxPoints)
                chart.ChartAreas.[0].AxisY.Maximum <- if yValues |> List.length > 0 then Math.Ceiling(yValues |> List.max) else 1.
                chart.ChartAreas.[0].AxisY.Minimum <- if yValues |> List.length > 0 then Math.Floor(yValues |> List.min) else 0.
            else
                ()

        let rec charting (mapping:Map<string,Series>, noOfPts:int) = 
            actor{
                let! message = mailbox.Receive ()
                match message with
                | InitializeChart series -> 
                    chart.Series.Clear ()
                    chart.ChartAreas.[0].AxisX.IntervalType <- DateTimeIntervalType.Number
                    chart.ChartAreas.[0].AxisY.IntervalType <- DateTimeIntervalType.Number
                    series |> Map.iter (fun k v -> 
                                            v.Name <- k
                                            chart.Series.Add v)
                    return! charting(series, noOfPts)
                | AddSeries series when not <| String.IsNullOrEmpty series.Name && mapping |> Map.containsKey series.Name |> not -> 
                    let newMapping = mapping.Add (series.Name, series)
                    chart.Series.Add series
                    setChartBoundaries (newMapping, noOfPts)
                    return! charting (newMapping, noOfPts)
                | RemoveSeries seriesName when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName -> 
                    chart.Series.Remove mapping.[seriesName] |> ignore
                    let newMapping = mapping.Remove seriesName
                    setChartBoundaries (newMapping, noOfPts)
                    return! charting (newMapping, noOfPts)
                | Metric(seriesName, counterValue) when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName -> 
                    let newNoOfPts = noOfPts + 1
                    let series = mapping.[seriesName] 
                    series.Points.AddXY (noOfPts, counterValue) |> ignore
                    while (series.Points.Count > maxPoints) do series.Points.RemoveAt 0
                    setChartBoundaries (mapping, newNoOfPts)
                    return! charting (mapping, newNoOfPts)
                | TogglePause -> 
                    setPauseButtonText true
                    return! paused (mapping, noOfPts)
            }
        and paused (mapping:Map<string,Series>, noOfPts:int) = 
            actor{
                let! message = mailbox.Receive ()
                match message with
                | TogglePause -> 
                    setPauseButtonText false
                    return! charting (mapping, noOfPts)
                | Metric(seriesName, counterValue) when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName -> 
                    let newNoOfPts = noOfPts + 1
                    let series = mapping.[seriesName]
                    series.Points.AddXY (newNoOfPts, 0.) |> ignore
                    while (series.Points.Count > maxPoints) do series.Points.RemoveAt 0
                    setChartBoundaries (mapping, newNoOfPts)
                    return! paused (mapping, newNoOfPts)
                | _ -> ()
                setChartBoundaries (mapping, noOfPts)
                return! paused (mapping, noOfPts)
            }

        charting (Map.empty<string, Series>, 0)


    let performanceCounterActor (seriesName:string)  (performanceCounterGenerator:unit -> PerformanceCounter) (mailbox:Actor<_>) =
        let counter = performanceCounterGenerator ()
        let cancelled = mailbox.Context.System.Scheduler.ScheduleTellRepeatedlyCancelable (TimeSpan.FromMilliseconds 250., 
                            TimeSpan.FromMilliseconds 250.,
                            mailbox.Self,
                            GatherMetrics,
                            ActorRefs.NoSender)

        mailbox.Defer (fun () -> 
            cancelled.Cancel()
            counter.Dispose () |> ignore
        )

        let rec loop(subscriptions) = actor {
            let! message = mailbox.Receive ()
            match box message :?> CounterMessage with
            | GatherMetrics -> 
                let msg = Metric(seriesName, float <| counter.NextValue ())
                subscriptions |> Seq.iter (fun subscriber -> subscriber <! msg)
                return! loop subscriptions
            | SubscribeCounter(s) -> 
                let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun i -> i <> s)
                return! loop (s::subscriptionsWithoutSubscriber)
            | UnsubscribeCounter(s) -> 
                let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun i -> i <> s)
                return! loop subscriptionsWithoutSubscriber
        }
        loop []

        
    let performanceCounterCoordinatorActor chartingActor (mailbox:Actor<_>) =
        let counterGenerators = Map.ofList [CounterType.Cpu, fun () -> new PerformanceCounter("Processor", "% Processor Time", "_Total", true)
                                            CounterType.Memory, fun () -> new PerformanceCounter("Memory", "% Committed Bytes In Use", true)
                                            CounterType.Disk, fun () -> new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)]
    
        let counterSeries = Map.ofList [CounterType.Cpu, fun () -> new Series(CounterType.Cpu.ToString (), ChartType = SeriesChartType.SplineArea, Color = Color.DarkGreen)
                                        CounterType.Memory, fun () -> new Series(CounterType.Memory.ToString (), ChartType = SeriesChartType.FastLine, Color = Color.MediumBlue)
                                        CounterType.Disk, fun () -> new Series(CounterType.Disk.ToString (), ChartType = SeriesChartType.SplineArea, Color = Color.DarkRed)]

        let rec loop(counterActors:Map<CounterType, IActorRef>) = actor {
            let! msg = mailbox.Receive ()
            
            match msg with
            | Watch counter when counterActors |> Map.containsKey counter |> not ->
                let counterName = counter.ToString ()
                let actor = spawn mailbox.Context (sprintf "counterActor-%s" counterName) (performanceCounterActor counterName counterGenerators.[counter])
                let newCounterActors = counterActors.Add (counter, actor)
                chartingActor <! AddSeries(counterSeries.[counter] ())
                newCounterActors.[counter] <! SubscribeCounter chartingActor
                return! loop newCounterActors
            | Watch counter ->
                chartingActor <! AddSeries(counterSeries.[counter] ())
                counterActors.[counter] <! SubscribeCounter chartingActor
            | Unwatch counter when (Map.containsKey counter counterActors) -> 
                chartingActor <! RemoveSeries((counterSeries.[counter] ()).Name)
                counterActors.[counter] <! UnsubscribeCounter chartingActor
            
            return! loop counterActors
        }
        loop Map.empty

        
    let buttonToggleActor coordinatorActor (myButton: System.Windows.Forms.Button) myCounterType isToggled (mailbox: Actor<_>) =
        let flipToggle (isOn) =
                let isToggledOn = not isOn
                myButton.Text <- (sprintf "%s (%s)" ((myCounterType.ToString ()).ToUpperInvariant ()) (if isToggledOn then "ON" else "OFF"))
                isToggledOn

        let rec loop (isToggledOn) = actor {
            let! message = mailbox.Receive ()
            match message with
            | Toggle when isToggledOn -> coordinatorActor <! Unwatch(myCounterType)
            | Toggle when not isToggledOn -> coordinatorActor <! Watch(myCounterType)
            | m -> mailbox.Unhandled m 
            return! loop (flipToggle isToggledOn)
        }
        loop isToggled
