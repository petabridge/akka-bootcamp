# Lesson 1.2: Defining and Handling Messages
In this lesson, you will make your own message types and learn how to control the processing flow within your actors, based on your custom messages. Doing so will teach you the fundamentals of communicating in a message- and event-driven manner within your actor system.

This lesson picks up where Lesson 1 left off, and continues extending our budding systems of console actors. In addition to defining our own messages, we'll also add some simple validation for the input we enter and take action based on the results of that validation.

## Key concepts / background
### What is a message?
You can leverage the F# type system by using algebraic data types to represent custom messages for your actors.

F# has a very rich and powerful type system. It has built in support for *Tuples*, *Discriminated Unions* and *Record Types*. Each of these structured representations of your model can be used semantically to represent the message.

```fsharp
 // this is a Tuple that could be used as an Actor Message
 let tuple = (42, “some text”, true)
 ```

*Tuples* are useful but have some disadvantages. First of all, the elements in a tuple are not labeled, and this can lead to confusion about which element is in which place. Second, Tuples are predefined and this makes it hard to differentiate between them.

```fsharp
 // this is a Record Type that could be used as an Actor Message
 type Person = { name:string; age:int }
 ```

 A *Record Type* has labeled elements and can be used as well. Record types provides structural equality out-of-the-box.

```fsharp
 // this is a Discriminated Union that could be used as an Actor Message
 type InputResult =
 | InputSuccess of string
 | InputError of reason: string * errorType: ErrorType
 ```

 Discriminated Unions represent well-defined and finite list of choices. In general, a discriminated union is used to create more complicated data structures.


### How do I send an actor a message?
As you saw in the first lesson, you can send (or "tell") messages to the actors defined in your system.
Akka.Fsharp Api provide a convenient and dedicated infix operator `<!` to send messages to a given actor.

### How do I handle a message?
This is entirely up to you, regardless of Akka.NET. You can handle (or ignore) a message any way you want within an actor.

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

The `box` function above boxes your message to an `Object` so the pattern matching can compare to a type using the `:?` operator. In the example above, we try to map the message either against a `string` or an `InputResult`.

### What happens if my actor receives a message it doesn't know how to handle?
Actors ignore messages they don't know how to handle. Whether or not this ignored message is logged as such depends on the type of actor.

Using Akka.Fsharp Api, we can create Actor using an imperative or a functional style as described in Lesson 1.1. In both cases, unhandled messages are not logged as such unless you manually mark them using `Unhandled`, as shown below:

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

In the previous example, we use pattern matching to detect the message type. In F#, pattern matching is exhaustive, meaning that the compiler will warm you if one or more matchings are missing. In the last branch, we use the underscore pattern as a wildcard that collects and matches all the missing cases. The wildcard pattern `\_` is very useful in the Akka.NET Actors ecosystem to be able to handle unhandled messages.

However, in an actor of type `ReceiveActor` (which we cover in Unit 2), unhandled messages are automatically sent to `Unhandled` so the logging is done for you.

### How do my actors respond to messages?
This is up to you - you can respond by simply processing the message, replying to the `Sender`, forwarding the message onto another actor, or doing nothing at all.

> **NOTE:** Whenever your actor receives a message, it will always have the sender of the current message available via the `Sender` property inside your actor.

## Exercise
In this exercise, we will introduce some basic validation into our system. We will then use custom message types to signal the results of that validation back to the user.

### Phase 1: Define your own message types
#### Add a new module called `Messages` in the corresponding file, `Messages.fs`
We will use a discriminated union type to represent and define system-level messages to signal events. The pattern we'll be using is to turn events into messages. That is, when an event occurs, we will send an appropriate message defined as a discriminated union to the actor(s) that need to know about it, and then listen/respond to that message as needed in the receiving actors.

Note: `Messages.fs` must be added above the other F# file in the solution.

#### Create the `InputResult` message
Define a discriminated union type called `InputResult` in `Messages.fs`.
The reason behind the use of a discriminated union is because this message can have two different outcomes.
The first outcome is represented by the `InputSuccess` case, carrying the state of the result as a `string` type. This message type is used to signal that the user input was correct and passed validation.
The second case is `InputError`, carrying the state of the error type and the reason for the failure. This message is used to signal that the user input is invalid.
In order to distinguish between the different possible errors made by the user, `InputError` uses another discriminated union called `ErrorType`.

```fsharp
// in Messages.fs
// The user didn't provide any input, or the input was not valid.
type ErrorType =
| Null
| Validation

// Discriminated union to determine whether or not the user input was valid.
type InputResult =
| InputSuccess of string
| InputError of reason: string * errorType: ErrorType
```
> **NOTE:** You can compare your final `Messages.fs` to [Messages.fs](Completed/Messages.fs) to make sure you're set up before we go on with the rest of the exercise.

### Phase 2: Turn events into messages and send them
Great! Now that we've got messages types set up to wrap our events, let's use them in `consoleReaderActor` and `consoleWriterActor`.

#### Update `Actors.fs`
Replace the pattern matching logic used to handle messages in `consoleReaderActor` as follows. Notice that we're now listening for our custom `InputError` messages, and taking action when we get an error.

```fsharp
// in Actors.fs
// consoleReaderActor
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
 ```

While we're at it, let's add a couple of helper functions to `Actors.fs`:
- `EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd` Active Pattern
- `doPrintInstructions ()`
- `getAndValidateInput ()` (inside `consoleReaderActor`)

For the implementation of `getAndValidateInput ()` we use pattern matching to determine the input received from the console.
These are internal functions that our `consoleReaderActor` will use to get input from the console and determine if it is valid. (Currently, "valid" just means that the input had an even number of characters. It's an arbitrary placeholder.)

```fsharp
// At the top of Actors.fs, before consoleReaderActor
// Active pattern matching to determine the charateristics of the message (empty, even length, or odd length)
let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
    match msg.Length, msg.Length % 2 with
    | 0, _ -> EmptyMessage
    | _, 0 -> MessageLengthIsEven
    | _, _ -> MessageLengthIsOdd

// At the top of Actors.fs, before consoleReaderActor
// Print instructions to the console
let doPrintInstructions () =
    Console.WriteLine "Write whatever you want into the console!"
    Console.WriteLine "Some entries will pass validation, and some won't...\n\n"
    Console.WriteLine "Type 'exit' to quit this application at any time.\n"

// Inside consoleReaderActor
// Read and validate the input from the console, then signal the appropriate action (success and continue processing, or error)
let getAndValidateInput () =
    let line = Console.ReadLine()
    match line with
    | Exit -> mailbox.Context.System.Terminate () |> ignore
    | Message(input) ->
        match input with
        | EmptyMessage ->
            mailbox.Self <! InputError ("No input received.", ErrorType.Null)
        | MessageLengthIsEven ->
            consoleWriter <! InputSuccess ("Thank you! The message was valid.")
            mailbox.Self  <! Continue
        | _ ->
            mailbox.Self <! InputError ("The message is invalid (odd number of characters)!", ErrorType.Validation)
```

#### Update `Program.fs`
Remove the definition and call to `printInstructions ()` from `Program.fs`. As a quick checkpoint, your `main` should now look like this:
```fsharp
let main argv =
    let myActorSystem = System.create "MyActorSystem" (Configuration.load ())
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor = spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))

    consoleReaderActor <! Start
    myActorSystem.WhenTerminated.Wait ()
    0
```

We don't need `printInstructions ()` here anymore, as we've just added `doPrintInstructions ()` in `Actors.fs` in the previous step.

#### Update `consoleWriterActor` in `Actors.fs`
Now, let's get `consoleWriterActor` to handle these new types of messages.

We are adding a helper function `printInColor` that will output messages in the console using a different `ForegroundColor`.
Change the pattern matching inside the mailbox of `consoleWriterActor` to the following:

```fsharp
// in Actors.fs
// consoleWriterActor
let printInColor color message =
    Console.ForegroundColor <- color
    Console.WriteLine (message.ToString ())
    Console.ResetColor ()

match box message with
| :? InputResult as inputResult ->
    match inputResult with
    | InputError (reason, _) -> printInColor ConsoleColor.Red reason
    | InputSuccess reason -> printInColor ConsoleColor.Green reason
| _ -> printInColor ConsoleColor.Yellow (message.ToString ())
```

As you can see here, we are making `consoleWriterActor` pattern match against the type of message it receives, and take different actions accordingly.

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
