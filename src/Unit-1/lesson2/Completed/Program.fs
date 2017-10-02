open System
open Akka.FSharp
open Akka.Actor
open WinTail

[<EntryPoint>]
let main argv =
    
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))

    consoleReaderActor <! Start

    myActorSystem.WhenTerminated.Wait ()
    0