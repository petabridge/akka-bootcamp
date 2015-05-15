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

let consoleReaderActor (validation: IActorRef) (mailbox: Actor<_>) message = 
    let doPrintInstructions () =
        Console.WriteLine "Write whatever you want into the console!"
        Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
        Console.WriteLine "Type 'exit' to quit this application at any time.\n"

    let getAndValidateInput () = 
        let line = Console.ReadLine()
        match line.ToLower() with
        | ExitCommand -> mailbox.Context.System.Shutdown()
        | _ -> validation <! line

    match message.ToString().ToLower() with
    | StartCommand -> doPrintInstructions()
    | _ -> ()
    getAndValidateInput ()

let consoleWriterActor message = 
    let (|Even|Odd|) n = if n % 2 = 0 then Even else Odd
    
    let printInColor color message =
        Console.ForegroundColor <- color
        Console.WriteLine(message.ToString())
        Console.ResetColor()

    match box message with
    | :? InputResult as inputResult ->
        match inputResult with 
        | InputError(reason,_) -> printInColor ConsoleColor.Red reason
        | InputSuccess(reason) -> printInColor ConsoleColor.Green reason
    | _ -> printInColor ConsoleColor.Black (message.ToString())

let validationActor (consoleWriter: IActorRef) (mailbox: Actor<_>) message = 
    let (|ValidMessage|_|) msg = if (msg.ToString()).Length % 2 = 0 then Some msg else None
    
    match message with
    | EmptyCommand -> consoleWriter <! InputError("No input received.", ErrorType.Null)
    | ValidMessage _ -> consoleWriter <! InputSuccess("Thank you! Message was valid.")
    | _ -> consoleWriter <! InputError("Invalid: input had odd number of characters.", ErrorType.Validation)
    mailbox.Sender() <! ContinueProcessing
