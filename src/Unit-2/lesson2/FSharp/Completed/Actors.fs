module Actors

open System
open System.Windows.Forms.DataVisualization.Charting
open Akka.Actor
open Akka.FSharp

let chartingActor (chart: Chart) =
    let seriesIndex = ref Map.empty  // 'mutable' can be used with F# v4.0 http://blogs.msdn.com/b/fsharpteam/archive/2014/11/12/announcing-a-preview-of-f-4-0-and-the-visual-f-tools-in-vs-2015.aspx 
    
    let addSeries (series: Series) = 
        seriesIndex := (!seriesIndex).Add (series.Name, series)
        chart.Series.Add series
    
    (fun message -> 
        match message with
        | InitializeChart series -> 
            chart.Series.Clear ()
            series |> Map.iter (fun k v -> 
                v.Name <- k
                v |> addSeries)
        | AddSeries series when 
            not (String.IsNullOrEmpty series.Name) &&
            not (!seriesIndex |> Map.containsKey series.Name) -> addSeries series
        | _ -> ())