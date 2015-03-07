module ChartDataHelper

open System.Windows.Forms.DataVisualization.Charting
open Akka.Util

let randomSeries (seriesName: string) (seriesType: SeriesChartType option) (points: int option) =
    let seriesChartType = defaultArg seriesType SeriesChartType.Line
    let seriesPoints = defaultArg points 100 
    let series = new Series(seriesName, ChartType = seriesChartType);
    [0..seriesPoints]
    |> List.iter (fun i -> 
            let rng = ThreadLocalRandom.Current.NextDouble ()
            series.Points.Add(new DataPoint(float i, float (2.0 * sin(rng) + sin(rng / 4.5))))
            series.BorderWidth <- 3)
    series
