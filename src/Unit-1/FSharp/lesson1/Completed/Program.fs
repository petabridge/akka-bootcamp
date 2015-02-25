open System
open Akka.FSharp
open Akka.FSharp.Spawn
open Akka.Actor

let printInstructions () =
    Console.WriteLine "Write whatever you want into the console!"
    Console.Write "Some lines will appear as"
    Console.ForegroundColor <- ConsoleColor.DarkRed
    Console.Write " red "
    Console.ResetColor ()
    Console.Write " and others will appear as"
    Console.ForegroundColor <- ConsoleColor.Green
    Console.Write " green! "
    Console.ResetColor ()
    Console.WriteLine ()
    Console.WriteLine ()
    Console.WriteLine "Type 'exit' to quit this application at any time.\n"

[<EntryPoint>]
let main argv = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    printInstructions ()
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))
    consoleReaderActor <! "start"
    myActorSystem.AwaitTermination ()
    0