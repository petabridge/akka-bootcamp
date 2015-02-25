open System
open Akka.FSharp
open Akka.Actor

[<EntryPoint>]
let main argv = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawnOpt myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor) [SpawnOption.SupervisorStrategy(SupervisorStrategy.DefaultStrategy)]
    let tailCoordinatorActor = spawnOpt myActorSystem "tailCoordinatorActor" (actorOf2 Actors.tailCoordinatorActor) [SpawnOption.SupervisorStrategy(Strategies.tailCoordinatorStrategy ())]
    let fileValidatorActor = spawnOpt myActorSystem "validationActor" (actorOf2 (Actors.fileValidatorActor consoleWriterActor)) [SpawnOption.Deploy(Deploy.Local)]
    let consoleReaderActor = spawnOpt myActorSystem "consoleReaderActor" (actorOf2 Actors.consoleReaderActor) [SpawnOption.Router(Akka.Routing.RouterConfig.NoRouter)]
    consoleReaderActor <! Actors.StartCommand
    myActorSystem.AwaitTermination ()
    0