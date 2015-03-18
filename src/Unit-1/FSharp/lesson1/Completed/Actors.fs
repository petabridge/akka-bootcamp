module Actors

open System
open Akka.Actor
open Akka.FSharp

[<Literal>]
let ExitCommand = "exit"

[<Literal>]
let ContinueCommand = "continue"

let consoleReaderActor (consoleWriter: ActorRef) (mailbox: Actor<_>) message = 
    let line = Console.ReadLine ()
    match line.ToLower () with
    | ExitCommand -> mailbox.Context.System.Shutdown ()
    | _ -> 
        consoleWriter <! line
        mailbox.Self  <! ContinueCommand

let consoleWriterActor message = 
    let (|Even|Odd|) n = if n % 2 = 0 then Even else Odd
    
    let printInColor color message =
        Console.ForegroundColor <- color
        Console.WriteLine (message.ToString ())
        Console.ResetColor ()

    match message.ToString().Length with
    | 0    -> printInColor ConsoleColor.DarkYellow "Please provide an input.\n"
    | Even -> printInColor ConsoleColor.Red "Your string had an even # of characters.\n"
    | Odd  -> printInColor ConsoleColor.Green "Your string had an odd # of characters.\n"