# Lesson 1.6: The Actor Lifecycle
This last lesson will wrap up our "fundamentals series" on working with actors, and it ends with a critical concept: actor life cycle.

## Key concepts / background
### What is the actor life cycle?
Actors have a well-defined life cycle. Actors are created and started, and then they spend most of their lives receiving messages. In the event that you no longer need an actor, you can terminate or "stop" an actor.

### What are the stages of the actor life cycle?
There are 5 stages of the actor life cycle in Akka.NET:

1. `Starting`
2. `Receiving`
3. `Stopping`
4. `Terminated`, or
5. `Restarting`

![Akka.NET actor life cycle steps with explicit methods](Images/lifecycle.png)

Let's take them in turn.

#### `Starting`
This is the initial state of the actor, when it is being initialized by the `ActorSystem`.

#### `Receiving`
The actor is now available to process messages. Its `Mailbox` (more on that later) will begin delivering messages into the `OnReceive` method of the actor for processing.

#### `Stopping`
During this phase, the actor is cleaning up its state. What happens during this phase depends on whether the actor is being terminated or restarted.

If the actor is being restarted, it's common to save state or messages during this phase to be processed once the actor is back in its Receiving state after the restart.

If the actor is being terminated, all the messages in its `Mailbox` will be sent to the `DeadLetters` mailbox of the `ActorSystem`. `DeadLetters` is a store of undeliverable messages, usually because an actor is dead.

#### `Terminated`
The actor is dead. Any messages sent to its former `IActorRef` will now go to `DeadLetters` instead. The actor cannot be restarted, but a new actor can be created at its former address (which will have a new `IActorRef` but an identical `ActorPath`).

#### `Restarting`
The actor is about to restart and go back into a `Starting` state.

### Life cycle hook methods
So, how can you link into the actor life cycle? Here are the 4 places you can hook in.

#### `PreStart`
`PreStart` logic gets run before the actor can begin receiving messages and is a good place to put initialization logic. Gets called during restarts too.

#### `PreRestart`
If your actor fails (i.e. throws an unhandled Exception) the actor's parent will restart the actor. `PreRestart` is where you can hook in to do cleanup before the actor restarts, or to save the current message for reprocessing later.

#### `PostStop`
`PostStop` is called once the actor has stopped and is no longer receiving messages. This is a good place to include clean-up logic. PostStop also gets called during `PreRestart`, but you can override `PreRestart` and simply not call `base.PreRestart` if you want to avoid this behavior during restarts.

`DeathWatch` is also when an actor notifies any other actors that have subscribed to be alerted when it terminates. `DeathWatch` is just a pub/sub system built into framework for any actor to be alerted to the termination of any other actor.

#### `PostRestart`
`PostRestart` is called during restarts after PreRestart but before PreStart. This is a good place to do any additional reporting or diagnosis on the error that caused the actor to crash, beyond what Akka.NET already does for you.

Here's where the hook methods fit into the stages of the life cycle:

![Akka.NET actor life cycle steps with explicit methods](Images/lifecycle_methods.png)

### How do I hook into the life cycle?
To hook in, you just override the method you want to hook into, like this:

```csharp
 /// <summary>
/// Initialization logic for actor
/// </summary>
protected override void PreStart()
{
    // do whatever you need to here
}
```

### Which are the most commonly used life cycle methods?
#### `PreStart`
`PreStart` is far and away the most common hook method used. It is used to set up initial state for the actor and run any custom initialization logic your actor needs.

#### `PostStop`
The second most common place to hook into the life cycle is in `PostStop`, to do custom cleanup logic. For example, you may want to make sure your actor releases file system handles or any other resources it is consuming from the system before it terminates.

#### `PreRestart`
`PreRestart` is in a distant third to the above methods, but you will occasionally use it. What you use it for is highly dependent on what the actor does, but one common case is to stash a message or otherwise take steps to get it back for reprocessing once the actor restarts.

### How does this relate to supervision?
In the event that an actor accidentally crashes (i.e. throws an unhandled Exception,) the actor's supervisor will automatically restart the actor's lifecycle from scratch - without losing any of the remaining messages still in the actor's mailbox.

As we covered in lesson 4 on the actor hierarchy/supervision, what occurs in the case of an unhandled error is determined by the `SupervisionDirective` of the parent. That parent can instruct the child to terminate, restart, or ignore the error and pick up where it left off. The default is to restart, so that any bad state is blown away and the actor starts clean. Restarts are cheap.

## Exercise
This final exercise is very short, as our system is already complete. We're just going to use it to optimize the initialization and shutdown of `TailActor`.

### Move initialization logic from `TailActor` constructor to `PreStart()`
See all this in the constructor of `TailActor`?

```csharp
// TailActor.cs constructor
// start watching file for changes
_observer = new FileObserver(Self, Path.GetFullPath(_filePath));
_observer.Start();

// open the file stream with shared read/write permissions
// (so file can be written to while open)
_fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, 
    FileAccess.Read, FileShare.ReadWrite);
_fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

// read the initial contents of the file and send it to console as first message
var text = _fileStreamReader.ReadToEnd();
Self.Tell(new InitialRead(_filePath, text));
```

While it works, initialization logic really belongs in the `PreStart()` method.

Time to use your first life cycle method!

Pull all of the above initialization logic out of the `TailActor` constructor and move it into `PreStart()`. We'll also need to change `_observer`, `_fileStream`, and `_fileStreamReader` to non-readonly fields since they're moving out of the constructor.

The top of `TailActor.cs` should now look like this

```csharp
// TailActor.cs
private string _filePath;
private IActorRef _reporterActor;
private FileObserver _observer;
private Stream _fileStream;
private StreamReader _fileStreamReader;

public TailActor(IActorRef reporterActor, string filePath)
{
    _reporterActor = reporterActor;
    _filePath = filePath;
}

// we moved all the initialization logic from the constructor
// down below to PreStart!

/// <summary>
/// Initialization logic for actor that will tail changes to a file.
/// </summary>
protected override void PreStart()
{
    // start watching file for changes
    _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
    _observer.Start();

    // open the file stream with shared read/write permissions
    // (so file can be written to while open)
    _fileStream = new FileStream(Path.GetFullPath(_filePath),
        FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);

    // read the initial contents of the file and send it to console as first message
    var text = _fileStreamReader.ReadToEnd();
    Self.Tell(new InitialRead(_filePath, text));
}
```

Much better! Okay, what's next?

### Let's clean up and take good care of our `FileSystem` resources
`TailActor` instances are each storing OS handles in `_fileStreamReader` and `FileObserver`. Let's use `PostStop()` to make sure those handles are cleaned up and we are releasing all our resources back to the OS.

Add this to `TailActor`:

```csharp
// TailActor.cs
/// <summary>
/// Cleanup OS handles for <see cref="_fileStreamReader"/> 
/// and <see cref="FileObserver"/>.
/// </summary>
protected override void PostStop()
{
    _observer.Dispose();
    _observer = null;
    _fileStreamReader.Close();
    _fileStreamReader.Dispose();
    base.PostStop();
}
```

### Phase 4: Build and Run!
That's it! Hit `F5` to run the solution and it should work exactly the same as before, albeit a little more optimized. :)

### Once you're done
Compare your code to the solution in the [Completed](Completed/) folder to see what the instructors included in their samples.

## Great job!

**Ready for more? [Start Unit 2 now](../../Unit-2/README.md "Akka.NET Bootcamp Unit 2").**

## Any questions?

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](https://github.com/petabridge/akka-bootcamp/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.
