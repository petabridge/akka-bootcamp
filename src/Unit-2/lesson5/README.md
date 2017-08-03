# Lesson 2.5: Using `Stash` to Defer Processing of Messages

At the end of [Lesson 4](../lesson4/) we discovered a significant bug in how we implemented the **Pause / Resume** functionality on live charts, as you can see below:

![Lesson 4 Output Bugs](../lesson4/images/dothis-fail4.gif)
> NOTE: If you're following along using the eBook / .ePub, you won't see the animation. [Click here to see it](https://github.com/petabridge/akka-bootcamp/raw/FSharp/src/Unit-2/lesson4/images/dothis-fail4.gif).

The bug is that when our `ChartingActor` changes its behavior to `Paused`, it no longer processes the `AddSeries` and `RemoveSeries` messages generated whenever a toggle button is pressed for a particular performance counter.

In its current form, it doesn't take much for the visual state of the buttons to get completely out of sync with the live chart. All you have to do is press a toggle button when the graph is paused and it's immediately out of sync.

So, how can we fix this?

The answer is to defer processing of `AddSeries` and `RemoveSeries` messages until the `ChartingActor` is back in its `Charting` behavior, at which time it can actually do something with those messages.

The mechanism for this is the [`Stash`](http://getakka.net/docs/Stash).

## Key Concepts / Background
One of the side effects of switchable behaviors for actors is that some behaviors may not be able to process specific types of messages. For instance, let's consider the authentication example we used for behavior-switching in [Lesson 4](../lesson4/).

### What is the `Stash`?
The `Stash` is a stack-like data structure implemented in your actor to defer messages for later processing.

#### Stashing Messages
Inside your actor's Message handler, you can call `mailbox.Stash ()` to put the current message at the top of the `Stash`.

You only need to stash messages that you don't want to process now - in the below visualization, our actor happily processes Message 1 but stashes messages 0 and 2.

Note: calling `Stash ()` automatically stashes the current message, so you don't need to pass the message to the `mailbox.Stash ()` call.

This is what that the full sequence of stashing a message looks like:

![Stashing Messages with Akka.NET Actors](images/actors-stashing-messages.gif)

Great! Now that we know how to `Stash` a message for later processing, how do we get messages back out of the `Stash`?

#### Unstashing a Single Message
We can call `mailbox.Unstash ()` to pop off the message at the top of the `Stash`.

**When you call `mailbox.Unstash ()`, the `Stash` will place this message *at the front of the actor's mailbox, ahead of other queued user messages*.**

##### The VIP line
Inside an actor's mailbox, we can think as if there were two separate queues of `user` messages to be processed: the normal message queue, and then the VIP line.

This VIP line is reserved for messages coming from the `Stash`, and any messages in the VIP line will jump ahead of messages in the normal queue and get processed by the actor first. (On that note, there's also a "super VIP" line for `system` message, which cuts ahead of all `user` messages. But that's out of the scope of this lesson.)

This is what the sequence of unstashing a message looks like:

![Unstashing a single message with Akka.NET actors](images/actor-unstashing-single-message.gif)

#### Unstashing the Entire Stash at Once
If we need to unstash *everything* in our actor's `Stash` all at once, we can use the `mailbox.UnstashAll ()` method to push the entire contents of the `Stash` into the front of the mailbox.

Here's what calling `mailbox.UnstashAll ()` looks like:
![Unstashing all stashed messages at once with Akka.NET actors](images/actor-unstashing-all-messages.gif)

### Do messages stay in their original order when they come out of the `Stash`?
It depends on how you take them out of the `Stash`.

#### `mailbox.UnstashAll ()` preserves FIFO message order
When you make a call to `mailbox.UnstashAll ()`, the `Stash` will ensure that the original FIFO order of the messages in the `Stash` is preserved when they're appended to the front of your actor's mailbox (As shown in the `mailbox.UnstashAll ()` animation).

#### `mailbox.Unstash ()` can change the message order
If you call `mailbox.Unstash ()` repeatedly, you can change the original FIFO order of the messages.

Remember that VIP line inside the mailbox, where the `Stash` puts messages when they are unstashed?

Well, when you `Unstash ()` a ***single*** message, it goes to the back of that VIP line. It's still ahead of normal `user` messages, but it is behind any other messages that were previously unstashed.

There is a lot more that goes into *why* this can happen, but it's well beyond the scope of this lesson.

### Does a `Stash`-ed message lose any data?
Absolutely not. When you `Stash` a message, you're technically stashing the message AND the message `Envelope`, which contains all the metadata for the message (its `Sender`, etc).

### What Happens to the Messages in an Actor's `Stash` During Restarts?
An excellent question! The `Stash` is part of your actor's ephemeral state. In the case of a restart, the stash will be destroyed and garbage collected. This is the opposite of the actor's mailbox, which persists its messages across restarts.

**However, you can preserve the contents of your `Stash` during restarts by calling `mailbox.UnstashAll ()` inside your actor's `PreRestart` lifecycle method**. This will move all the stashed messages into the actor mailbox, which persists through the restart:

```fsharp
let preRestart = Some(fun (basefn: exn * obj -> unit) -> mailbox.UnstashAll () |> ignore)
let mySampleActor = spawnOvrd system "actor" (actorOf sampleActor) <| { defOvrd with PreRestart = preRestart }
```

### Real-World Scenario: Authentication with Buffering of Messages
Now that you know what the `Stash` is and how it works, let's revisit the `userActor` from our chat room example and solve the problem with throwing away messages before the user was `authenticated`.

This is the `userActor` we designed in the Concepts area of [Lesson 4](../lesson4/), with behavior switching for different states of authentication:

```fsharp
let userActor (userId: string) (chatroomId: string) (mailbox: Actor<_>) =
    // start the authentication process for this user
    mailbox.Context.ActorSelection "/user/authenticator/" <! userId

    let rec authenticating () =
        actor {
            let! message = mailbox.Receive ()

            match message with
            | AuthenticationSuccess -> return! authenticated () // switch behavior to Authenticated
            | AuthenticationFailure -> return! unauthenticated ()  // switch behavior to Unauthenticated
            | IncomingMessage (roomId, msg) when roomId = chatroomId  -> () // can't accept the message yet - not auth'd
            | OutgoingMessage (roomId, msg) when roomId = chatroomId  -> () // can't send the message yet - not auth'd
            return! authenticating ()
        }
    and unauthenticated () =
        actor {
            let! message = mailbox.Receive ()

            match message with
            | RetryAuthentication -> return! authenticating () // swith behavior to Authenticating
            | IncomingMessage (roomId, msg) when roomId = chatroomId  -> () // can't accept the message yet - not auth'd
            | OutgoingMessage (roomId, msg) when roomId = chatroomId  -> () // can't send the message yet - not auth'd
            return! authenticating ()
        }
    and authenticated () =
        actor {
            let! message = mailbox.Receive ()

            match message with
            | IncomingMessage (roomId, msg) when roomId = chatroomId  -> () // print message for user
            | OutgoingMessage (roomId, msg) when roomId = chatroomId  -> () // send message to chatroom
            return! authenticated ()
        }

    authenticating ()
```

When we first saw that chat room `userActor` example in [Lesson 4](../lesson4/), we were focused on switching behaviors to enable authentication in the first place. But we ignored a major problem with the `userActor`: during the `authenticating` phase, we simply throw away any attempted `OutgoingMessage` and `IncomingMessage` instances.

We're losing messages to/from the user for no good reason, because we didn't know how to delay message processing. **Yuck!** Let's fix it.

The right way to deal these messages is to temporarily store them until the `userActor` enters either the `authenticated` or `unauthenticated` state. At that time, the `userActor` will be able to make an intelligent decision about what to do with messages to/from the user.

This is what it looks like once we update the `Authenticating` behavior of our `UserActor` to delay processing messages until it knows whether or not the user is authenticated:

```fsharp
let userActor (userId: string) (chatroomId: string) (mailbox: Actor<_>) =
    ...

    let rec authenticating () =
        actor {
            let! message = mailbox.Receive ()

            match message with
            | AuthenticationSuccess ->
                mailbox.UnstashAll ()
                return! authenticated () // switch behavior to Authenticated
            | AuthenticationFailure ->
                mailbox.UnstashAll ()
                return! unauthenticated ()  // switch behavior to Unauthenticated
            | IncomingMessage (roomId, msg) when roomId = chatroomId  ->
                mailbox.Stash ()
                () // can't accept the message yet - not auth'd
            | OutgoingMessage (roomId, msg) when roomId = chatroomId  ->
                mailbox.Stash ()
                () // can't send the message yet - not auth'd
            return! authenticating ()
        }
    and unauthenticated () = ...
    and authenticated () = ...
```

Now any message the `userActor` receives while it iss `authenticating` will be available for processing when it switches behavior to `authenticated` or `unauthenticated`.

Excellent! Now that you understand the `Stash`, let's put it to work to fix our system graphs.

## Exercise
In this section, we're going to fix the **Pause / Resume** bug inside the `ChartingActor` that we noticed at the end of [Lesson 4](../lesson4/).

### Add `Stash` Method Calls to Message Handlers Inside `paused` Behavior
Go to the `paused` method declared inside `chartingActor`.

Update it to `Stash ()` the `AddSeries` and `RemoveSeries` messages:

```fsharp
let chartingActor (chart: Chart) (pauseButton: System.Windows.Forms.Button) (mailbox: Actor<_>) =

    ...

    let rec charting (mapping: Map<string, Series>, numberOfPoints: int) = 
        actor {
            ...
        }
    and paused (mapping: Map<string, Series>, numberOfPoints: int) =
        actor {
            let! message = mailbox.Receive ()

            match message with
            | TogglePause ->
                setPauseButton false
                // ChartingActor is leaving the Paused state, put messages back into mailbox for processing under new behavior
                mailbox.UnstashAll ()
                return! charting (mapping, numberOfPoints)

            | AddSeries series -> mailbox.Stash () // stash messages while paused
            | RemoveSeries series -> mailbox.Stash () // stash messages while paused

            ...

            setChartBoundaries (mapping, numberOfPoints)
            return! paused (mapping, numberOfPoints)
        }

    charting (Map.empty<string, Series>, 0)
```

That's it! The `chartingActor` will now save any `AddSeries` or `RemoveSeries` messages and will replay them in the order they were received as soon as it switches from the `paused` state to the `charting` state.

The bug should now be fixed!

### Once you're done
Build and run `SystemCharting.sln` and you should see the following:

![Successful Unit 2 Output](images/syncharting-complete-output.gif)

Compare your code to the code in the [/Completed/ folder](Completed/) to compare your final output to what the instructors produced.

## Great job!

### Wohoo! You did it! Unit 2 is complete! Now go enjoy a well-deserved break, and gear up for Unit 3!

**Ready for more? [Start Unit 3 now](../../Unit-3 "Akka.NET Bootcamp Unit 3").**

## Any questions?
**Don't be afraid to ask questions** :).

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](https://github.com/petabridge/akka-bootcamp/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.