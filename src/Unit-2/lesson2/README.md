# Lesson 2.2: Using `ReceiveActor` for Smarter Message Handling

In the first module, you learned how to use the `UntypedActor` ([docs](http://getakka.net/wiki/Working%20with%20actors#untypedactor-api "Akka.NET Untyped Actors")) to build your first actors and handle some simple message types. 

In this lesson we're going to show you how to use the `ReceiveActor` ([docs](http://getakka.net/wiki/ReceiveActor "Akka.NET - ReceiveActor")) to easily handle more sophisticated types of pattern matching and message handling in Akka.NET!

## Key Concepts / background
Actors in Akka.NET depend heavily on the concept of pattern matching - being able to selectively handle messages based on their [.NET Type](https://msdn.microsoft.com/en-us/library/ms173104.aspx) and their values.

In the first module, you learned how to use the `UntypedActor` to handle and receive messages using blocks of code that looked a lot like this:

```csharp
protected override void OnReceive(object message){
	if(message is Foo){
		var foo = message as Foo;
		//do something with foo
	}
	else if(message is Bar){
		var bar = message as Bar;
		//do something with bar
	}
	//.... other matches
	else{
		//couldn't match this message
		Unhandled(message);
	}
}
```

This method of pattern matching in Akka.NET works great for simple matches, but what if your matching needs were a little bit more sophisticated? How would you handle these use cases?

1. Match `message` if it's a `string` and begins with "AkkaDotNet" or
2. Match `message` if it's of type `Foo` and `Foo.Count` is less than 4 and `Foo.Max` is greater than 10?

Hmmm... Well if we tried to do all of that inside an `UntypedActor` we'd end up with something like this:

```csharp
protected override void OnReceive(object message){
	if(message is string 
		&& message.AsInstanceOf<string>()
			.BeginsWith("AkkaDotNet")){
		var str = message as string;
		//do some work with str...
	}
	else if(message is Foo 
			&& message.AsInstanceOf<Foo>().Count < 4
			&& message.AsInstanceOf<Foo>().Max > 10){
		var foo = message as Foo;
		//do something with foo
	}
	//.... other matches
	else{
		//couldn't match this message
		Unhandled(message);
	}
}
```

### *Yuck!*

There has to be a better way of doing this, right? *Yes, there is* - the `ReceiveActor` ([docs](http://getakka.net/wiki/ReceiveActor "Akka.NET - ReceiveActor"))!

### Introducing the `ReceiveActor`

The `ReceiveActor` is built on top of the `UntypedActor` and exposes some capabilities for easily handling more sophisticated pattern matching and message handling.

Here's what that ugly code sample from a few moments ago would look like, rewritten as a `ReceiveActor`:

```csharp
public class FooActor : ReceiveActor
{
    public FooActor()
    {
        Receive<string>(s => s.StartsWith("AkkaDotNet"), s =>
        {
            //handle string
        });

        Receive<Foo>(foo => foo.Count < 4 && foo.Max > 10, foo =>
        {
            //handle foo
        });
    }
}
```

### *Much better*.

So what's the secret sauce that helped us simplify and cleanup all of that pattern matching code from earlier?

> It's the *`Receive<T>` handler*, i.e. `Receive<T>(Predicate<T>, Action<T>)`; 
> 
> A `ReceiveActor` enables developers like you to add an easily configurable layer of strongly typed, compile-time pattern matching to your actors.

### Different Flavors of `Receive<T>` Handlers

So what can these handlers do? Here's a few important flavors to remember:

* `Receive<T>(Action<T> handler)` - executes the message handler only if the message is of type T.
* `Receive<T>(Predicate<T> pred, Action<T> handler)` and `Receive<T>(Action<T> handler, Predicate<T> pred)` - executes the message handler only if the message is of type T **and** the [predicate function](https://msdn.microsoft.com/en-us/library/bfcke1bz.aspx) returns true for this instance of T.
* `Receive(Type type, Action<object> handler)` and `Receive(Type type, Action<object> handler, Predicate<object> pred)` - a non-generic version of the typed + predicate message handlers.
* `ReceiveAny` - a catch-all handler. Accepts all `object` instances.

You can match messages easily based on type, and then use typed predicates to perform additional checks or validations when deciding whether or not your actor can handle a specific message.

### The Order of `Receive<T>` Handlers Matters!

So what happens if we need to handle two overlapping types of messages? For instance:

1. `string` messages that begin with `AkkaDotNetSuccess` and
2. `string` messages that begin with `AkkaDotNet`?

What would happen if our `ReceiveActor` was written like this?

```csharp
public class StringActor : ReceiveActor
{
    public StringActor()
    {
        Receive<string>(s => s.StartsWith("AkkaDotNet"), s =>
        {
            //handle string
        });
		
		//NEVER gets hit
        Receive<string>(s => s.StartsWith("AkkaDotNetSuccess"), s =>
        {
            //handle string
        });
    }
}
```

The answer is that the second handler never gets invoked, and this is because `ReceiveActor` is lazy!


> `ReceiveActor` will handle your message using the *first* matching handler, not the *best* match!

So how do we solve this problem? Simple - `ReceiveActor` [evaluates its handlers for each message in the order in which they were declared](http://getakka.net/wiki/ReceiveActor#handler-priority)! Therefore, we can fix this problem by making sure that the more specific handlers get called first!

```csharp
public class StringActor : ReceiveActor
{
    public StringActor()
    {
		//Works as expected
        Receive<string>(s => s.StartsWith("AkkaDotNetSuccess"), s =>
        {
            //handle string
        });

        Receive<string>(s => s.StartsWith("AkkaDotNet"), s =>
        {
            //handle string
        });
    }
}
```

And with that knowledge in-hand, we can put `ReceiveActor` to work for us.

## Exercise
In this exercise we're going to add the ability to add multiple data series to our chart, and we're going to modify the `ChartingActor` to handle commands to do this.

### Phase 1 - Add a "Add Series" Button to the UI

First thing we're going to do is add a new button called "Add Series" to our form - here's where we put it:

![Adding a 'Add Series' button in Design view in Visual Studio](images/add-series-button.png)

Double click on the button in the **[Design]** view so it automatically adds a click handler for you inside `Main.cs`:

```csharp
// automatically added inside main.cs if you double click on button in designer
private void button1_Click(object sender, EventArgs e)
{

}
```

Leave this blank for now - we'll wire up this button to our `ChartingActor` shortly.

### Phase 2 - Add a `AddSeries` Message Type to the `ChartingActor`

Let's define a new message class for putting additional `Series` on the `Chart` managed by the `ChartingActor` - the `AddSeries` message type.

```csharp
// add this to the Actors/ChartingActor.cs file inside the #Messages region

/// <summary>
/// Add a new <see cref="Series"/> to the chart
/// </summary>
public class AddSeries
{
    public AddSeries(Series series)
    {
        Series = series;
    }

    public Series Series { get; private set; }
}
```

### Phase 3 - Have `ChartingActor` Inherit from `ReceiveActor`

Now for the meaty part - changing the `ChartingActor` from an `UntypedActor` to a `ReceiveActor`.

So let's change the declaration for `ChartingActor`:

```csharp
// Actors/ChartingActor.cs

public class ChartingActor : ReceiveActor 
```

And** delete the current `OnReceive` method for the `ChartingActor`**.

### Phase 4 - Define `Receive<T>` Handlers for `ChartingActor`

Right now our `ChartingActor` can't handle any messages that are sent to it - so let's fix that by defining some `Receive<T>` handlers for the types of messages we want to accept.

First things first, add the following method to the `Individual Message Type Handlers` region of the `ChartingActor`:

```csharp
// Actors/ChartingActor.cs in the ChartingActor class (Individual Message Type Handlers region)

private void HandleAddSeries(AddSeries series)
{
    if(!string.IsNullOrEmpty(series.Series.Name) && !_seriesIndex.ContainsKey(series.Series.Name))
    {
        _seriesIndex.Add(series.Series.Name, series.Series);
        _chart.Series.Add(series.Series);
    }
}
```

And now let's modify the constructor of the `ChartingActor` to set a `Recieve<T>` hook for `InitializeChart` and `AddSeries`.

```csharp
// Actors/ChartingActor.cs in the ChartingActor constructor

public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex)
{
    _chart = chart;
    _seriesIndex = seriesIndex;

    Receive<InitializeChart>(ic => HandleInitialize(ic));
    Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
}
```



> **NOTE**: The other constructor for `ChartingActor`, `ChartingActor(Chart chart)` doesn't need to be modified, as it calls `ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex)` anyway.

And with that, our `ChartingActor` should now be able to receive and process both types of messages easily.

### Phase 5 - Have the Button Clicked Handler for "Add Series" Button Send `ChartingActor` an `AddSeries` Message

Let's go back to the click handler we added for the button in phase 1.

In `Main.cs`, add this code to the body of the click handler:

```csharp
// Main.cs - class Main
private void button1_Click(object sender, EventArgs e)
{
    var series = ChartDataHelper.RandomSeries("FakeSeries" + _seriesCounter.GetAndIncrement());
    _chartActor.Tell(new ChartingActor.AddSeries(series));
}
```

And that should do it!

### Once you're done
Build and run `SystemCharting.sln` and you should see the following:

![Successful Lesson 2 Output](images/dothis-successful-run2.gif)

Compare your code to the code in the [/Completed/ folder](Completed/) to compare your final output to what the instructors produced.

## Great job!
Nice work, again. After having completed this lesson you should have a much better understanding of pattern matching in Akka.NET and an appreciation for how `ReceiveActor` is different than `UntypedActor`.

**Let's move onto [Lesson 3 - Using the `Scheduler` to Send Recurring Messages](../lesson3).**