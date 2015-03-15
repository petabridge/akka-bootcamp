module Actors

open System.Windows.Forms.DataVisualization.Charting
open Akka.Actor
open Akka.FSharp

let chartingActor (chart: Chart) message =
    match message with
    | InitializeChart series -> 
        chart.Series.Clear ()
        series |> Map.iter (fun k v -> 
            v.Name <- k
            chart.Series.Add(v))