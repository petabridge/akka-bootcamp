namespace ChartApp

open Akka.Actor
open Akka.FSharp
open System.Drawing
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting
open Akka.Util.Internal

[<AutoOpen>]
module Form =
    let sysChart = new Chart(Name = "sysChart", Text = "sysChart", Dock = DockStyle.Fill, Location = Point(0, 0), Size = Size(684, 446), TabIndex = 0)
    let form = new Form(Name = "Main", Visible = true, Text = "System Metrics", AutoScaleDimensions = SizeF(6.F, 13.F), AutoScaleMode = AutoScaleMode.Font, ClientSize = Size(684, 446))
    let chartArea1 = new ChartArea(Name = "ChartArea1")
    let legend1 = new Legend(Name = "Legend1")
    let series1 = new Series(Name = "Series1", ChartArea = "ChartArea1", Legend = "Legend1")
    sysChart.BeginInit ()
    form.SuspendLayout ()
    sysChart.ChartAreas.Add chartArea1
    sysChart.Legends.Add legend1
    sysChart.Series.Add series1
    form.Controls.Add sysChart
    sysChart.EndInit ()
    form.ResumeLayout false

    let load (myActorSystem:ActorSystem) =
        let chartActor = spawn myActorSystem "charting" (actorOf (Actors.chartingActor sysChart))
        let series = ChartDataHelper.randomSeries "FakeSeries1" None None
        chartActor <! InitializeChart(Map.ofList [(series.Name, series)])
        form