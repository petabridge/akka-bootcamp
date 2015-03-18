[<AutoOpen>]
module AkkaExtensions

open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq
open Akka.Actor
open Akka.FSharp.Linq

let toExpression<'Actor>(f : Quotations.Expr<(unit -> 'Actor)>) = 
    match QuotationEvaluator.ToLinqExpression f with
    | Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]) -> 
        Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<'Actor>>
    | _ -> failwith "Doesn't match"

let spawnObj<'Actor when 'Actor :> ActorBase> (actorFactory : ActorRefFactory) (name : string) (f : Quotations.Expr<(unit -> 'Actor)>) : ActorRef = 
    let e = toExpression<'Actor> f
    actorFactory.ActorOf((Props.Create e), name)
