module Program

open System
open System.Windows.Forms
open Akka.Actor
open Akka.FSharp
open Akka.Configuration.Hocon
open System.Configuration
open ChartApp

let chartActors = System.create "ChartActors" (Configuration.load())

Application.EnableVisualStyles ()
Application.SetCompatibleTextRenderingDefault false

[<STAThread>]
do Application.Run (Form.load chartActors)