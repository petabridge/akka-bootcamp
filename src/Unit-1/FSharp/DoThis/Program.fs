open System
open Akka.FSharp
open Akka.FSharp.Spawn
open Akka.Actor
open WinTail

let printInstructions () =
    Console.WriteLine "Write whatever you want into the console!"
    Console.Write "Some lines will appear as"
    Console.ForegroundColor <- ConsoleColor.Red
    Console.Write " red"
    Console.ResetColor ()
    Console.Write " and others will appear as"
    Console.ForegroundColor <- ConsoleColor.Green
    Console.Write " green! "
    Console.ResetColor ()
    Console.WriteLine ()
    Console.WriteLine ()
    Console.WriteLine "Type 'exit' to quit this application at any time.\n"

[<EntryPoint>]
let main argv = 
    // initialize an actor system
    // YOU NEED TO FILL IN HERE
        
    printInstructions ()
    
    // make your first actors using the 'spawn' function
    // YOU NEED TO FILL IN HERE

    // tell the consoleReader actor to begin
    // YOU NEED TO FILL IN HERE

    myActorSystem.WhenTerminated.Wait ()
    0
