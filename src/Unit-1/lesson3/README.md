# Lesson 1.3: Using `IActorRef`s
In this lesson, we will review/reinforce the different ways you can create actors and send them messages. This lesson is more conceptual and has less coding for you to do, but it's an essential foundation and key to understanding the code you will see down the line.

In this lesson, the code has changed a bit. The change is that the `consoleReaderActor` no longer does any validation work, but instead, just passes off the messages it receives from the console to another actor for validation (the `validationActor`).

## Key concepts / background
### `IActorRef`s
#### What is an `IActorRef`?
An `IActorRef` is a reference or handle to an actor. The purpose of an `IActorRef` is to support sending messages to an actor through the `ActorSystem`. You never talk directly to an actor — you send messages to its `IActorRef` and the `ActorSystem` takes care of delivering those messages for you.

#### WTF? I don't actually talk to my actors? Why not?
You do talk to them, just not directly :) You have to talk to them via the intermediary of the `ActorSystem`.

Here are two of the reasons why it is an advantage to send messages to an `IActorRef` and let the underlying `ActorSystem` do the work of getting the messages to the actual actor.
  - It gives you better information to work with and messaging semantics. The `ActorSystem` wraps all messages in an `Envelope` that contains metadata about the message. This metadata is automatically unpacked and made available in the context of your actor.
  - It allows "location transparency": this is a fancy way of saying that you don't have to worry about which process or machine your actor lives in. Keeping track of all this is the system's job. This is essential for allowing remote actors, which is how you can scale an actor system up to handle massive amounts of data (e.g. have it work on multiple machines in a cluster). More on this later.

#### How do I know my message got delivered to the actor?
For now, this is not something you should worry about. The underlying `ActorSystem` of Akka.NET itself provides mechanisms to guarantee this, but `GuaranteedDeliveryActors` are an advanced topic.

For now, just trust that delivering messages is the `ActorSystem`s job, not yours. Trust, baby. :)

#### Okay, fine, I'll let the system deliver my messages. So how do I get an `IActorRef`?
There are two ways to get an `IActorRef`.

##### 1) Create the actor
Actors form intrinsic supervision hierarchies (we cover those in detail in lesson 4). This means there are "top level" actors, which essentially report directly to the `ActorSystem` itself, and there are "child" actors, which report to other actors.

To make an actor, you have to create it from its context. We have been using the ``actorOf`` and ``actorOf2`` functions provided by Akka.FSharp, which are shorthand functions that define our actor behavior. And **you've already done this!** Remember this?
```fsharp
// assume we have an existing actor system, "myActorSystem"
let myFirstActor = spawn myActorSystem "myFirstActor" (actorOf firstActor)
```

Without the use of the shorthand functions ``actorOf`` and ``actorOf2``, you can define the actor by using an `actor` computation expression. The expression is expected to be represented as self-invoking recursive function. It is important to remember that each actor returning point should point to the next recursive function call - any other value returned will result in the current actor being stopped.
```fsharp
let firstActor (mailbox:Actor<_>) =
  let rec loop() = actor {
      let! message = mailbox.Receive()
      // handle an incoming message
      return! loop()
  }
  loop()

// assume we have an existing actor system, "myActorSystem"
let myFirstActor = spawn myActorSystem "myFirstActor" firstActor
```

As shown in the examples above, you create an actor in the context of the actor that will supervise it (almost always). When you create the actor on the `ActorSystem` directly, it is a top-level actor.

You make child actors the same way, except you create them from another actor, like so:
```fsharp
// you have to create the child actor somewhere inside myFirstActor
let firstActor (mailbox:Actor<_>) =
  let myFirstChildActor = spawn mailbox.Context "myFirstChildActor" (actorOf firstChildActor)

  let rec loop() = actor {
      let! message = mailbox.Receive()
      // handle an incoming message
      return! loop()
  }
  loop()
```

**Further reading about Akka.NET F# API:  
 [F# API Support in Akka.NET](https://github.com/akkadotnet/akka.net/tree/dev/src/core/Akka.FSharp)**


##### 2) Look up the actor
All actors have an address (technically, an `ActorPath`) which represents where they are in the system hierarchy, and you can get a handle to them (get their `IActorRef`) by looking them up by their address.

We will cover this in much more detail in the next lesson.

#### Are there different types of `IActorRef`s?
Actually, yes. The most common, by far, is just a plain-old `IActorRef` or handle to an actor, as above.

However, there are also some other `IActorRef`s available to you within the context of an actor. As we said, all actors have a context. That context holds metadata, which includes information  about the current message being processed. That information includes things like the `Parent` or `Children` of the current actor, as well as the `Sender` of the current message.

`Parent`, `Children`, and `Sender` all provide `IActorRef`s that you can use.

Enough of this conceptual business. Let's get to it!

## Exercise
Before we can get into the meat of this lesson `IActorRef`s, we have to do a bit of cleanup.

### Move validation into its own actor
We're going to move all our validation code into its own actor. It really doesn't belong in the `consoleReaderActor`. Validation deserves to have its own actor.

#### Create `validationActor`
Make a new function called `validationActor` and fill it with all the validation logic that is currently in `consoleReaderActor`:

```fsharp
let validationActor (consoleWriter: IActorRef) (mailbox: Actor<_>) message =
    let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
        match msg.Length, msg.Length % 2 with
        | 0, _ -> EmptyMessage
        | _, 0 -> MessageLengthIsEven
        | _, _ -> MessageLengthIsOdd

    match message with
    | EmptyMessage ->  consoleWriter <! InputError ("No input received.", ErrorType.Null)
    | MessageLengthIsEven -> consoleWriter <! InputSuccess ("Thank you! The message was valid.")
    | _ -> consoleWriter <! InputError ("The message is invalid (odd number of characters)!", ErrorType.Validation)

    mailbox.Sender ()  <! Continue
```

#### Make a new `IActorRef` for `validationActor`
In `Main()`, your code should look like this right now:

```fsharp
let main argv =
    //initialize Acctor System
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())

    //writer actor
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)

    //new actor to validate messages
    let validationActor = spawn myActorSystem "validationActor" (actorOf2 (Actors.validationActor consoleWriterActor))

    //reader actor
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor validationActor))
    consoleReaderActor <! Start

    // blocks the main thread from exiting until the actor system is shut down
    myActorSystem.WhenTerminated.Wait ()
    0
```

#### Calling out a special `IActorRef`: `Sender`
You may not have noticed it, but we actually are using a special `IActorRef` now: `Sender`. Go look for this in `validationActor`:

```fsharp
// tell sender to continue doing its thing (whatever that may be, this actor doesn't care)
mailbox.Sender ()  <! Continue
```

This is the special `Sender` handle that is made available within an actors `Context` when it is processing a message. The `Context` always makes this reference available, along with some other metadata (more on that later).

### Phase 4: A bit of cleanup
Just a bit of cleanup since we've changed our class structure. Then we can run our app again!

#### Update `consoleReaderActor`
Now that `validationActor` is doing our validation work, we should really slim down `consoleReaderActor`. Let's clean it up and have it just hand the message off to the `validationActor` for validation.

We'll also need to store a reference to `validationActor` inside the `consoleReaderActor`, and we don't need a reference to the the `consoleWriterActor` anymore. Modify your version of `consoleReaderActor` to match the below:

```fsharp
let consoleReaderActor (validation: IActorRef) (mailbox: Actor<_>) message =
    let doPrintInstructions () =
        Console.WriteLine "Write whatever you want into the console!"
        Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
        Console.WriteLine "Type 'exit' to quit this application at any time.\n"

    let (|Message|Exit|) (str:string) =
        match str.ToLower() with
        | "exit" -> Exit
        | _ -> Message(str)

    let getAndValidateInput () =
        let line = Console.ReadLine ()
        match line with
        | Exit -> mailbox.Context.System.Shutdown ()
        | _ -> validation <! line

    match box message with
    | :? Command as command ->
        match command with
        | Start -> doPrintInstructions ()
        | _ -> ()
    | _ -> ()
    
    getAndValidateInput ()
```

As you can see, we're now handing off the input from the console to the `validationActor` for validation and decisions. `consoleReaderActor` is now only responsible for reading from the console and handing the data off to another more sophisticated actor.

There. That's better.

### Once you're done
Compare your code to the solution in the [Completed](Completed/) folder to see what the instructors included in their samples.

If everything is working as it should, the output you see should be identical to last time:
![Petabridge Akka.NET Bootcamp Lesson 1.2 Correct Output](Images/working_lesson3.jpg)


## Great job! Onto Lesson 4!
Awesome work! Well done on completing your this lesson. It was a big one.

**Let's move onto [Lesson 4: Child Actors, Actor Hierarchies, and Supervision](../lesson4).**

## Any questions?
**Don't be afraid to ask questions** :).

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.
