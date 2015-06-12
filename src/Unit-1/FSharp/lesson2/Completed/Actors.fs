module Actors

open System
open Akka.Actor
open Akka.FSharp

[<Literal>]
let StartCommand = "start"
[<Literal>]
let ExitCommand = "exit"
[<Literal>]
let EmptyCommand = ""

let consoleReaderActor (consoleWriter: ActorRef) (mailbox: Actor<_>) message = 
    let (|ValidMessage|_|) msg = if msg.ToString().Length % 2 = 0 then Some msg else None

    let doPrintInstructions () =
        Console.WriteLine "Write whatever you want into the console!"
        Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
        Console.WriteLine "Type 'exit' to quit this application at any time.\n"

    let getAndValidateInput () = 
        let line = Console.ReadLine()
        match line.ToLower () with
        | EmptyCommand -> mailbox.Self <! InputError ("No input received.", ErrorType.Null)
        | ExitCommand -> mailbox.Context.System.Shutdown ()
        | ValidMessage _ -> 
            consoleWriter <! InputSuccess ("Thank you! Message was valid.")
            mailbox.Self <! ContinueProcessing
        | _ -> mailbox.Self <! InputError ("Invalid: input had odd number of characters.", ErrorType.Validation)

    match box message with
    | :? string as command ->
        match command with
        | StartCommand -> doPrintInstructions ()
        | _ -> ()
    | :? InputResult as inputResult ->
        match inputResult with
        | InputError(_,_) as error -> consoleWriter <! error
        | _ -> ()
    | _ -> ()
    getAndValidateInput ()

let consoleWriterActor message = 
    
    let printInColor color message =
        Console.ForegroundColor <- color
        Console.WriteLine (message.ToString ())
        Console.ResetColor ()

    match box message with
    | :? InputResult as inputResult ->
        match inputResult with
        | InputError (reason,_) -> printInColor ConsoleColor.Red reason
        | InputSuccess reason -> printInColor ConsoleColor.Green reason
    | _ -> printInColor ConsoleColor.Black (message.ToString ())
