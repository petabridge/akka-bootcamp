﻿open System
open Akka.FSharp
open Akka.Actor

[<EntryPoint>]
let main argv = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawnOpt myActorSystem "ConsoleWriterActor" (actorOf Actors.consoleWriterActor) [SpawnOption.SupervisorStrategy(SupervisorStrategy.DefaultStrategy)]
    let validationActor = spawnOpt myActorSystem "ValidationActor" (actorOf2 (Actors.validationActor consoleWriterActor)) [SpawnOption.Deploy(Deploy.Local)]
    let consoleReaderActor = spawnOpt myActorSystem "ConsoleReaderActor" (actorOf2 (Actors.consoleReaderActor validationActor)) [SpawnOption.Router(Akka.Routing.RouterConfig.NoRouter)]
    consoleReaderActor <! Actors.StartCommand
    myActorSystem.AwaitTermination ()
    0