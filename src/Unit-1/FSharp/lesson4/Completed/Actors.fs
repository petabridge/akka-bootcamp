module Actors

open System
open System.IO
open System.Text
open Akka.Actor
open Akka.FSharp

[<Literal>]
let StartCommand = "start"
[<Literal>]
let ExitCommand = "exit"
[<Literal>]
let EmptyCommand = ""

let consoleReaderActor (validation: ActorRef) (mailbox: Actor<_>) message = 
    let doPrintInstructions () = Console.WriteLine "Please provide the URI of a log file on disk.\n"

    let getAndValidateInput () = 
        let message = Console.ReadLine ()
        match message.ToLower () with
        | ExitCommand -> mailbox.Context.System.Shutdown ()
        | _ -> validation <! message

    match (message.ToString ()).ToLower () with
    | StartCommand _ -> doPrintInstructions ()
    | _ -> ()
    getAndValidateInput ()

let consoleWriterActor (message: 'a) = 
    let (|Even|Odd|) n = if n % 2 = 0 then Even else Odd
    
    let printInColor color message =
        Console.ForegroundColor <- color
        Console.WriteLine (message.ToString ())
        Console.ResetColor ()

    match box message with
    | :? InputResult as inputResult ->
        match inputResult with
        | InputError(reason,_) -> printInColor ConsoleColor.Red reason
        | InputSuccess reason -> printInColor ConsoleColor.Green reason
    | _ -> printInColor ConsoleColor.Black (message.ToString ())

let fileValidatorActor (consoleWriter: ActorRef) (tailCordinator: ActorRef) (mailbox: Actor<_>) message = 
    let (|IsFileUri|_|) path = if File.Exists path then Some path else None
    
    match message with
    | EmptyCommand -> 
        consoleWriter <! InputError("Input was blank. Please try again.\n", ErrorType.Null)
        mailbox.Sender () <! ContinueProcessing
    | IsFileUri _ -> 
        consoleWriter <! InputSuccess (sprintf "Starting processing for %s" message)
        tailCordinator <! StartTail(message, consoleWriter)
    | _ -> 
        consoleWriter <! InputError (sprintf "%s is not an existing URI on disk." message, ErrorType.Validation)
        mailbox.Sender () <! ContinueProcessing

let tailActorBuilder (reporter: ActorRef) (filePath: string) (mailbox: Actor<_>) =
    let observer = new FileObserver(mailbox.Self, Path.GetFullPath(filePath))
    observer.Start ()
    let fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    let fileStreamReader = new StreamReader(fileStream, Encoding.UTF8)
    let text = fileStreamReader.ReadToEnd ()
    mailbox.Self <! InitialRead(filePath, text)
    
    let tailActor message =
        match message with
        | FileWrite(_) -> 
            let text = fileStreamReader.ReadToEnd ()
            if not <| String.IsNullOrEmpty text then reporter <! text else ()
        | FileError(_,reason) -> reporter <! sprintf "Tail error: %s" reason
        | InitialRead(_,text) -> reporter <! text

    let rec loop () = actor {
        let! message = mailbox.Receive ()
        tailActor <| message
        return! loop () }
    loop ()

let tailCoordinatorActor (mailbox: Actor<_>) message =
    match message with
    | StartTail(filePath,reporter) -> spawn mailbox.Context "tailActor" (tailActorBuilder reporter filePath) |> ignore
    | _ -> ()