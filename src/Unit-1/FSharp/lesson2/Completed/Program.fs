open System
open Akka.FSharp
open Akka.Actor

[<EntryPoint>]
let main argv = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "ConsoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "ConsoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))
    consoleReaderActor <! Actors.StartCommand
    myActorSystem.AwaitTermination ()
    0