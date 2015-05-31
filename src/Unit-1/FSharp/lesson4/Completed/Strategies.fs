module Strategies

open System
open Akka.Actor
open Akka.FSharp

let tailCoordinatorStrategy () = 
    Strategy.OneForOne((function
        | :? ArithmeticException  -> Directive.Resume
        | :? NotSupportedException -> Directive.Stop
        | _ -> Directive.Restart), 10, TimeSpan.FromSeconds(30.))