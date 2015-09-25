[<AutoOpen>]
module AkkaExtensions

open System
open System.Linq
open System.Linq.Expressions
open Microsoft.FSharp.Linq
open Akka.Actor
open Akka.FSharp
open Akka.FSharp.Linq
open Akka.Dispatch
open System.Threading
open System.Reflection
open System.Collections.Concurrent

let private taskContinuation (task: System.Threading.Tasks.Task) : unit =
    match task.IsFaulted with
    | true -> raise task.Exception
    | _ -> ()

let toExpression<'Actor>(f : Quotations.Expr<(unit -> 'Actor)>) = 
    match QuotationEvaluator.ToLinqExpression f with
    | Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]) -> 
        Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<'Actor>>
    | _ -> failwith "Doesn't match"

let spawnObj<'Actor when 'Actor :> ActorBase> (actorFactory : ActorRefFactory) (name : string) (f : Quotations.Expr<(unit -> 'Actor)>) : ActorRef = 
    let e = toExpression<'Actor> f
    actorFactory.ActorOf((Props.Create e), name)

let scheduleCancellableTell (after: TimeSpan) (every: TimeSpan) (message: 'Message) (receiver: ActorRef) (scheduler: Scheduler) (cancellationToken: CancellationToken): Async<unit> =
        Async.AwaitTask (scheduler.Schedule(after, every, receiver, message, cancellationToken).ContinueWith taskContinuation)

type ConcurrentQueueMailbox with
    member private this.GetQueue () = typedefof<ConcurrentQueueMailbox>.GetField("_userMessages", BindingFlags.NonPublic ||| BindingFlags.Instance).GetValue this :?> ConcurrentQueue<Envelope>
    member private this.SetQueue queue = typedefof<ConcurrentQueueMailbox>.GetField("_userMessages", BindingFlags.NonPublic ||| BindingFlags.Instance).SetValue (this, queue)
    member this.EnqueueFirst message =
        let queue = (this.GetQueue ()).ToList ()
        queue.Insert(0, message)
        let newQueue = new ConcurrentQueue<Envelope>(queue)
        this.SetQueue newQueue

type Actor<'Message> with
    member private this.GetActorCell () = this.Context :?> ActorCell
    member this.EnqueueFirst messages = 
        let actorCell = this.GetActorCell ()
        let mailbox = actorCell.Mailbox :?> ConcurrentQueueMailbox
        messages |> Seq.toList |> List.map mailbox.EnqueueFirst |> ignore
    member this.GetEnvelope (message: 'Message) = 
        let actorCell = this.GetActorCell ()
        Envelope(Message = message, Sender = actorCell.Sender)