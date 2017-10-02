namespace WinTail

open Akka.Actor

[<AutoOpen>]
module Messages =
    type Command = 
    | Start
    | Continue
    | Message of string
    | Exit

    type ErrorType =
    | Null
    | Validation

    type InputResult =
    | InputSuccess of string
    | InputError of reason: string * errorType: ErrorType

    type TailCommand =
    | StartTail of filePath: string * reporterActor: IActorRef
    | StopTail of  filePath: string

    type FileCommand =
    | FileWrite of fileName: string
    | FileError of fileName: string * reason: string
    | InitialRead of fileName: string * text: string