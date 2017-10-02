open System
open Akka.FSharp
open Akka.Actor
open WinTail

[<EntryPoint>]
let main argv =
    
    let strategy () = Strategy.OneForOne((fun ex ->
        match ex with
        | :? ArithmeticException -> Directive.Resume
        | :? NotSupportedException -> Directive.Stop
        | _ -> Directive.Restart), 10, TimeSpan.FromSeconds 30.)

    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let tailCoordinatorActor = spawnOpt myActorSystem "tailCoordinatorActor" (actorOf2 Actors.tailCoordinatorActor) [ SpawnOption.SupervisorStrategy(strategy ()) ]
    let fileValidatorActor = spawn myActorSystem "validationActor" (actorOf2 (Actors.fileValidatorActor consoleWriterActor tailCoordinatorActor))
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor fileValidatorActor))
    
    consoleReaderActor <! Start
    myActorSystem.WhenTerminated.Wait ()
    0