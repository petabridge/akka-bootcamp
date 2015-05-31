﻿open System
open Akka.Actor
open Akka.FSharp

let printInstructions () =
    Console.WriteLine "Write whatever you want into the console!"
    Console.Write "Some lines will appear as"
    Console.ForegroundColor <- ConsoleColor.DarkRed
    Console.Write " red "
    Console.ResetColor()
    Console.Write "and others will appear as"
    Console.ForegroundColor <- ConsoleColor.Green
    Console.Write " green! "
    Console.ResetColor()
    Console.WriteLine()
    Console.WriteLine()
    Console.WriteLine "Type 'exit' to quit this application at any time.\n"

[<EntryPoint>]
let main argv = 
    // initialize an actor system
    // YOU NEED TO FILL IN HERE
    
    printInstructions()
    
    // make your first actors using the 'spawn' function
    // YOU NEED TO FILL IN HERE

    // tell the consoleReader actor to begin
    // YOU NEED TO FILL IN HERE

    myActorSystem.AwaitTermination()
    0
