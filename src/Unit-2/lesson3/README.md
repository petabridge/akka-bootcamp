# Lesson 2.3: Using the `Scheduler` to Send Messages Later
Welcome to Lesson 2.3!

Where are we? At this point, we have our basic chart set up, along with our `chartingActor`  which is supposed to be graphing system metrics. Except right now, `chartingActor` isn't actually graphing anything! It's time to change that.

In this lesson, we'll be hooking up the various components of our system to make our Resource Monitor application actually chart system resource consumption! **This is a big lesson—it's the core of Unit 2—so get your coffee and get comfortable!**

To make our resource monitoring app work as intended, we need to wire up `chartingActor` to the actual system [Performance Counters](https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.aspx?cs-save-lang=1&cs-lang=fsharp "PerformanceCounter Class - F#") for the graph data. This needs to happen on an ongoing basis so that our chart regularly updates.

One of the most powerful capabilities Akka.NET exposes is the ability to schedule messages to be sent in the future, including regularly occurring messages. And it turns out, this is exactly the functionality we need to have `chartingActor` regularly update our graphs.

In this lesson you'll learn two powerful Akka.NET concepts:

1. How to use the `Scheduler`, and
2. How to implement the [Publish-subscribe (pub-sub) pattern](http://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) using actors. This is a powerful technique for creating reactive systems.

## Key Concepts / Background
How do you get an actor to do something in the future? And what if you want that actor to do something on a recurring basis in the future?

Perhaps you want an actor to periodically fetch information, or to occasionally ping another actor within the system for its status.

Akka.NET provides a mechanism for doing just this sort of thing. Meet your new best friend: the `Scheduler`.

### What is the `Scheduler`?
The `ActorSystem.Scheduler` ([docs](http://api.getakka.net/docs/stable/html/FB15E2E6.htm "Akka.NET Stable API Docs - IScheduler interface")) is a singleton within every `ActorSystem` that allows you to schedule messages to be sent to an actor in the future. The `Scheduler` can send both one-off and recurring messages.

### How do I use the `Scheduler`?
As we mentioned, you can schedule one-off or recurring messages to an actor.

You can also schedule an `Action` to occur in the future, instead of sending a message to an actor.

#### Access the `Scheduler` via the `ActorSystem`
`Scheduler` must be accessed through the `ActorSystem`, like so:

```fsharp
// inside Program.fs we have direct handle to the ActorSystem
let myActorSystem = System.create "myActorSystem" (Configuration.load ())
myActorSystem.Scheduler.ScheduleTellOnce (TimeSpan.FromMinutes 30., someActor, someMessage)

// but inside an actor, we access the ActorSystem via the ActorContext
mailbox.Context.System.Scheduler.ScheduleTellOnce (TimeSpan.FromMinutes 30., someActor, someMessage)
```

#### Schedule one-off messages with `ScheduleTellOnce()`
Let's say we want to have one of our actors fetch the latest content from an RSS feed 30 minutes in the future. We can use [`IScheduler.ScheduleTellOnce()`](http://api.getakka.net/docs/stable/html/190E4EB.htm "Akka.NET Stable API Docs - IScheduler.ScheduleTellOnce method") to do that:

```fsharp
let actorSystem = System.create "myActorSystem" (Configuration.load ())
let someActor = spawn actorSystem "someActor" (actorOf sampleActor)
let someMessage = { Uri = ... }

//schedule the one-off message
actorSystem.Scheduler.ScheduleTellOnce (TimeSpan.FromMinutes 30., someActor, someMessage)
```

Voila! `someActor` will receive `someMessage` in 30 minutes time.

#### Schedule recurring messages with `ScheduleTellRepeatedly()`
Now, **what if we want to schedule this message to be delivered once *every 30 minutes*?**

For this we can use the following [`IScheduler.ScheduleTellRepeatedly()`](http://api.getakka.net/docs/stable/html/A909C289.htm "Akka.NET Stable API Docs - IScheduler.ScheduleTellRepeatedly") overload.

```fsharp
let actorSystem = System.create "myActorSystem" (Configuration.load ())
let someActor = spawn actorSystem "someActor" (actorOf sampleActor)
let someMessage = { Uri = ... }

//schedule the recurring message
actorSystem.Scheduler.ScheduleTellRepeatedly(
							TimeSpan.FromMinutes 30.,
							TimeSpan.FromMinutes 30.,
							someActor,
							someMessage)
```

That's it!

### How do I cancel a scheduled message?
What happens if we need to cancel a scheduled or recurring message? We use a [`ICancelable`](http://api.getakka.net/docs/stable/html/3FA8058E.htm "Akka.NET Stable API Docs - ICancelable interface"), which we can create using a [`Cancelable`](http://api.getakka.net/docs/stable/html/8869EC52.htm) instance.

First, the message must be scheduled so that it can be cancelled. If a message is cancelable, we then just have to call `Cancel()` on our handle to the `ICancelable` and it will not be delivered. For example:

```fsharp
let actorSystem = System.create "myActorSystem" (Configuration.load ())
let cancellation = new Cancelable (actorSystem.Scheduler)
let sampleActor = spawn actorSystem "someActor" (actorOf sampleActor)
let sampleMessage = { Uri = ... }

// first, set up the message so that it can be cancelled
actorSystem.Scheduler.ScheduleTellRepeatedly (
							TimeSpan.FromMinutes 30.,
							TimeSpan.FromMinutes 30.,
							sampleActor,
							sampleMessage,
							ActorRefs.NoSender,
							cancellation)

// here we actually cancel the message and prevent it from being delivered
cancellation.Cancel ()
```

#### Alternative: get an `ICancelable` task using `ScheduleTellRepeatedlyCancelable`
One of the new `IScheduler` methods we introduced in Akka.NET v1.0 is the [`ScheduleTellRepeatedlyCancelable` extension method](http://api.getakka.net/docs/stable/html/9B66375D.htm "Akka.NET API Docs - SchedulerExtensions.ScheduleTellRepeatedlyCancelable extension method")]. This extension method inlines the process of creating an `ICancelable` instance for your recurring messages and simply returns an `ICancelable` for you.

```fsharp
let actorSystem = System.create "myActorSystem" (Configuration.load ())
let cancellation = new Cancelable (actorSystem.Scheduler)
let sampleActor = spawn actorSystem "someActor" (actorOf sampleActor)
let sampleMessage = { Uri = ... }

// cancellable recurring message send created automatically
let cancellation = actorSystem.Scheduler.ScheduleTellRepeatedlyCancelable (
							TimeSpan.FromMinutes 30.,
							TimeSpan.FromMinutes 30.,
							sampleActor,
							sampleMessage,
							ActorRefs.NoSender)

// here we actually cancel the message and prevent it from being delivered
cancellation.Cancel ()
```
This is a more concise alternative to the previous example, and we recommend using it going forward even though we won't be using it in this bootcamp.

### How precise is the timing of scheduled messages?
***Scheduled messages are more than precise enough for all the use cases we've come across.***

That said, there are two situations of imprecision that we're aware of:

1. Scheduled messages are scheduled onto the CLR threadpool and use `Task.Delay` under the hood. If there is a high load on the CLR threadpool, the task might finish a little later than planned. There is no guarantee that the task will execute at EXACTLY the millisecond you expect.
2. If your scheduling requirements demand precision below 15 milliseconds then the `Scheduler` is not precise enough for you. Nor is any typical operating system such as Windows, OSX, or Linux. This is because ~15ms is the interval in which Windows and other general OSes update their system clock ("clock resolution"), so these OSs can't support any timing more precise than their own system clocks.

### What are the various overloads of `Schedule` and `ScheduleOnce`?
Here are all the overload options you have for scheduling a message.

#### Overloads of `ScheduleTellRepeatedly`
These are the various API calls you can make to schedule recurring messages.

[Refer to the `IScheduler` API documentation](http://api.getakka.net/docs/stable/html/FB15E2E6.htm "Akka.NET Stable API Documentation - IScheduler Interface").

#### Overloads of `ScheduleTellOnce`
These are the various API calls you can make to schedule one-off messages.

[Refer to the `IScheduler` API documentation](http://api.getakka.net/docs/stable/html/FB15E2E6.htm "Akka.NET Stable API Documentation - IScheduler Interface").

### How do I do Pub/Sub with Akka.NET Actors?
It's actually very simple. Many people expect this to be very complicated and are suspicious that there isn't more code involved. Rest assured, there's nothing magic about pub/sub with Akka.NET actors. It can literally be as simple as this:

```fsharp
type Message =
    | Subscribe of IActorRef
    | Unsubscribe of IActorRef
    | Msg of string

let publisherActor (mailbox:Actor<_>) =
    let rec loop subscriptions = actor {
        let! message = mailbox.Receive ()
		
        match box message :?> Message with
        | Msg content -> // iterate through subscription list and send message to each subscriber
            subscriptions |> Seq.iter (fun subscriber -> subscriber <! content)
            return! loop subscriptions
        | Subscribe subscriber -> // add subscriber to subscription list (just in case, remove subscriber if duplicate)
            let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun i -> i <> subscriber)
            return! loop (subscriber::subscriptionsWithoutSubscriber)
        | Unsubscribe subscriber -> // remove subscriber from subscription list
            let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun i -> i <> subscriber)
            return! loop subscriptionsWithoutSubscriber
    }
    loop [] // start with an empty subscription list

let subscriberActor (mailbox:Actor<_>) msg =
    printfn "%A => %A" mailbox.Self.Path msg

let actorSystem = System.create "myactorsystem" (Configuration.load ())
let publisher = spawn actorSystem "publisher" publisherActor
let subscriber1 = spawn actorSystem "subscriber1" (actorOf2 subscriberActor)
let subscriber2 = spawn actorSystem "subscriber2" (actorOf2 subscriberActor)
let subscriber3 = spawn actorSystem "subscriber3" (actorOf2 subscriberActor)

publisher <! Subscribe subscriber1
publisher <! Msg ("hello")
publisher <! Unsubscribe subscriber1

publisher <! Subscribe subscriber2
publisher <! Subscribe subscriber3
publisher <! Msg ("hello again")
```

Pub/sub is trivial to implement in Akka.NET and it's a pattern you can feel comfortable using regularly when you have scenarios that align well with it.
> **NOTE**: The tail recursive call that processes the messages takes a list of subscriptions. We start with an empty subscription list and then add/filter the subscriber based on subscribe/unsubscribe. **This avoids the issue of using mutable variables.**

Now that you're familiar with how the `Scheduler` works, lets put it to use and make our charting UI reactive!

## Exercise
**HEADS UP:** This section is where 90% of the work happens in all of Unit 2. We're going to add a few new actors who are responsible for setting up pub/sub relationships with the `chartingActor` in order to graph `PeformanceCounter` data at regular intervals.

### Step 1 - Add 3 New Buttons to `Form.fs`

We are not going to need our `btnAddSeries` anymore, so you can safely remove the following code from `Form.fs`:

```fsharp
// remove the button from the form
let btnAddSeries = new Button(Name = "btnAddSeries", Text = "Add Series", Location = Point(540, 300), Size = Size(100, 40), TabIndex = 1, UseVisualStyleBackColor = true)
form.Controls.Add btnAddSeries
// and remove its click handler as well
 btnAddSeries.Click.Add (fun _ -> 
	let newSeriesName = sprintf "FakeSeries %i" (sysChart.Series.Count + 1)    
	let newSeries = ChartDataHelper.randomSeries newSeriesName None None
	chartActor <! AddSeries newSeries
)
```

Instead, We're going to add three new buttons and click handlers. Here are the names we'll be using for each button when we refer to them later:

* **CPU (ON)** - `btnCpu`
* **MEMORY (OFF)** - `btnMemory`
* **DISK (OFF)** - `btnDisk`


```fsharp
// create the buttons
let btnCpu = new Button(Name = "btnCpu", Text = "CPU (ON)", Location = Point(560, 275), Size = Size(110, 40), TabIndex = 1, UseVisualStyleBackColor = true)
let btnMemory = new Button(Name = "btnMemory", Text = "MEMORY (OFF)", Location = Point(560, 320), Size = Size(110, 40), TabIndex = 2, UseVisualStyleBackColor = true)
let btnDisk = new Button(Name = "btnDisk", Text = "DISK (OFF)", Location = Point(560, 365), Size = Size(110, 40), TabIndex = 3, UseVisualStyleBackColor = true)

// and add them to the form
form.Controls.Add btnCpu
form.Controls.Add btnMemory
form.Controls.Add btnDisk
```

Now let's add a click handler for each of our new buttons in the `load` function:

```fsharp
btnCpu.Click.Add (fun _ -> () )
btnMemory.Click.Add (fun _ -> () )
btnDisk.Click.Add (fun _ -> () )
```

We'll fill in these handlers later.

### Step 2 - Add Some New Message Types
We're going to add a few new actors to our project in a moment, but before we do that let's define some new message types to our `Messages` module in `Actors.fs`:

```fsharp
type CounterType =
	| Cpu = 1
	| Memory = 2
	| Disk = 3

type CounterMessage =
	| GatherMetrics
	| SubscribeCounter of subscriber: IActorRef
	| UnsubscribeCounter of subscriber: IActorRef

type CoordinationMessage =
	| Watch of counter: CounterType
	| Unwatch of counter: CounterType

type ButtonMessage = Toggle

// and also add new cases to the existing ChartMessage:
type ChartMessage = 
    | InitializeChart of initialSeries: Map<string, Series>
    | AddSeries of series: Series
    | RemoveSeries of seriesName: string
	| Metric of series: string * counterValue: float
```

Now we can start adding the actors who depend on these message definitions.

### Step 3 - Create the `PerformanceCounterActor`

The `performanceCounterActor` is the actor who's going to publish `PerformanceCounter` values to the `chartingActor` using Pub/Sub and the `Scheduler`.

Type the following in `Actors.fs`:

```fsharp
// add those at the beginning of the file, we are going to need them later.
open System.Diagnostics
open System.Drawing

// in the Actors module
let performanceCounterActor (seriesName: string) (perfCounterGenerator: unit -> PerformanceCounter) (mailbox: Actor<_>) =
	let counter = perfCounterGenerator ()
	let cancelled = mailbox.Context.System.Scheduler.ScheduleTellRepeatedlyCancelable (
						TimeSpan.FromMilliseconds 250.,
						TimeSpan.FromMilliseconds 250.,
						mailbox.Self,
						GatherMetrics,
						ActorRefs.NoSender)

	mailbox.Defer (fun _ ->
		cancelled.Cancel ()
		counter.Dispose () |> ignore)

	let rec loop subscriptions = actor {
		let! message = mailbox.Receive ()

		match box message :?> CounterMessage with
		| GatherMetrics ->
			let msg = Metric(seriesName, counter.NextValue () |> float)
			subscriptions |> Seq.iter (fun subscriber -> subscriber <! msg)
			return! loop subscriptions
		| SubscribeCounter sub ->
			let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun actor -> actor <> sub)
			return! loop (sub::subscriptionsWithoutSubscriber)
		| UnsubscribeCounter sub ->
			let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun actor -> actor <> sub)
			return! loop subscriptionsWithoutSubscriber
	}
	loop []
```

*Before we move onto the next step, let's talk about what you just did...*

#### Generator for Reliability
Did you notice how `performanceCounterActor` takes a `unit -> PerformanceCounter` and NOT a `PerformanceCounter`? If you didn't, go back and look now. What gives? We use it whenever we have to inject an `IDisposable` object into the constructor of an actor. Why?

Well, we've got an actor that takes an `IDisposable` object as a parameter. So we're going to assume that this object will actually become `Disposed` at some point and will no longer be available.

What happens when the `performanceCounterActor` needs to restart?

**Every time the `peformanceCounterActor` attempts to restart it will re-use its original arguments, which includes reference types**. If we re-use the same reference to the now-`Disposed` `PerformanceCounter`, the actor will crash repeatedly. Until its parent decides to just kill it altogether.


#### Pub / Sub Made Easy
The `performanceCounterActor` has pub / sub built into it by way of its handlers for `SubscribeCounter` and `UnsubscribeCounter` messages:

```fsharp
| SubscribeCounter sub ->
	let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun actor -> actor <> sub)
	return! loop (sub::subscriptionsWithoutSubscriber)
| UnsubscribeCounter sub ->
	let subscriptionsWithoutSubscriber = subscriptions |> List.filter (fun actor -> actor <> sub)
	return! loop subscriptionsWithoutSubscriber
```

In this lesson, `performanceCounterActor` only has one subscriber (`chartingActor` inside `Actors.fs`) but with a little re-architecting you could have these actors publishing their `PeformanceCounter` data to multiple recipients. Maybe that's a do-it-yourself exercise you can try later? ;)

#### How did we schedule publishing of `PeformanceCounter` data?
Inside the `PreStart` lifecycle method, we used the `Context` object to get access to the `Scheduler`, and then we had `peformanceCounterActor` send itself a `GatherMetrics` method once every 250 milliseconds.

This causes `peformanceCounterActor` to fetch data every 250ms and publish it to `chartingActor`, giving us a live graph with a frame rate of 4 FPS.

```fsharp					
let counter = perfCounterGenerator ()
let cancelled = mailbox.Context.System.Scheduler.ScheduleTellRepeatedlyCancelable (
					TimeSpan.FromMilliseconds 250.,
					TimeSpan.FromMilliseconds 250.,
					mailbox.Self,
					GatherMetrics,
					ActorRefs.NoSender)
```

Notice that inside the `performanceCounterActor`'s `Defer` function, we invoke the `ICancelable` we created to cancel this recurring message:

```fsharp
mailbox.Defer (fun _ ->
	cancelled.Cancel () |> ignore  // terminate the scheduled task
	counter.Dispose () |> ignore  // stop the generator
)
```
We do this for the same reason we `Dispose` the `PerformanceCounter` - to eliminate resource leaks and to prevent the `IScheduler` from sending recurring messages to dead or restarted actors.

### Step 4 - Create the `performanceCounterCoordinatorActor`

The `performanceCounterCoordinatorActor` is the interface between the `chartingActor` and all of the `performanceCounterActor` instances.

It has the following jobs:

* Lazily create all `peformanceCounterActor` instances that are requested by the end-user
* Provide the `peformanceCounterActor` with a function (`unit -> PerformanceCounter`) for creating its counters
* Manage all counter subscriptions for the `chartingActor`
* Tell the `chartingActor` how to render each of the individual counter metrics (which colors and plot types to use for each `Series` that corresponds with a `PeformanceCounter`)

Sounds complicated, right? Well, you'll be surprised when you see how small the code footprint is!

Create the following actor in `Actors.fs`:

```fsharp
// Actors/performanceCoordinatorActor
let performanceCounterCoordinatorActor chartingActor (mailbox: Actor<_>) =
	let counterGenerators = Map.ofList [CounterType.Cpu, fun _ -> new PerformanceCounter("Processor", "% Processor Time", "_Total", true)
										CounterType.Memory, fun _ -> new PerformanceCounter("Memory", "% Committed Bytes In Use", true)
										CounterType.Disk, fun _ -> new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)]

	let counterSeries = Map.ofList [CounterType.Cpu, fun _ -> new Series(string CounterType.Cpu, ChartType = SeriesChartType.SplineArea, Color = Color.DarkGreen)
									CounterType.Memory, fun _ -> new Series(string CounterType.Memory, ChartType = SeriesChartType.FastLine, Color = Color.MediumBlue)
									CounterType.Disk, fun _ -> new Series(string CounterType.Disk, ChartType = SeriesChartType.SplineArea, Color = Color.DarkRed)]

	let rec loop (counterActors: Map<CounterType, IActorRef>) = actor {
		let! message = mailbox.Receive ()

		match message with
		| Watch counter when counterActors |> Map.containsKey counter |> not ->
			let counterName = string counter
			let actor = spawn mailbox.Context (sprintf "counterActor-%s" counterName) (performanceCounterActor counterName counterGenerators.[counter])
			let newCounterActors = counterActors.Add (counter, actor)
			chartingActor <! AddSeries(counterSeries.[counter] ())
			newCounterActors.[counter] <! SubscribeCounter(chartingActor)
			return! loop newCounterActors
		| Watch counter ->
			chartingActor <! AddSeries(counterSeries.[counter] ())
			counterActors.[counter] <! SubscribeCounter(chartingActor)
		| Unwatch counter when counterActors |> Map.containsKey counter ->
			chartingActor <! RemoveSeries((counterSeries.[counter] ()).Name)
			counterActors.[counter] <! UnsubscribeCounter(chartingActor)
		
		return! loop counterActors
	}
	loop Map.empty
```

*Notice that the tail recursive call that processes the messages. We start with an empty map and add to the map when we add a new actor.*

Okay, we're almost there. Just one more actor to go!

### Step 5 - Create the `buttonToggleActor`
You didn't think we were going to let you just fire off those buttons you created in Step 2 without adding some actors to manage them, did you? ;)

In this step, we're going to add a new type of actor that will run on the UI thread just like the `chartingActor`.

The job of the `buttonToggleActor` is to turn click events on the `Button` it manages into messages for the `performanceCounterCoordinatorActor`. The `buttonToggleActor` also makes sure that the visual state of the `Button` accurately reflects the state of the subscription managed by the `peformanceCounterCoordinatorActor` (e.g. ON/OFF).

Type the following in `Actors.fs`:

```fsharp
// Actors/buttonToggleActor
let buttonToggleActor coordinatorActor (button: System.Windows.Forms.Button) counterType isToggled (mailbox: Actor<_>) =
	let flipToggle isOn =
		let isToggledOn = not isOn
		button.Text <- sprintf "%s (%s)" (counterType.ToString().ToUpperInvariant()) (if isToggledOn then "ON" else "OFF")
		isToggledOn

	let rec loop isToogledOn = actor {
		let! message = mailbox.Receive ()

		match message with
		| Toggle when isToogledOn -> coordinatorActor <! Unwatch(counterType)
		| Toggle when not isToogledOn -> coordinatorActor <! Watch(counterType)
		| m -> mailbox.Unhandled m

		return! loop (flipToggle isToogledOn)
	}
	loop isToggled
```

### Step 6 - Update the `chartingActor`
Home stretch! We're almost there.

We need to integrate all of the new message types we defined in Step 2 into the `chartingActor`. We also need to make some changes to the way we render the `Chart` since we're going to be making *live updates* to it continuously.

To start, add the `setChartBoundaries` function at the very top of `chartingActor`. It is is used to make sure that the boundary area of our chart gets updated as we remove old points from the beginning of the chart as time elapses.

```fsharp
// Actors/chartingActor
let chartingActor (chart: Chart) (mailbox:Actor<_>) =

	let maxPoints = 250

	let setChartBoundaries (mapping: Map<string, Series>, numberOfPoints: int) =
		let allPoints =
                mapping
                |> Map.toList
                |> Seq.collect (fun (_, series) -> series.Points)
                |> (fun points -> HashSet<DataPoint>(points))
				
		if allPoints |> Seq.length > 2 then
			let yValues = allPoints |> Seq.collect (fun p -> p.YValues) |> Seq.toList
			chart.ChartAreas.[0].AxisX.Maximum <- float numberOfPoints
			chart.ChartAreas.[0].AxisX.Minimum <- (float numberOfPoints - float maxPoints)
			chart.ChartAreas.[0].AxisY.Maximum <- if List.length yValues > 0 then Math.Ceiling(List.max yValues) else 1.
			chart.ChartAreas.[0].AxisY.Minimum <- if List.length yValues > 0 then Math.Floor(List.min yValues) else 0.
		else
			()
```

Then onto the actor itself! We need to handle the new `ChartMessage` cases we added in step 2. Let's update our `charting` function accordingly:

```fsharp
let rec charting(mapping: Map<string, Series>, numberOfPoints: int) = actor {
	let! message = mailbox.Receive ()

	match message with  
	| InitializeChart series ->
		chart.Series.Clear ()
		chart.ChartAreas.[0].AxisX.IntervalType <- DateTimeIntervalType.Number
		chart.ChartAreas.[0].AxisY.IntervalType <- DateTimeIntervalType.Number
		series |> Map.iter (fun k v ->
			v.Name <- k
			chart.Series.Add v)
		return! charting(series, numberOfPoints)

	| AddSeries series when not <| String.IsNullOrEmpty series.Name && not <| (mapping |> Map.containsKey series.Name) ->
		let newMapping = mapping.Add (series.Name, series)
		chart.Series.Add series
		setChartBoundaries (newMapping, numberOfPoints)
		return! charting (newMapping, numberOfPoints)

	| RemoveSeries seriesName when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName ->
		chart.Series.Remove mapping.[seriesName] |> ignore
		let newMapping = mapping.Remove seriesName
		setChartBoundaries (newMapping, numberOfPoints)
		return! charting (newMapping, numberOfPoints)

	| Metric (seriesName, counterValue) when not <| String.IsNullOrEmpty seriesName && mapping |> Map.containsKey seriesName ->
		let newNoOfPts = numberOfPoints + 1
		let series = mapping.[seriesName]
		series.Points.AddXY (numberOfPoints, counterValue) |> ignore
		while (series.Points.Count > maxPoints) do series.Points.RemoveAt 0
		setChartBoundaries (mapping, newNoOfPts)
		return! charting (mapping, newNoOfPts)
}
charting (Map.empty<string, Series>, 0)
```

our `chartingActor` is now able to handle removal of a given series as well as actually getting to add new data points to an existing series!

### Step 7 - Update the `load` function in `Form.fs`
Now that we have real data we want to plot in real-time, we need to update the original `load` event handler that original supplied fake data to our `chartActor`.
Update the `load` function so it looks like this:

```fsharp
let load (myActorSystem:ActorSystem) = 
	let chartActor = spawn myActorSystem "charting" (Actors.chartingActor sysChart)
	let coordinatorActor = spawn myActorSystem "counters" (Actors.performanceCounterCoordinatorActor chartActor)
	let toggleActors = Map.ofList [(CounterType.Cpu, spawnOpt myActorSystem "cpuCounter" (Actors.buttonToggleActor coordinatorActor btnCpu CounterType.Cpu false) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])
									(CounterType.Memory, spawnOpt myActorSystem "memoryCounter" (Actors.buttonToggleActor coordinatorActor btnMemory CounterType.Memory false) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])
									(CounterType.Disk, spawnOpt myActorSystem "diskCounter" (Actors.buttonToggleActor coordinatorActor btnDisk CounterType.Disk false) [SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")])]

	// the CPU counter will auto-start at launch
	toggleActors.[CounterType.Cpu] <! Toggle

	btnCpu.Click.Add (fun _ -> () )
	btnMemory.Click.Add (fun _ -> () )
	btnDisk.Click.Add (fun _ -> () )

	form
```

#### Wait a minute, what's this `SpawnOption.Dispatcher` nonsense?!
`spawnOpt` allows you to configure your actor deployments programmatically. In this instance we decided to use the `[SpawnOption.Dispatcher("akka.actor.synchronized-dispatcher")]` to guarantee that each of the `buttonToggleActor` instances run on the UI thread.

As we saw in [Lesson 2.1](../lesson1/), you can also configure the `Dispatcher` for an actor via the HOCON config. So if an actor has a `Dispatcher` set in HOCON, *and* one declared programmatically via `spawnOpt`, which wins?

*In case of a conflict, `Config` wins and `spawnOpt` loses .* Any conflicting settings declared by the `spawnOpt` fluent interface will always be overriden by what was declared in the HOCON configuration.

### Step 8 - Have Button Handlers Send `Toggle` Messages to Corresponding `buttonToggleActor`
**THIS IS THE LAST STEP.** We promise :) Thanks for hanging in there.

Finally, we need to wire up the button handlers we created in Step 3.

Wire up your button handlers in `load` function for `Form.fs`. They should look like this:

```fsharp
// wiring up the button handlers added in step 3
btnCpu.Click.Add (fun _ -> toggleActors.[CounterType.Cpu] <! Toggle)
btnMemory.Click.Add (fun _ -> toggleActors.[CounterType.Memory] <! Toggle)
btnDisk.Click.Add (fun _ -> toggleActors.[CounterType.Disk] <! Toggle)

```

### Once you're done
Build and run `SystemCharting.sln` and you should see the following:

![Successful Lesson 3 Output](images/dothis-successful-run3.gif)

Compare your code to the code in the [/Completed/ folder](Completed/) to compare your final output to what the instructors produced.

## Great job!
*Wow*. That was *a lot* of code. Great job and thanks for sticking it out! We now have a fully functioning Resource Monitor app, implemented with actors.

Every other lesson builds on this one, so before continuing, please make sure your code matches the output of the [/Completed/ folder](Completed/).

At this point. you should understand how the `Scheduler` works and how you can use it alongside patterns like Pub-sub to make very reactive systems with actors that have a comparatively small code footprint.

Here is a high-level overview of our working system at this point:

![Akka.NET Bootcamp Unit 2 System Overview](images/system_overview_2_3.png)

**Let's move onto [Lesson 4 - Switching Actor Behavior at Run-time with `BecomeStacked` and `UnbecomeStacked`](../lesson4).**

## Any questions?
**Don't be afraid to ask questions** :).

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](https://github.com/petabridge/akka-bootcamp/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.
