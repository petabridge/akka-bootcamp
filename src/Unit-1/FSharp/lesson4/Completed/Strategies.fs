module Strategies

open System
open Akka.Actor
open Akka.FSharp

let tailCoordinatorStrategy () = 
    Strategy.OneForOne((fun ex ->
        match ex with 
        | :? ArithmeticException  -> Directive.Resume
        | :? NotSupportedException -> Directive.Stop
        | _ -> Directive.Restart), 10, TimeSpan.FromSeconds(30.))