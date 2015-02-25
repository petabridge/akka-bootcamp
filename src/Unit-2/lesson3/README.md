# Lesson 2.3: Using the `Scheduler` to Send Recurring Messages

One of the most powerful capabilities Akka.NET exposes is the ability to schedule messages to be sent in the future, including regularly occurring messages. All of this accomplished via the [`Scheduler` in Akka.NET](http://getakka.net/wiki/Scheduler).

In this lesson you're going to get to learn two really powerful Akka.NET concepts:

1. How to use the `Scheduler` and
2. How to implement the [Publish-subscribe (pub-sub) pattern](http://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) using Actors, a powerful technique for creating reactive systems.

## Keys Concepts / Background

Being able to schedule messages to be sent at in the future is really useful in situations where you want to an actor to do *something later*. 

If you want an actor to fetch the latest information from a [Performance Counter](https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.aspx "PerformanceCounter Class - C#"), for instance, you might schedule a specific message to be sent to that actor at a regular interval.

And this is exactly what the [`Scheduler`](http://getakka.net/wiki/Scheduler) allows you to do!

> **FUTURE BREAKING CHANGES:** [The `Scheduler` API is going to be modified as part of the upcoming Akka.NET V1 release](https://github.com/akkadotnet/akka.net/issues/468). All of the `Scheduler`'s capabilities will remain intact, but the API signatures will be different. 

### Scheduling Future Messages
Let's say we want to have one of our actors fetch the latest content from an RSS feed 30 minutes in the future. We can use the `Scheduler` to do that:

```csharp
var system = ActorSystem.Create("MySystem");
var someActor = system.ActorOf<SomeActor>("someActor");
var someMessage = new FetchFeed() {Url = ...};
system
   .Scheduler
   .Schedule(TimeSpan.FromMinutes(30), //initial delay of 30 min
             someActor, someMessage);
```

Viola - `someActor` will receive `someMessage` in 30 minutes time.

Now **what if we want to schedule this message to be delivered once *every 30 minutes*?** Then we can use the following `Schedule` overload.

```csharp
var system = ActorSystem.Create("MySystem");
var someActor = system.ActorOf<SomeActor>("someActor");
var someMessage = new FetchFeed() {Url = ...};
system
   .Scheduler
   .Schedule(TimeSpan.FromMinutes(30), //initial delay of 30 min
			 TimeSpan.FromMinutes(30) //recur every 30 minutes
             someActor, someMessage);
```

That's it!

### Cancelling Scheduled Messages

What happens if we need to cancel a scheduled or recurring message? We use a `CancellationToken` by way of a [`CancellationTokenSource`](https://msdn.microsoft.com/en-us/library/vstudio/system.threading.cancellationtokensource.aspx) for that!

```csharp
var cancellation = new CancellationTokenSource();
var system = ActorSystem.Create("MySystem");
var someActor = system.ActorOf<SomeActor>("someActor");
var someMessage = new FetchFeed() {Url = ...};
system
   .Scheduler
   .Schedule(TimeSpan.FromMinutes(30), //initial delay of 30 min
             someActor, someMessage, 
			cancellation.Token); //add cancellation support
cancellation.Cancel(); //stop the delivery of the message
```

### Pub/Sub with Akka.NET Actors
There's nothing magic about pub/sub with Akka.NET actors - it can literally be as simple as this:

```csharp
public class PubActor : ReceiveActor{
	//HashSet automatically eliminates duplicates
	private HashSet<ActorRef> _subscribers;
	PubActor(){
		_subscribers = new HashSet<ActorRef>();
		Receive<Subscribe>(sub => {
			_subscribers.Add(sub.ActorRef);
		});
		
		Receive<ThingSubscribersWant>(thing => {
			//notify each subscriber
			foreach(var sub in _subscribers){
				sub.Tell(thing);
			}
		});

		Receive<Unsubscribe>(unsub => {
			_subscribers.Remove(unsub.ActorRef);
		}
	}
}
```

Pub sub is trivial to implement in Akka.NET, and it's a pattern you can feel comfortable using regularly when you have scenarios that align well with it.

## Exercise

**HEADS UP.** This is where 90% of the work happens in Unit 2. We're going to introduce a few new actors who are responsible for setting up pub / sub relationships with the `ChartingActor` in order to publish `PeformanceCounter` data at regular intervals.

### Phase 1 - Delete the "Add Series" Button and Click Handler from Lesson 2

We're not doing to need it. **Delete the "Add Series" button** from the **[Design]** view of `Main.cs` and remove the click handler:

```csharp
// Main.cs - Main
// DELETE ME
private void button1_Click(object sender, EventArgs e)
{
    var series = ChartDataHelper.RandomSeries("FakeSeries" + _seriesCounter.GetAndIncrement());
    _chartActor.Tell(new ChartingActor.AddSeries(series));
}
```

### Phase 2 - Add 3 New Buttons to `Main.cs`

We're going to add three new buttons and click handlers for each:

* **CPU (ON)**
* **MEMORY (OFF)**
* **DISK (OFF)**

Your **[Design]** view in Visual Studio for `Main.cs` should look like this:

![Add 3 buttons for tracking different performance counter metrics](images/add-3-buttons.png)

Make sure that you given a descriptive name to each of these buttons, because we're going to need to refer to them later. You can set a descriptive name for each using the **Properties** window in Visual Studio:

![Set a descriptive name for each button](images/button-properties-window.png)

Here are the names we'll be using for each button when we refer to them later:

* **CPU (ON)** - `btnCpu`
* **MEMORY (OFF)** - `btnMemory`
* **DISK (OFF)** - `btnDisk`

Once you've renamed your buttons, add click handlers for each by simply double-clicking on the buttons in **[Design]** view.

```csharp
// Main.cs - Main
private void btnCpu_Click(object sender, EventArgs e)
{
   
}

private void btnMemory_Click(object sender, EventArgs e)
{
    
}

private void btnDisk_Click(object sender, EventArgs e)
{
    
}
```

We'll fill these handlers in later.

### Phase 3 - Add Some New Message Types
We're going to add a few new actors to our project in a moment, but before we do that let's create a new file inside the `/Actors` folder in our project and define some new message types:

```csharp
// Actors/ChartingMessages.cs

using Akka.Actor;

namespace ChartApp.Actors
{
    #region Reporting

    /// <summary>
    /// Signal used to indicate that it's time to sample all counters
    /// </summary>
    public class GatherMetrics { }

    /// <summary>
    /// Metric data at the time of sample
    /// </summary>
    public class Metric
    {
        public Metric(string series, float counterValue)
        {
            CounterValue = counterValue;
            Series = series;
        }

        public string Series { get; private set; }

        public float CounterValue { get; private set; }
    }

    #endregion

    #region Performance Counter Management

    /// <summary>
    /// All types of counters supported by this example
    /// </summary>
    public enum CounterType
    {
        Cpu,
        Memory,
        Disk
    }

    /// <summary>
    /// Enables a counter and begins publishing values to <see cref="Subscriber"/>.
    /// </summary>
    public class SubscribeCounter
    {
        public SubscribeCounter(CounterType counter, ActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }

        public CounterType Counter { get; private set; }

        public ActorRef Subscriber { get; private set; }
    }

    /// <summary>
    /// Unsubscribes <see cref="Subscriber"/> from receiving updates for a given counter
    /// </summary>
    public class UnsubscribeCounter
    {
        public UnsubscribeCounter(CounterType counter, ActorRef subscriber)
        {
            Subscriber = subscriber;
            Counter = counter;
        }

        public CounterType Counter { get; private set; }

        public ActorRef Subscriber { get; private set; }
    }

    #endregion
}
```

Now we can start adding the actors who depend on these message definitions.

### Phase 4 - Create the `PerformanceCounterActor`

The `PerformanceCounterActor` is the actor who's going to publish `PerformanceCounter` values to the `ChartingActor` using Pub/Sub and the `Scheduler`.

Create a new file in the `/Actors` folder called `PerformanceCounterActor.cs` and type the following:

```csharp
// Actors/PerformanceCounterActor.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for monitoring a specific <see cref="PerformanceCounter"/>
    /// </summary>
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private PerformanceCounter _counter;

        private readonly HashSet<ActorRef> _subscriptions;
        private readonly CancellationTokenSource _cancelPublishing;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            _seriesName = seriesName;
            _performanceCounterGenerator = performanceCounterGenerator;
            _subscriptions = new HashSet<ActorRef>();
            _cancelPublishing = new CancellationTokenSource();
        }

        #region Actor lifecycle methods

        protected override void PreStart()
        {
            //create a new instance of the performance counter
            _counter = _performanceCounterGenerator();
            Context.System.Scheduler.Schedule(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250), Self,
                new GatherMetrics(), _cancelPublishing.Token);
        }

        protected override void PostStop()
        {
            try
            {
                //terminate the scheduled task
                _cancelPublishing.Cancel(false);
                _counter.Dispose();
            }
            catch 
            {
                //don't care about additional "ObjectDisposed" exceptions
            }
            finally
            {
                base.PostStop();    
            }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            if (message is GatherMetrics)
            {
                //publish latest counter value to all subscribers
                var metric = new Metric(_seriesName, _counter.NextValue());
                foreach(var sub in _subscriptions)
                    sub.Tell(metric);
            }
            else if (message is SubscribeCounter)
            {
                // add a subscription for this counter
                // (it's parent's job to filter by counter types)
                var sc = message as SubscribeCounter;
                _subscriptions.Add(sc.Subscriber);
            }
            else if (message is UnsubscribeCounter)
            {
                // remove a subscription from this counter
                var uc = message as UnsubscribeCounter;
                _subscriptions.Remove(uc.Subscriber);
            }
        }
    }
}
```

Before we move onto the next phase, let's talk about what you just did.

#### Functional Programming for Relability

First and foremost, notice how we take a `Func<PerformanceCounter>` in the constructor of the `PerformanceCounterActor` and not a `PerformanceCounter`? This is a functional programming technique, and we use it whenever we have to inject an `IDisposable` object into the constructor of an actor.

Why? Because what happens the `PerformanceCounter` instance fails, becomes `Disposed`, and the `PerformanceCounterActor` needs to restart? **Every time the `PeformanceCounterActor` attempts to restart it will re-use its original constructor arguments, which includes reference types**. If we re-use the same reference to the disposed `PerformanceCounter` the actor will crash repeatedly until its parent decides to just kill it altogether.

A better technique is to pass a function the `PerformanceCounterActor` can use to get a fresh instance of its `PeformanceCounter`, and that's exactly why we use a `Func<PerformanceCounter>` in the constructor, which gets invoked during the actor's `PreStart()` lifecycle method.

```csharp
//create a new instance of the performance counter
_counter = _performanceCounterGenerator();
```

Because of this, we also have to clean up the `PeformanceCounter` instance inside the `PostStop` lifecycle method of the actor - because we know we're going to get a fresh instance of that counter if the actor restarts and we want to prevent resource leaks.

```csharp
protected override void PostStop()
{
    try
    {
        //terminate the scheduled task
        _cancelPublishing.Cancel(false);
        _counter.Dispose();
    }
    catch 
    {
        //don't care about additional "ObjectDisposed" exceptions
    }
    finally
    {
        base.PostStop();    
    }
}
```

#### Pub / Sub Made Easy

The `PerformanceCounterActor` has pub / sub built into it by way of its handlers for `SubscribeCounter` and `UnsubscribeCounter` messages inside its `OnReceive` method:

```csharp
else if (message is SubscribeCounter)
{
    // add a subscription for this counter
    // (it's parent's job to filter by counter types)
    var sc = message as SubscribeCounter;
    _subscriptions.Add(sc.Subscriber);
}
else if (message is UnsubscribeCounter)
{
    // remove a subscription from this counter
    var uc = message as UnsubscribeCounter;
    _subscriptions.Remove(uc.Subscriber);
}
```

In this example we're only going to have one subscriber (the instance of `ChartingActor` started from inside `Main.cs`) but with a little re-architecture you could have these actors publishing their `PeformanceCounter` data to multiple recipients. Maybe that's a do-it-yourself exercise you can try later? ;)

#### Scheduled Publishing of `PeformanceCounter` Data
Inside the `PreStart` lifecycle method we use the `Context` object to get access to the `Scheduler`, and then we have the `PeformanceCounterActor` send itself a `GatherMetrics` method once every 250 milliseconds - which will give us a framerate of 4 FPS.

```csharp
 protected override void PreStart()
{
    //create a new instance of the performance counter
    _counter = _performanceCounterGenerator();
    Context.System.Scheduler.Schedule(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250), Self,
        new GatherMetrics(), _cancelPublishing.Token);
}
```

Notice that inside the `PerformanceCounterActor`'s `PostStop` method we invoke the `CancellationTokenSource` we created to cancel this recurring message:

```csharp
 //terminate the scheduled task
_cancelPublishing.Cancel(false);
```

We do this for the same reason we `Dispose` the `PerformanceCounter` - to eliminate resource leaks and to prevent the `Scheduler` from sending recurring messages to dead or restarted actors.

### Phase 5 - Create the `PerformanceCounterCoordinatorActor`

The `PerformanceCounterCoordinatorActor` is the interface between the `ChartingActor` and all of the `PerformanceCounterActor` instances. 

It has the following jobs:

* Lazily create all `PeformanceCounterActor` instances that are requested by the end-user;
* Provide the `PeformanceCounterActor` with a factory method (`Func<PerformanceCounter>`) for creating its counters;
* Manage all counter subscriptions for the `ChartingActor`; and
* Tell the `ChartingActor` how to render each of the individual counter metrics (which colors and plot types to use for each `Series` that corresponds with a `PeformanceCounter`.)

Sound complicated? Then prepare to be surprised when you see how small the code footprint is!

Create a new file in the `/Actors` folder called `PerformanceCounterCoordinatorActor.cs` and type the following:

```csharp
// Actors/PerformanceCoordinatorActor.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for translating UI calls into ActorSystem messages
    /// </summary>
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        #region Message types

        /// <summary>
        /// Subscribe the <see cref="ChartingActor"/> to updates for <see cref="Counter"/>.
        /// </summary>
        public class Watch
        {
            public Watch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        /// <summary>
        /// Unsubscribe the <see cref="ChartingActor"/> to updates for <see cref="Counter"/>.
        /// </summary>
        public class Unwatch
        {
            public Unwatch(CounterType counter)
            {
                Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        #endregion

        /// <summary>
        /// Methods for generating new instances of all <see cref="PerformanceCounter"/>s we want to monitor
        /// </summary>
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerators = 
			new Dictionary<CounterType, Func<PerformanceCounter>>()
        {
            {CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)},
            {CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true)},
            {CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)},
        };

        /// <summary>
        /// Methods for creating new <see cref="Series"/> with distinct colors and names
		/// corresponding to each <see cref="PerformanceCounter"/>
        /// </summary>
        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries = 
			new Dictionary<CounterType, Func<Series>>()
        {
            {CounterType.Cpu, () => 
			new Series(CounterType.Cpu.ToString()){ ChartType = SeriesChartType.SplineArea,
			 Color = Color.DarkGreen}},
            {CounterType.Memory, () => 
			new Series(CounterType.Memory.ToString()){ ChartType = SeriesChartType.FastLine, 
			Color = Color.MediumBlue}},
            {CounterType.Disk, () => 
			new Series(CounterType.Disk.ToString()){ ChartType = SeriesChartType.SplineArea, 
			Color = Color.DarkRed}},
        };

        private Dictionary<CounterType, ActorRef> _counterActors;

        private ActorRef _chartingActor;

        public PerformanceCounterCoordinatorActor(ActorRef chartingActor) : 
			this(chartingActor, new Dictionary<CounterType, ActorRef>())
        {
        }

        public PerformanceCounterCoordinatorActor(ActorRef chartingActor, Dictionary<CounterType, ActorRef> counterActors)
        {
            _chartingActor = chartingActor;
            _counterActors = counterActors;

            Receive<Watch>(watch =>
            {
                if (!_counterActors.ContainsKey(watch.Counter))
                {
                    //create a child actor to monitor this counter if one doesn't exist already
                    var counterActor = Context.ActorOf(Props.Create(() => 
						new PerformanceCounterActor(watch.Counter.ToString(), CounterGenerators[watch.Counter])));

                    //add this counter actor to our index
                    _counterActors[watch.Counter] = counterActor;
                }

                //register this series with the ChartingActor
                _chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

                //tell the counter actor to begin publishing its statistics to the _chartingActor
                _counterActors[watch.Counter].Tell(new SubscribeCounter(watch.Counter, _chartingActor));
            });

            Receive<Unwatch>(unwatch =>
            {
                if (!_counterActors.ContainsKey(unwatch.Counter))
                {
                    return; // do nothing
                }

                //unsubscribe the ChartingActor from receiving anymore updates
                _counterActors[unwatch.Counter].Tell(new UnsubscribeCounter(unwatch.Counter, _chartingActor));

                //remove this series from the ChartingActor
                _chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));
            });
        }


    }
}
```
One more actor to go!

### Phase 6 - Create the `ButtonToggleActor`
You didn't think we were going to let you just fire off those buttons you created in Phase 2 without adding some actors to manage them, did you? ;)

In this step we're going to add a new type of actor who, just like the `ChartingActor`, also runs on the UI thread.

The `ButtonToggleActor`'s job to turn click events on the `Button` it manages into messages for the `PerformanceCounterCoordinatorActor` and to make sure that the visual state of the `Button` accurately reflects the state of the subscription managed by the `PeformanceCounterCoordinatorActor`.

Create a new file in the `/Actors` folder called `ButtonToggleActor.cs` and type the following:

```csharp
// Actors/ButtonToggleActor.cs

using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for managing button toggles
    /// </summary>
    public class ButtonToggleActor : UntypedActor
    {
        #region Message types

        /// <summary>
        /// Toggles this button on or off and sends an appropriate messages
        /// to the <see cref="PerformanceCounterCoordinatorActor"/>
        /// </summary>
        public class Toggle { }

        #endregion

        private readonly CounterType _myCounterType;
        private bool _isToggledOn;
        private readonly Button _myButton;
        private readonly ActorRef _coordinatorActor;

        public ButtonToggleActor(ActorRef coordinatorActor, Button myButton, 
				CounterType myCounterType, bool isToggledOn = false)
        {
            _coordinatorActor = coordinatorActor;
            _myButton = myButton;
            _isToggledOn = isToggledOn;
            _myCounterType = myCounterType;
        }

        protected override void OnReceive(object message)
        {
            if (message is Toggle && _isToggledOn)
            {
                //toggle is currently on

                //stop watching this counter
                _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Unwatch(_myCounterType));

                FlipToggle();
            }
            else if (message is Toggle && !_isToggledOn)
            {
                //toggle is currently off

                //start watching this counter
                _coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(_myCounterType));

                FlipToggle();
            }
            else
            {
                Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            //flip the toggle
            _isToggledOn = !_isToggledOn;
            
            //change the text of the button
            _myButton.Text = string.Format("{0} ({1})", _myCounterType.ToString().ToUpperInvariant(),
                _isToggledOn ? "ON" : "OFF");
        }
    }
}
```

### Phase 7 - Update the `ChartingActor`

We need to integrate all of the new message types we defined in Phase 3 into the `ChartingActor`, and we also need to make some changes to the way we render the `Chart` since we're going to be making *live updates* to it continuously.

Start by defining the following at the very top of the `ChartingActor` class:

```csharp
// Actors/ChartingActor.cs

/// <summary>
/// Maximum number of points we will allow in a series
/// </summary>
public const int MaxPoints = 250;

/// <summary>
/// Incrementing counter we use to plot along the X-axis
/// </summary>
private int xPosCounter = 0;
```

Next, let's add a new message type that the `ChartingActor` is going to use - add this inside the `Messages` region.

```csharp
// Actors/ChartingActor.cs - inside the Messages region

/// <summary>
/// Remove an existing <see cref="Series"/> from the chart
/// </summary>
public class RemoveSeries
{
    public RemoveSeries(string seriesName)
    {
        SeriesName = seriesName;
    }

    public string SeriesName { get; private set; }
}
```

Add the following method to the bottom of the `ChartingActor` class:

```csharp
// Actors/ChartingActor.cs

private void SetChartBoundaries()
{
    double maxAxisX, maxAxisY, minAxisX, minAxisY = 0.0d;
    var allPoints = _seriesIndex.Values.Aggregate(new HashSet<DataPoint>(), 
			(set, series) => new HashSet<DataPoint>(set.Concat(series.Points)));
    var yValues = allPoints.Aggregate(new List<double>(), (list, point) => list.Concat(point.YValues).ToList());
    maxAxisX = xPosCounter;
    minAxisX = xPosCounter - MaxPoints;
    maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
    minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;
    if (allPoints.Count > 2)
    {
        var area = _chart.ChartAreas[0];
        area.AxisX.Minimum = minAxisX;
        area.AxisX.Maximum = maxAxisX;
        area.AxisY.Minimum = minAxisY;
        area.AxisY.Maximum = maxAxisY;
    }
}
```

> **NOTE**: the `SetChartBoundaries()` method is used to make sure that the boundary area of our chart gets updated as we remove old points from the beginning of the chart as time elapses.

Next, we're going to redefine all of our message handlers to use the new `SetChartBoundaries()` method. 

**Delete everything inside the previous `Individual Message Type Handlers` region** and **replace with the following**:

```csharp
// Actors/ChartingActor.cs - inside the Individual Message Type Handlers region
private void HandleInitialize(InitializeChart ic)
{
    if (ic.InitialSeries != null)
    {
        //swap the two series out
        _seriesIndex = ic.InitialSeries;
    }

    //delete any existing series
    _chart.Series.Clear();

    //set the axes up
    var area = _chart.ChartAreas[0];
    area.AxisX.IntervalType = DateTimeIntervalType.Number;
    area.AxisY.IntervalType = DateTimeIntervalType.Number;

    SetChartBoundaries();

    //attempt to render the initial chart
    if (_seriesIndex.Any())
    {
        foreach (var series in _seriesIndex)
        {
            //force both the chart and the internal index to use the same names
            series.Value.Name = series.Key;
            _chart.Series.Add(series.Value);
        }
    }

    SetChartBoundaries();
}

private void HandleAddSeries(AddSeries series)
{
    if(!string.IsNullOrEmpty(series.Series.Name) && !_seriesIndex.ContainsKey(series.Series.Name))
    {
        _seriesIndex.Add(series.Series.Name, series.Series);
        _chart.Series.Add(series.Series);
        SetChartBoundaries();
    }
}

private void HandleRemoveSeries(RemoveSeries series)
{
    if (!string.IsNullOrEmpty(series.SeriesName) && _seriesIndex.ContainsKey(series.SeriesName))
    {
        var seriesToRemove = _seriesIndex[series.SeriesName];
        _seriesIndex.Remove(series.SeriesName);
        _chart.Series.Remove(seriesToRemove);
        SetChartBoundaries();
    }
}

private void HandleMetrics(Metric metric)
{
    if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
    {
        var series = _seriesIndex[metric.Series];
        series.Points.AddXY(xPosCounter++, metric.CounterValue);
        while(series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
        SetChartBoundaries();
    }
}
```

And finally, we need to add a couple of `Receive<T>` handlers to the constructor for `ChartingActor`:

```csharp
// Actors/ChartingActor.cs - add these below the original Receive<T> handlers in the constructor
Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
Receive<Metric>(metric => HandleMetrics(metric));
```

### Phase 8 - Replace the `Main_Load` Handler in `Main.cs`
Now that we have real data we want to plot in real-time, we need to replace the original `Main_Load` event handler, which supplied fake data to our `ChartActor` with a real one that sets us up for live charting.

First things first, you need to add the following declarations to the top of the `Main` class inside `Main.cs`:

```csharp
// Main.cs - at top of Main class
private ActorRef _coordinatorActor;
private Dictionary<CounterType, ActorRef> _toggleActors = new Dictionary<CounterType, ActorRef>();
```

Then we can replace the `Main_Load` event handler.

```csharp
// Main.cs - replace Main_Load event handler
private void Main_Load(object sender, EventArgs e)
{
    _chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart)), "charting");
    _chartActor.Tell(new ChartingActor.InitializeChart(null)); //no initial series

    _coordinatorActor = Program.ChartActors.ActorOf(Props.Create(() => 
			new PerformanceCounterCoordinatorActor(_chartActor)), "counters");

    //CPU button toggle actor
    _toggleActors[CounterType.Cpu] = Program.ChartActors.ActorOf(
        Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnCpu, CounterType.Cpu, false))
            .WithDispatcher("akka.actor.synchronized-dispatcher"));

    //MEMORY button toggle actor
    _toggleActors[CounterType.Memory] = Program.ChartActors.ActorOf(
       Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnMemory, CounterType.Memory, false))
           .WithDispatcher("akka.actor.synchronized-dispatcher"));

    //DISK button toggle actor
    _toggleActors[CounterType.Disk] = Program.ChartActors.ActorOf(
       Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnDisk, CounterType.Disk, false))
           .WithDispatcher("akka.actor.synchronized-dispatcher"));

    //Set the CPU toggle to ON so we start getting some data
    _toggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
}
```

#### Wait a minute, what's this `WithDispatcher` nonsense?!
`Props` has a built-in fluent interface which allows you to configure your actor deployments programmatically, and in this instance we decided to use the `Props.WithDispatcher` method to guarantee that each of the `ButtonToggleActor` instances run on the UI thread.

> **NOTE**: If an actor has both a programmatic configuration declared via `Props`' fluent interface and a deployment configuration declared inside the `Config` used by the `ActorSystem`, the `Config` object will always override the programmatic configuration.

### Phase 9 - Have Button Handlers Send `Toggle` Messages to Corresponding `ButtonToggleActor`

**LAST STEP.** We need to wire up the button handlers we created in Phase 3.

Here's what the code should look like:

```csharp
// Main.cs - button handlers added in phase 3
private void btnCpu_Click(object sender, EventArgs e)
{
    _toggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
}

private void btnMemory_Click(object sender, EventArgs e)
{
    _toggleActors[CounterType.Memory].Tell(new ButtonToggleActor.Toggle());
}

private void btnDisk_Click(object sender, EventArgs e)
{
    _toggleActors[CounterType.Disk].Tell(new ButtonToggleActor.Toggle());
}
```

Make sure that your `ButtonToggleActor`s correspond to their respective buttons!

### Once you're done
Build and run `SystemCharting.sln` and you should see the following:

![Successful Lesson 3 Output](images/dothis-successful-run3.gif)

Compare your code to the code in the [/Completed/ folder](Completed/) to compare your final output to what the instructors produced.

## Great job!
*Phew*. That was *a lot* of code.

Every other lesson builds on this one, so make sure your code matches the output of the [/Completed/ folder](Completed/).

After this lesson you should understand how the `Scheduler` works and how you use it alongside patterns like Pub-sub with actors. 

**Let's move onto [Lesson 4 - Switching Actor Behavior at Run-time with `Become` and `Unbecome`](../lesson4).**