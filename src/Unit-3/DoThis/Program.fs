module Program

open System
open System.Windows.Forms
open GithubActors

Application.EnableVisualStyles ()
Application.SetCompatibleTextRenderingDefault false

[<STAThread>]
do Application.Run (GithubAuthForm.load ())