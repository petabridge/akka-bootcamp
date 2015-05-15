[<AutoOpen>]
module Messages

open Akka.Actor

type ProcessCommand = 
    | ContinueProcessing

type ErrorType =
    | Null
    | Validation

type InputResult =
    | InputSuccess of reason: string
    | InputError of reason: string * errorType: ErrorType

type TailCommand =
    | StartTail of filePath: string * reporterActor: IActorRef
    | StopTail of  filePath: string

type FileCommand =
    | FileWrite of fileName: string
    | FileError of fileName: string * reason: string
    | InitialRead of fileName: string * text: string