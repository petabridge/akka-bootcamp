namespace ChartApp

open System.Collections.Generic
open System.Windows.Forms.DataVisualization.Charting
open Akka.Actor
open Akka.FSharp

[<AutoOpen>]
module Messages =
    type InitializeChart =
        | InitializeChart of initialSeries: Map<string, Series>

/// Actors used to intialize chart data
[<AutoOpen>]
module Actors =
    let chartingActor (chart: Chart) message =
        match message with
        | InitializeChart series ->
            chart.Series.Clear ()
            series |> Map.iter (fun k v ->
                v.Name <- k
                chart.Series.Add(v))