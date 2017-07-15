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
    let btnAddSeries = new Button(Name = "btnAddSeries", Text = "Add Series", Location = Point(540, 300), Size = Size(100, 40), TabIndex = 1, UseVisualStyleBackColor = true)
    sysChart.BeginInit ()
    form.SuspendLayout ()
    sysChart.ChartAreas.Add chartArea1
    sysChart.Legends.Add legend1
    sysChart.Series.Add series1
    form.Controls.Add btnAddSeries
    form.Controls.Add sysChart
    sysChart.EndInit ()
    form.ResumeLayout false

    let load (myActorSystem:ActorSystem) = 
        let chartActor = spawn myActorSystem "charting" (Actors.chartingActor sysChart)
        let series = ChartDataHelper.randomSeries ("FakeSeries1" ) None None
        chartActor <! InitializeChart(Map.ofList [(series.Name, series)])

        btnAddSeries.Click.Add (fun _ -> 
            let newSeriesName = sprintf "FakeSeries %i" (sysChart.Series.Count + 1)    
            let newSeries = ChartDataHelper.randomSeries newSeriesName None None
            chartActor <! AddSeries newSeries
        )

        form