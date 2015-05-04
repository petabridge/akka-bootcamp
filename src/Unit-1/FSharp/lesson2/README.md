# Lesson 1.2: Defining and Handling Messages
In this lesson, you will make your own message types and learn how to control processing flow within your actors, based on your custom messages. Doing so will teach you the fundamentals of communicating in a message- and event-driven manner within your actor system.

This lesson picks where Lesson 1 left off, and continues extending our budding systems of console actors. In addition to defining our own messages, we'll also add some simple validation for the input we enter and take action based on the results of that validation.

## Key concepts / background
### What is a message?
Any POCO can be a message. A message can be a `string`, a value like `int`, a type, an object that implements an interface... whatever you want.

That being said, the recommended approach is to make your own custom messages and leverage the F# type system to represent messages adopting algebraic data types.

F# has a very rich and powerful type system. As mentioned in lesson 1.1, in F# can use algebraic data types to represent and compose types, and can be used to follow the same pattern leveraging F# type system for describing and representing an Actor message.
F# has built in support for Tuple, Discrimination Union and Record-Type. Each of these structured representations of your model can be used semantically to represent the message. A Tuple group set values, which could be of different types, which are ordered but unmanned. 

`// this is a tuple that could be used as Actor Message
 let tuple = (42, “some text”, true)`

Tuples are useful but a have some disadvantages. First of all, the pairs in a tuple are not labeled, and this can lead to confusion about which element is in which place. Second, Tuples are predefined and this makes it hard to differentiate between them.  
A Record-Type is a Tuple with the elements labeled.

`// this is a Record-Type that could be used as Actor Message
 type Person = { name:string; age:int }`

Record-type overcomes the limitation of the Tuples, and it provides out of the box structural equality.
Discriminated Union is a type that represents well-defined and finite list of choices. In general, a discriminated union is used to create more complicated data structures.

`// this is a Discrimination Union that could be used as Actor Message
 type InputResult =
 | InputSuccess of string
 | InputError of reason: string * errorType: ErrorType` 

### How do I send an actor a message?
As you saw in the first lesson, you `Tell()`the actor the message.
Akka.Fsharp Api provide a convenient and dedicated infix operator ` <! ` to send message.

### How do I handle a message?
This is entirely up to you, regardless of Akka.NET. you can handle (or not handle) a message as you choose within an actor.

In F# we can unleash the power of the type system using types such as Discrimination Union, Record-Type and Tuple for describing Actor messages in combination of pattern matching.

In F# and Akka.Net, we can use pattern matching to verify and compare the message type with a logical structure. Pattern matching is also able to deconstruct the type received as message and extract the encapsulated state.
` // this is an example of pattern matching 
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
    | _ -> mailbox.Unhandled msg

// Imperative implementation of an Actor using F#
type MyActor() =
    inherit UntypedActor()

    override x.OnReceive message =
        match message with
        | Message.InputError as msg -> 
                Console.ForegroundColor <- ConsoleColor.Red
                Console.WriteLine msg.Reason
        | _ -> x.Unhandled msg
``
In the previous example, we have used pattern matching to detect the message type. In F# pattern matching are exhaustive matching, and the compiler will warm you if one or more matching are missing. The last of the branch matching we used the underscore pattern, this pattern is a wildcard that collect and match all the missing matching. The wildcard (_) pattern is very useful in the Akka.NET Actors ecosystem to be able to handle the unhandled messages.

However, in a `ReceiveActor`—which we cover in Unit 2—unhandled messages are automatically sent to `Unhandled` so the logging is done for you.

### How do my actors respond to messages?
This is up to you - you can respond by simply processing the message, replying to the `Sender`, forwarding the message onto another actor, or doing nothing at all.

> **NOTE:** Whenever your actor receives a message, it will always have the sender of the current message available via the `Sender` property inside your actor.

## Exercise
In this exercise, we will introduce some basic validation into our system. We will then use custom message types to signal the results of that validation back to the user.

### Phase 1: Define your own message types
#### Add a new type called `Messages` and the corresponding file, `Messages.fs`.
As I previously mentioned, F# has a superb type system and we will use the discriminate union type to represent and define system-level messages that we can use to signal events. The pattern we'll be using is to turn events into messages. That is, when an event occurs, we will send an appropriate message defined as discriminated union to the actor(s) that need to know about it, and then listen for / respond to that message as needed in the receiving actors.

#### Make `ContinueProcessing` message type
Define a discrimination union type ` ProcessCommand` in the file Messages.fs` that we'll use to signal to continue processing (the "blank input" case):

```fsharp
// in Messages.fs
// Marker discrimination union type to continue processing.
type ProcessCommand = 
| ContinueProcessing
```

#### Make `InputResult` message
Define a discrimination union type `InputResult ` in the file Messages.fs`. 
The reason behind the use of a discrimination union is because this message can have two different outcome and types. The power of using a discrimination union as message is based on the fact the we can send different message types that have in common the same base one, and we can use pattern matching for the detection and deconstruction of the message that the Actor received. 
The discrimination union has two types.  The first is the `InputResult` which is carrying the state of the result as string type. This message type is used to signal that the user's input was good and passed validation.
The second type message is `InputError` that is carrying the state of the error type and the reason for the failure, This message is used to signal invalid input occurring. 
To describe and distinguish between different error types signaled by the `InputError` message utilize discrimination union `ErrorType`.

```fsharp
// in Messages.fs
// User provided blank input.
type ErrorType =
| Null
| Validation

// Discrimination union for signalling that user input was valid or invalid.
type InputResult =
| InputSuccess of string
| InputError of reason: string * errorType: ErrorType
```
> **NOTE:** You can compare your final `Messages.fs` to [Messages.fs](Completed/Messages.fs/) to make sure you're set up right before we go on.

### Phase 2: Turn events into messages and send them
Great! Now that we've got messages types set up to wrap our events, let's use them in `consoleReaderActor` and `consoleWriterActor`.

#### Update ` Actors `
Add the following literal type as internal message to `consoleReaderActor`:
```fsharp
// in Actors
[<Literal>]
let StartCommand = "start"
 ```

Update the `Main` method to use `Actors.StartCommand`:

Replace this:

```fsharp
// in Program.fs
// tell console reader to begin
consoleReaderActor <! "start"
```

with this:

```fsharp
// in Program.fs
// tell console reader to begin
consoleReaderActor <! Actors.StartCommand
```

Replace the pattern matching logic used to handle messages in `consoleReaderActor` as follows. Notice that we're now listening for our custom `InputError` messages, and taking action when we get an error.

```fsharp
// in consoleReaderActor
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
    getAndValidateInput ()
 ```

While we're at it, let's add few new functions `doPrintInstructions()`, `getAndValidateInput()` and ‘ValidateMessage` Active Pattern to `consoleReaderActor`. 
For the implementation of `getAndValidateInput()` we are using pattern matching to determinate the input receive in combination of two string Literal ‘ExitCommand` and `EmptyCommand`.
These are internal functions that our `consoleReaderActor` will use to get input from the console and determine if it is valid. (Currently, "valid" just means that the input had an even number of characters. It's an arbitrary placeholder.)

```fsharp
// in top of Actors.fs before consoleReaderActor
[<Literal>]
let ExitCommand = "exit"
[<Literal>]
let EmptyCommand = ""

// in consoleReaderActor,
let doPrintInstructions () =
    Console.WriteLine "Write whatever you want into the console!"
    Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
    Console.WriteLine "Type 'exit' to quit this application at any time.\n"

// Reads input from console, validates it, then signals appropriate response
// (continue processing, error, success, etc.).
let getAndValidateInput () = 
    let line = Console.ReadLine()
    match line.ToLower () with
    | EmptyCommand -> mailbox.Self <! InputError ("No input received.", ErrorType.Null)
    | ExitCommand -> mailbox.Context.System.Shutdown ()
    | ValidMessage _ -> 
         consoleWriter <! InputSuccess ("Thank you! Message was valid.")
         mailbox.Self <! ContinueProcessing
    | _ -> mailbox.Self <! InputError ("Invalid: input had odd number of characters.", ErrorType.Validation)

```

#### Update `Program`
First, remove the definition and call to `printInstructions()` from `Program.fs`.

Now that `consoleReaderActor` has its own well-defined `StartCommand`, let's go ahead and use that instead of hardcoding the string "start" into the message.

As a quick checkpoint, your `main` should now look like this:
```fsharp
let main argv = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))
    consoleReaderActor <! Actors.StartCommand
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
 | _ -> printInColor ConsoleColor.Black (message.ToString ())```

As you can see here, we are making `consoleWriterActor` pattern match against the type of message it receives, and take different actions according to what type of message it receives.

### Phase 3: Build and run!
You should now have everything you need in place to be able to build and run. Give it a try!

If everything is working as it should, you will see an output like this:
![Petabridge Akka.NET Bootcamp Lesson 1.2 Correct Output](Images/working_lesson2.jpg)

### Once you're done
Compare your code to the solution in the [Completed](Completed/) folder to see what the instructors included in their samples.

##  Great job! Onto Lesson 3!
Awesome work! Well done on completing this lesson.

**Let's move onto [Lesson 3 - `Props` and `ActorRef`s](../lesson3).**

