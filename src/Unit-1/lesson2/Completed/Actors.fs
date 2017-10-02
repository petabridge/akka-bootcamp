namespace WinTail

open System
open Akka.Actor
open Akka.FSharp

module Actors =

    let (|Message|Exit|) (str:string) =
        match str.ToLower () with
        | "exit" -> Exit
        | _ -> Message(str)

    // Active pattern matching to determine the charateristics of the message (empty, even length, or odd length)
    let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
        match msg.Length, msg.Length % 2 with
        | 0, _ -> EmptyMessage
        | _, 0 -> MessageLengthIsEven
        | _, _ -> MessageLengthIsOdd

    // Print instructions to the console
    let doPrintInstructions () =
        Console.WriteLine "Write whatever you want into the console!"
        Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
        Console.WriteLine "Type 'exit' to quit this application at any time.\n"
    
    let consoleReaderActor (consoleWriter: IActorRef) (mailbox: Actor<_>) message =
    
        // Read and validate the input from the console, then signal the appropriate action (success and continue processing, or error)
        let getAndValidateInput () =
            let line = Console.ReadLine ()
            match line with
            | Exit -> mailbox.Context.System.Terminate () |> ignore
            | Message(input) -> 
                match input with
                | EmptyMessage ->
                    mailbox.Self <! InputError ("No input received.", ErrorType.Null)
                | MessageLengthIsEven ->
                    consoleWriter <! InputSuccess ("Thank you! The message was valid.")
                    mailbox.Self <! Continue
                | _ ->
                    mailbox.Self <! InputError ("The message is invalid (odd number of characters)!", ErrorType.Validation)

        match box message with
        | :? Command as command -> 
            match command with
            | Start -> doPrintInstructions ()
            | _ -> ()
        | :? InputResult as inputResult ->
            match inputResult with
            | InputError(_, _) as error -> consoleWriter <! error
            | _ -> ()
        | _ -> ()
        
        getAndValidateInput ()
            
    let consoleWriterActor message =
        
        // Active pattern matching to define if a number is even or odd
        let (|Even|Odd|) n = if n % 2 = 0 then Even else Odd
    
        let printInColor color message =
            Console.ForegroundColor <- color
            Console.WriteLine (message.ToString ())
            Console.ResetColor ()

        match box message with
        | :? InputResult as inputResult ->
            match inputResult with
            | InputError (reason,_) -> printInColor ConsoleColor.Red reason
            | InputSuccess reason -> printInColor ConsoleColor.Green reason
        | _ -> printInColor ConsoleColor.Yellow (message.ToString ())