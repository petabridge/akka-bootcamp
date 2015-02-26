module Actors

open System
open System.IO
open System.Text
open Akka.Actor
open Akka.FSharp
open System.Linq.Expressions
open Microsoft.FSharp.Linq

[<Literal>]
let StartCommand = "start"
[<Literal>]
let ExitCommand = "exit"
[<Literal>]
let EmptyCommand = ""

let consoleReaderActor (mailbox: Actor<_>) message = 
    let doPrintInstructions () = Console.WriteLine "Please provide the URI of a log file on disk.\n"

    let getAndValidateInput () = 
        let message = Console.ReadLine ()
        match message.ToLower () with
        | ExitCommand -> mailbox.Context.System.Shutdown ()
        | _ -> select "/user/validationActor" mailbox.Context.System <! message

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

let fileValidatorActor (consoleWriter: ActorRef) (mailbox: Actor<_>) message = 
    let (|IsFileUri|_|) path = if File.Exists path then Some path else None
    
    match message with
    | EmptyCommand -> 
        consoleWriter <! InputError("Input was blank. Please try again.\n", ErrorType.Null)
        mailbox.Sender () <! ContinueProcessing
    | IsFileUri _ -> 
        consoleWriter <! InputSuccess (sprintf "Starting processing for %s" message)
        select "user/tailCoordinatorActor" mailbox.Context.System <! StartTail(message, consoleWriter)
    | _ -> 
        consoleWriter <! InputError (sprintf "%s is not an existing URI on disk." message, ErrorType.Validation)
        mailbox.Sender () <! ContinueProcessing

type TailActor(reporter, filePath) as this =
    inherit UntypedActor()

    let mutable fileStreamReader = null
    let mutable observer = Some <| new FileObserver(this.Self, Path.GetFullPath(filePath))

    override this.PreStart () =
        observer |> Option.map (fun o -> o.Start ()) |> ignore
        let fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        fileStreamReader <- new StreamReader(fileStream, Encoding.UTF8)
        let text = fileStreamReader.ReadToEnd ()
        this.Self <! InitialRead(filePath, text)
    
    override this.OnReceive message =
        match message :?> FileCommand with
        | FileWrite(_) -> 
            let text = fileStreamReader.ReadToEnd ()
            if not <| String.IsNullOrEmpty text then reporter <! text else ()
        | FileError(_,reason) -> reporter <! sprintf "Tail error: %s" reason
        | InitialRead(_,text) -> reporter <! text
    
    override this.PostStop () =
        observer |> Option.map (fun o -> (o :> IDisposable).Dispose ()) |> ignore
        observer <- None
        fileStreamReader.Close ()
        fileStreamReader.Dispose ()
        base.PostStop ()

let tailCoordinatorActor (mailbox: Actor<_>) message =
    match message with
    | StartTail(filePath,reporter) -> spawnObj mailbox.Context "tailActor" (<@ (fun () -> new TailActor(reporter, filePath)) @>) |> ignore
    | _ -> ()