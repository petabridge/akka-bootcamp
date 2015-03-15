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
let btnCpu = new Button(Name = "btnCpu", Text = "CPU (ON)", Location = Point(562, 274), Size = Size(110, 41), TabIndex = 1, UseVisualStyleBackColor = true)
let btnMemory = new Button(Name = "btnMemory", Text = "MEMORY (OFF)", Location = Point(562, 321), Size = Size(110, 41), TabIndex = 2, UseVisualStyleBackColor = true)
let btnDisk = new Button(Name = "btnDisk", Text = "DISK (OFF)", Location = Point(562, 368), Size = Size(110, 41), TabIndex = 3, UseVisualStyleBackColor = true)
sysChart.BeginInit ()
form.SuspendLayout ()
sysChart.ChartAreas.Add chartArea1
sysChart.Legends.Add legend1
sysChart.Series.Add series1
form.Controls.Add btnCpu
form.Controls.Add btnMemory
form.Controls.Add btnDisk
form.Controls.Add sysChart
sysChart.EndInit ()
form.ResumeLayout false

let chartActor = spawn chartActors "charting" (actorOf (Actors.chartingActor sysChart))
chartActor <! InitializeChart(Map.empty)

let coordinatorActor = spawn chartActors "counters" (actorOf2 (Actors.performanceCounterCoordinatorActor chartActor))

let toggleActors = Map.ofList [(CounterType.Cpu, spawnOpt chartActors "cpuCounter" (actorOf2 (Actors.buttonToggleActor coordinatorActor btnCpu CounterType.Cpu false)) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])
                               (CounterType.Memory, spawnOpt chartActors "memoryCounter" (actorOf2 (Actors.buttonToggleActor coordinatorActor btnMemory CounterType.Memory false)) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])
                               (CounterType.Disk, spawnOpt chartActors "diskCounter" (actorOf2 (Actors.buttonToggleActor coordinatorActor btnDisk CounterType.Disk false)) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])]

toggleActors.[CounterType.Cpu] <! Toggle

btnCpu.Click.Add (fun _ -> toggleActors.[CounterType.Cpu] <! Toggle)
btnMemory.Click.Add (fun _ -> toggleActors.[CounterType.Memory] <! Toggle)
btnDisk.Click.Add (fun _ -> toggleActors.[CounterType.Disk] <! Toggle)

[<STAThread>]    
do Application.Run (form)