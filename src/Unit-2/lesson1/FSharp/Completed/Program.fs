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
let config = section.AkkaConfig
let chartActors = ActorSystem.Create ("ChartActors", config)

Application.EnableVisualStyles ()
Application.SetCompatibleTextRenderingDefault false

let seriesCounter = new AtomicCounter(1)
let sysChart = new Chart()
let form = new Form()
form.Visible <- true 
let chartArea1 = new ChartArea()
let legend1 = new Legend()
let series1 = new Series()
sysChart.BeginInit ()
form.SuspendLayout ()
chartArea1.Name <- "ChartArea1"
sysChart.ChartAreas.Add chartArea1
sysChart.Dock <- DockStyle.Fill
legend1.Name <- "Legend1"
sysChart.Legends.Add legend1
sysChart.Location <- Point(0, 0)
sysChart.Name <- "sysChart"
series1.ChartArea <- "ChartArea1"
series1.Legend <- "Legend1"
series1.Name <- "Series1"
sysChart.Series.Add series1
sysChart.Size <- new Size(684, 446)
sysChart.TabIndex <- 0;
sysChart.Text <- "sysChart"
form.AutoScaleDimensions <- new SizeF(6.F, 13.F)
form.AutoScaleMode <- AutoScaleMode.Font
form.ClientSize <- Size(684, 446)
form.Controls.Add sysChart
form.Name <- "Main"
form.Text <- "System Metrics"
sysChart.EndInit ()
form.ResumeLayout false

let chartActor = spawn chartActors "charting" (actorOf (Actors.chartingActor sysChart))
let series = ChartDataHelper.randomSeries ("FakeSeries" + (seriesCounter.GetAndIncrement ()).ToString ()) None None
chartActor <! InitializeChart(Map.ofList [(series.Name, series)])

[<STAThread>]    
do Application.Run (form)
