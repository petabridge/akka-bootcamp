# Lesson 2.4: Switching Actor Behavior at Run-time

In this lesson we're going to learn about one of the really cool things actors can do: [change their behavior at run-time](http://getakka.net/docs/Working%20with%20actors#hotswap "Akka.NET - Actor behavior hotswap")!

## Key Concepts / Background
Let's start with a real-world scenario in which you'd want the ability to change an actor's behavior.

### Real-World Scenario: Authentication
Imagine you're building a simple chat system using Akka.NET actors, and here's what your `UserActor` looks like - this is the actor that is responsible for all communication to and from a specific human user.

```fsharp
let useractor (userId: string) (chatroomId: string) (mailbox: Actor<_>) msg =
    match msg with
    | IncomingMessage(chatroom) when chatroom = chatroomId -> // print message for user
    | OutgoingMessage(chatroom) when chatroom = chatroomId -> // send message to chatroom
    | _ -> ()
```

So we have a basic chat working - yay! But&hellip; right now there's nothing to guarantee that this user is who they say they are. This system needs some authentication.

How could we rewrite this actor to handle these same types of chat messages differently when:

* The user is **authenticating**
* The user is **authenticated**, or
* The user **couldn't authenticate**?

Simple: we can use switchable actor behaviors to do this!

### What is switchable behavior?
One of the core attributes of an actor in the [Actor Model](https://en.wikipedia.org/wiki/Actor_model) is that an actor can change its behavior between messages that it processes.

This capability allows you to do all sorts of cool stuff, like build [Finite State Machines](http://en.wikipedia.org/wiki/Finite-state_machine) or change how your actors handle messages based on other messages they've received.

Switchable behavior is one of the most powerful and fundamental capabilities of any true actor system. It's one of the key features enabling actor reusability, and helping you to do a massive amount of work with a very small code footprint.

### Isn't it problematic for actors to change behaviors?
No, actually it's safe and is a feature that gives your `ActorSystem` a ton of flexibility and code reuse.

Here are some common questions about switchable behavior:

#### When is the new behavior applied?
We can safely switch actor message-processing behavior because [Akka.NET actors only process one message at a time](http://petabridge.com/blog/akkadotnet-async-actors-using-pipeto/). The new message processing behavior won't be applied until the next message arrives.

### Back to the real-world example
Okay, now that you understand switchable behavior, let's return to our real-world scenario and see how it is used. Recall that we need to add authentication to our chat system actor.

So, how could we rewrite this actor to handle chat messages differently when:

* The user is **authenticating**
* The user is **authenticated**, or
* The user **couldn't authenticate**?

Here's one way we can implement switchable message behavior in our `UserActor` to handle basic authentication:

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

Whoa! What's all this stuff? Let's quickly review it.

First, we took split the message handlers defined on our `userActor` into three separate functions. Each represents a state that controls how the actor processes messages:

* `authenticating ()`: this behavior is used to process messages when the user is attempting to authenticate (initial behavior).
* `authenticated ()`: this behavior is used to process messages when the authentication operation is successful; and,
* `unauthenticated ()`: this behavior is used to process messages when the authentication operation fails.

We called `authenticating ()`, so our actor begins in the `authenticating` state.

*This means that only the handlers defined in the `authenticating` function will be used to process messages (initially)*.

However, if we receive a message of type `AuthenticationSuccess` or `AuthenticationFailure`, we will switch behaviors to either `Authenticated` or `Unauthenticated`, respectively.

Now, let's put behavior switching to work for us!

## Exercise
In this lesson we're going to add the ability to pause and resume live updates to the `chartingActor` via switchable actor behaviors.

### Phase 1 - Add a New `Pause / Resume` Button to `Form.fs`
This is the last button you'll have to add, we promise. Add the following code below the other button declarations in `Form.fs`:

```fsharp
let btnPauseResume = new Button(Name = "btnPauseResume", Text = "PAUSE ||", Location = Point(570, 200), Size = Size(110, 40), TabIndex = 4, UseVisualStyleBackColor = true)
```

and add the new control on the form:

```fsharp
// in Form.fs
sysChart.BeginInit ()
form.SuspendLayout ()
sysChart.ChartAreas.Add chartArea1
sysChart.Legends.Add legend1
sysChart.Series.Add series1

form.Controls.Add btnPauseResume // new button for Pause/Resume

form.Controls.Add btnCpu
form.Controls.Add btnMemory
form.Controls.Add btnDisk

form.Controls.Add sysChart
sysChart.EndInit ()
form.ResumeLayout false
```

As before, we also need to add a click handler for the new button in the `load` function of `Form.fs`:

```fsharp
btnPauseResume.Click.Add (fun _ -> ())
```

We'll implement this click handler later in the lesson.

### Phase 2 - Add Switchable Behavior to `chartingActor`

First, we need to add a new `TogglePause` case to `ChartMessage` for `chartingActor`.

```fsharp
type ChartMessage = 
    | InitializeChart of initialSeries: Map<string, Series>
    | AddSeries of series: Series
    | RemoveSeries of seriesName: string
    | Metric of series: string * counterValue: float
    | TogglePause // new case
```

Add a new function called `paused` to the `chartingActor`. The `paused` function needs to handle the following messages:
1. The `TogglePause` message, which requires the handler to switch to the original behavior, and
2. The `Metric` message, which requires the handler to add an empty data point on the chart.

```fsharp
// in Actors.fs, chartingActor
let rec charting(mapping: Map<string, Series>, numberOfPoints: int) =
    actor {
        // implementation of the charting function...
    }
and paused (mapping: Map<string, Series>, numberOfPoints: int) =
    actor {
        let! message = mailbox.Receive ()

        match message with
        | TogglePause ->
            setPauseButton false
            return! charting (mapping, numberOfPoints)

        | Metric (seriesName, counterValue) when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName ->
            let newNoOfPts = numberOfPoints + 1
            let series = mapping.[seriesName]
            series.Points.AddXY (newNoOfPts, 0.) |> ignore
            while (series.Points.Count > maxPoints) do series.Points.RemoveAt 0
            setChartBoundaries (mapping, newNoOfPts)
            return! paused (mapping, newNoOfPts)

        | _ -> ()

        setChartBoundaries (mapping, numberOfPoints)
        return! paused (mapping, numberOfPoints)
    }

charting (Map.empty<string, Series>, 0)
```

The key change here is that we set the value of the new, paused points to `0.` instead of their real value:

```fsharp
// when charting
series.Points.AddXY (numberOfPoints, counterValue) |> ignore
// when paused
series.Points.AddXY (newNoOfPts, 0.) |> ignore
```

Define a new method called `setPauseButtonText` at the top of the `chartingActor` class:

```fsharp
// Actors/chartingActor - add to the top of the chartingActor class
let setPauseButtonText paused = pauseButton.Text <- if not paused then "PAUSE ||" else "RESUME ->"
```

Then handle the `TogglePause` case in the `charting` function by adding the following to the bottom of the `actor` computation expression:

```fsharp
let rec charting (mapping : Map<string, Series>, numberOfPoints : int) =
    actor{
        let! message = mailbox.Receive()

        match message with
          // implementation of other cases...

          | TogglePause ->
            setPauseButtonText true
            return! paused (mapping, numberOfPoints)
    }
```

And finally, let's **update `chartingActor`'s defintion**:

```fsharp
let chartingActor (chart: Chart) (pauseButton: System.Windows.Forms.Button) (mailbox: Actor<_>) =
```

### Phase 3 - Update the `load` function and `Pause / Resume` Click Handler in `Form.fs`
Since we changed the arguments for `chartingActor` in Phase 2, we need to fix this inside our `load` function for `Forms.fs`

```fsharp
let chartActor = spawn myActorSystem "charting" (Actors.chartingActor sysChart btnPauseResume)
```

And finally, we need to update our `btnPauseResume` click event handler to have it tell the `chartActor` to pause or resume live updates:

```fsharp
btnPauseResume.Click.Add (fun _ -> chartActor <! TogglePause)

```

### Once you're done
Build and run `SystemCharting.sln` and you should see the following:

![Successful Lesson 4 Output](images/dothis-successful_2-run4.gif)

Compare your code to the code in the [/Completed/ folder](Completed/) to compare your final output to what the instructors produced.

## Great job!
YEAAAAAAAAAAAAAH! We have a live updating chart that we can pause over time!

Here is a high-level overview of our working system at this point:

![Akka.NET Bootcamp Unit 2 System Overview](images/system_overview_2_4.png)

***But wait a minute!***

What happens if I toggle a chart on or off when the `ChartingActor` is in a paused state?

![Lesson 4 Output Bugs](images/dothis-fail4.gif)

### DOH!!!!!! It doesn't work!

*This is exactly the problem we're going to solve in the next lesson*, using a message `Stash` to defer processing of messages until we're ready.

**Let's move onto [Lesson 5 - Using `Stash` to Defer Processing of Messages](../lesson5).**

## Any questions?
**Don't be afraid to ask questions** :).

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](https://github.com/petabridge/akka-bootcamp/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.
