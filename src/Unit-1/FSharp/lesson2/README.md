# Lesson 1.2: Defining and Handling Messages
In this lesson, you will make your own message types and learn how to control processing flow within your actors, based on your custom messages. Doing so will teach you the fundamentals of communicating in a message- and event-driven manner within your actor system.

This lesson picks where Lesson 1 left off, and continues extending our budding systems of console actors. In addition to defining our own messages, we'll also add some simple validation for the input we enter and take action based on the results of that validation.

## Key concepts / background
### What is a message?
You can make your own custom messages and leverage the F# type system to represent messages adopting algebraic data types.

F# has a very rich and powerful type system. It has built in support for Tuple, Discriminated Union and Record-Type. Each of these structured representations of your model can be used semantically to represent the message.

```fsharp
 // this is a tuple that could be used as an Actor Message
 let tuple = (42, “some text”, true)
 ```

Tuples are useful but have some disadvantages. First of all, the pairs in a tuple are not labeled, and this can lead to confusion about which element is in which place. Second, Tuples are predefined and this makes it hard to differentiate between them.

```fsharp
 // this is a Record-Type that could be used as Actor Message
 type Person = { name:string; age:int }
 ```

 A Record-Type has labeled elements and can be used as well. Record-types provides out of the box structural equality.

```fsharp
 // this is a Discrimination Union that could be used as Actor Message
 type InputResult =
 | InputSuccess of string
 | InputError of reason: string * errorType: ErrorType
 ```

 Discriminated Unions represent well-defined and finite list of choices. In general, a discriminated union is used to create more complicated data structures.


### How do I send an actor a message?
As you saw in the first lesson, you can send the actor (known as "tell") the message.
Akka.Fsharp Api provide a convenient and dedicated infix operator ` <! ` to send message.

### How do I handle a message?
This is entirely up to you, regardless of Akka.NET. you can handle (or not handle) a message as you choose within an actor.

You can use pattern matching to verify and compare the message type with a logical structure. Pattern matching is also able to deconstruct the type received as message and extract the encapsulated state.
```fsharp
 // this is an example of pattern matching
 match box message with
 | :? string as command ->
     match command with
     | StartCommand -> doPrintInstructions ()
     | _ -> ()
 | :? InputResult as inputResult ->
     match inputResult with
     | InputError(_,_) as error -> consoleWriter <! error
     | _ -> ()
 | _ -> ()
```

### What happens if my actor receives a message it doesn't know how to handle?
Actors ignore messages they don't know how to handle. Whether or not this ignored message is logged as such depends on the type of actor.

Using Akka.Fsharp Api, we can create Actor using an imperative or a functional style as described in Lesson 1.1. 	Either implementation of an Actor, unhandled messages are not logged as unhandled unless you manually mark them as such, for example:

```fsharp
// Functional implementation of an Actor
let myActor (mailbox:Actor<_>) message =
    match message with
    | Message.InputError as msg ->
            Console.ForegroundColor <- ConsoleColor.Red
            Console.WriteLine msg.Reason
    | _ -> mailbox.Unhandled message

// Imperative implementation of an Actor using F#
type MyActor() =
    inherit UntypedActor()

    override x.OnReceive message =
        match message with
        | Message.InputError as msg ->
                Console.ForegroundColor <- ConsoleColor.Red
                Console.WriteLine msg.Reason
        | _ -> x.Unhandled message
```

In the previous example, we have used pattern matching to detect the message type. In F# pattern matching are exhaustive matching, and the compiler will warm you if one or more matching are missing. The last of the branch matching we used the underscore pattern, this pattern is a wildcard that collect and match all the missing matching. The wildcard (\_) pattern is very useful in the Akka.NET Actors ecosystem to be able to handle the unhandled messages.

However, in a `ReceiveActor`—which we cover in Unit 2—unhandled messages are automatically sent to `Unhandled` so the logging is done for you.

### How do my actors respond to messages?
This is up to you - you can respond by simply processing the message, replying to the `Sender`, forwarding the message onto another actor, or doing nothing at all.

> **NOTE:** Whenever your actor receives a message, it will always have the sender of the current message available via the `Sender` property inside your actor.

## Exercise
In this exercise, we will introduce some basic validation into our system. We will then use custom message types to signal the results of that validation back to the user.

### Phase 1: Define your own message types
#### Add a new type called `Messages` and the corresponding file, `Messages.fs`
We will use the discriminate union type to represent and define system-level messages to signal events. The pattern we'll be using is to turn events into messages. That is, when an event occurs, we will send an appropriate message defined as discriminated union to the actor(s) that need to know about it, and then listen/respond to that message as needed in the receiving actors.

#### Make `InputResult` message
Define a discrimination union type `InputResult ` in the file `Messages.fs`.
The reason behind the use of a discrimination union is because this message can have two different outcome and types.
The discrimination union has two cases. The first is the `InputResult` carrying the state of the result as string type. This message type is used to signal that the user's input was good and passed validation.
The second case is `InputError` carrying the state of the error type and the reason for the failure, This message is used to signal invalid input occurring.
To describe and distinguish between different error cases signaled by the `InputError` message utilize discrimination union `ErrorType`.

```fsharp
// in Messages.fs
// User provided blank input.
type ErrorType =
| Null
| Validation

// Discrimination union for signaling that user input was valid or invalid.
type InputResult =
| InputSuccess of string
| InputError of reason: string * errorType: ErrorType
```
> **NOTE:** You can compare your final `Messages.fs` to [Messages.fs](Completed/Messages.fs) to make sure you're set up right before we go on.

### Phase 2: Turn events into messages and send them
Great! Now that we've got messages types set up to wrap our events, let's use them in `consoleReaderActor` and `consoleWriterActor`.

#### Update ` Actors `
Replace the pattern matching logic used to handle messages in `consoleReaderActor` as follows. Notice that we're now listening for our custom `InputError` messages, and taking action when we get an error.

```fsharp
// in consoleReaderActor
  match box message with
  | :? Command as command ->
      match command with
      | Start -> doPrintInstructions ()
      | _ -> ()
  | :? InputResult as inputResult ->
      match inputResult with
      | InputError(_,_) as error -> consoleWriter <! error
      | _ -> ()
  | _ -> ()
  getAndValidateInput ()
 ```

While we're at it, let's add few new functions `doPrintInstructions()`, `getAndValidateInput()` and `EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd` Active Pattern to `consoleReaderActor`.
For the implementation of `getAndValidateInput()` we are using pattern matching to determinate the input receive.
These are internal functions that our `consoleReaderActor` will use to get input from the console and determine if it is valid. (Currently, "valid" just means that the input had an even number of characters. It's an arbitrary placeholder.)

```fsharp
// in top of Actors.fs before consoleReaderActor
let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
    match msg.Length, msg.Length % 2 with
    | 0,_ -> EmptyMessage
    | _,0 -> MessageLengthIsEven
    | _,_ -> MessageLengthIsOdd

// in consoleReaderActor,
let doPrintInstructions () =
    Console.WriteLine "Write whatever you want into the console!"
    Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
    Console.WriteLine "Type 'exit' to quit this application at any time.\n"

// Reads input from console, validates it, then signals appropriate response
// (continue processing, error, success, etc.).
let getAndValidateInput () =
    let line = Console.ReadLine()
    match line with
    | Exit -> mailbox.Context.System.Shutdown ()
    | Message(input) ->
        match input with
        | EmptyMessage ->
            mailbox.Self <! InputError ("No input received.", ErrorType.Null)
        | MessageLengthIsEven ->
            consoleWriter <! InputSuccess ("Thank you! Message was valid.")
            mailbox.Self  <! Continue
        | _ ->
            mailbox.Self <! InputError ("Invalid: input had odd number of characters.", ErrorType.Validation)
```

#### Update `Program`
Remove the definition and call to `printInstructions()` from `Program.fs`. As a quick checkpoint, your `main` should now look like this:
```fsharp
let main argv =
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))
    consoleReaderActor <! Start
    myActorSystem.AwaitTermination ()
    0
```

Not much has changed here, just a bit of cleanup.

#### Update `consoleWriterActor`
Now, let's get `consoleWriterActor` to handle these new types of messages.

We are adding a helper function `printInColor` that will output messages in the console using different ForegroundColor.
Change the pattern matching inside the mailbox of `consoleWriterActor` as follows:

```fsharp
// in consoleWriterActor.fs
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
```

As you can see here, we are making `consoleWriterActor` pattern match against the type of message it receives, and take different actions according to what type of message it receives.

### Phase 3: Build and run!
You should now have everything you need in place to be able to build and run. Give it a try!

If everything is working as it should, you will see an output like this:
![Petabridge Akka.NET Bootcamp Lesson 1.2 Correct Output](Images/working_lesson2.jpg)

### Once you're done
Compare your code to the solution in the [Completed](Completed/) folder to see what the instructors included in their samples.

##  Great job! Onto Lesson 3!
Awesome work! Well done on completing this lesson.

**Let's move onto [Lesson 3: Using `IActorRef`s](../lesson3).**


## Any questions?
**Don't be afraid to ask questions** :).

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.
