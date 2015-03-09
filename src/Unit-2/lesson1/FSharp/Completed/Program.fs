module Program

open System
open System.Drawing
open System.Windows.Forms
open System.Windows.Forms.DataVisualization.Charting
open Akka.Util.Internal
open Akka.Actor
open Akka.FSharp
open Akka.Configuration.Hocon
open System.Configuration
open Actors

let section = ConfigurationManager.GetSection "akka" :?> AkkaConfigurationSection
let chartActors = System.create "ChartActors" section.AkkaConfig // TODO: use Configuration.load when this fix is released https://github.com/akkadotnet/akka.net/issues/671

Application.EnableVisualStyles ()
Application.SetCompatibleTextRenderingDefault false

let seriesCounter = AtomicCounter(1)
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

let chartActor = spawn chartActors "charting" (actorOf (Actors.chartingActor sysChart))
let series = ChartDataHelper.randomSeries ("FakeSeries" + (seriesCounter.GetAndIncrement ()).ToString ()) None None
chartActor <! InitializeChart(Map.ofList [(series.Name, series)])

[<STAThread>]    
do Application.Run (form)
