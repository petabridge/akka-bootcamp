# Lesson 2.4: Switching Actor Behavior at Run-time with `Become` and `Unbecome`

In this lesson we're going to learn about one the really cool things actors can do - [change their behavior at run-time](http://getakka.net/wiki/Working%20with%20actors#hotswap "Akka.NET - Actor behavior hotswap")! Woah!

This capability allows you to do all sorts of cool stuff, like build [Finite State Machines](http://en.wikipedia.org/wiki/Finite-state_machine) or change how your actors handle messages based on other messages they've received!

## Key Concepts / background

Let's start with a real-world scenario for when you might want to change an actor's behavior...

### Real-World Scenario: Authentication

Imagine you're building a simple chat system using Akka.NET actors, and here's what your `UserActor` looks like - this is the actor that is responsible for all communication to and from a specific human user.

```csharp
public class UserActor : ReceiveActor{
	private readonly string _userId;
	private readonly string _chatRoomId;

	public UserActor(string userId, string chatRoomId){
		_userId = userId;
		_chatRoomId = chatRoomId;
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// print message for user
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// send message to chatroom				
			});
	}
}
```

So we have basic chat working - yay! But... There's nothing to guarantee that this user is who they say they are. 

How could we rewrite this actor to handle these same types of messages differently when:

* The user is **authenticating**;
* The user is **authenticated** (yay!); or
* The user **couldn't authenticate** (doh!) ?

Simple! We can use [switchable actor behaviors](http://getakka.net/wiki/Working%20with%20actors#hotswap "Akka.NET - switchable actor behavior") to do this!

### Switching Message-Handling Behaviors

Here's how we might implement switchable message behavior in our `UserActor` from the previous snippet:

```csharp
public class UserActor : ReceiveActor{
	private readonly string _userId;
	private readonly string _chatRoomId;

	public UserActor(string userId, string chatRoomId){
		_userId = userId;
		_chatRoomId = chatRoomId;
		
		//start with the Authenticating behavior
		Authenticating();
	}

	protected override void PreStart(){
		//start the authentication process for this user
		Context.ActorSelection("/user/authenticator/")
			.Tell(new AuthenticatePlease(_userId));
	}

	private void Authenticating(){
		Receive<AuthenticationSuccess>(auth => {
			Become(Authenticated); //switch behavior to Authenticated
		});
		Receive<AuthenticationFailure>(auth => {
			Become(Unauthenticated); //switch behavior to Unauthenticated
		});
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// can't accept message yet - not auth'd
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// can't send message yet - not auth'd			
			});
	}

	private void Unauthenticated(){
		//switch to Authenticating
		Receive<RetryAuthentication>(retry => Become(Authenticating));
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// have to reject message - auth failed
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// have to reject message - auth failed	
			});
	}

	private void Authenticated(){
		Receive<IncomingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// print message for user
			});
		Receive<OutgoingMessage>(inc => inc.ChatRoomId == _chatRoomId,
			inc => { 
				// send message to chatroom				
			});
	}
}
```

Woah! What's all this stuff? We took the `Receive<T>` handlers we defined on our receive Actor and put them into three separate methods:

* `Authenticating()` - the default behavior we use for when the user is attempting to authenticate;
* `Authenticated()` - when the authentication operation is successful; and
* `Unauthenticated()` - when the authentication operation is **not** successful.

We called `Authenticating()` from the constructor, which meant that all of `UserActor`'s  `Receive<T>` handlers would be only defined by what's in the `Authenticating()` method.

However, whenever we receive a message of type `AuthenticationSuccess` or `AuthenticationFailure` we  use the `Become` method ([docs](http://getakka.net/wiki/ReceiveActor#become "Akka.NET - ReceiveActor Become")) to switch behaviors to `Authenicated` or `Unauthenticated` respectively. 

What's going on there?

### The Behavior Stack

Akka.NET actors have the concept of a "behavior stack":

![Initial Behavior Stack for UserActor](images/behaviorstack-initialization.png)

Whichever method sits at the top of the behavior stack defines the actor's current behavior. 

> The current behavior of an actor dictates which `Receive` methods will be used to process any messages delivered to an actor.

Whenever we call `Become`, we tell the `ReceiveActor` to push a new behavior onto the stack:

![Become Authenticated - push a new behavior onto the stack](images/behaviorstack-become.gif)

And whenever we call `Unbecome`, we pop our current behavior off of the stack and replace it with the previous behavior from before:

![Unbecome - pop the current behavior off of the stack](images/behaviorstack-unbecome.gif)

> NOTE: By default, `Become` will delete the old behavior off of the stack - so the stack will never have more than one behavior in it at a time. This is because most Akka.NET users don't use `Unbecome`.
> 
> To preserve the previous behavior on the stack, call `Become(Method(), false)`

We can safely switch actor message-processing behavior because [Akka.NET actors only process one message at a time](http://petabridge.com/blog/akkadotnet-async-actors-using-pipeto/). So the new message processing behavior doesn't get applied until the next message arrives.

How deep can the behavior stack go? *Really* deep, but not to an unlimited extent. And each time your actor restarts the behavior stack is cleared and you start from scratch.

And what happens if you call `Unbecome` and there's nothing left in the behavior stack? The answer is: *nothing* - `Unbecome` is a safe method and won't do anything unless there's more than one behavior in the stack.

### Switchable Behaviors Also Work for `UntypedActor`

`Become` looks slightly different for `UntypedActor` instances:

```csharp
public class MyActor : UntypedActor{
	protected override void OnReceive(object message){
		if(message is SwitchMe){
			//preserve the previous behavior on the stack
			Context.Become(OtherBehavior, false);
		}
	}

	private void OtherBehavior(object message){
		if(message is SwitchMeBack){
			//switch back to previous behavior on the stack
			Context.Unbecome();
		}
	}
}
```

To switch behaviors in an `UntypedActor`, you have to use the following methods:

* `Context.Become(Receive rec, bool discardPrevious = true)` - pushes a new behavior on the stack or
* `Context.Unbecome()` - pops the current behavior and switches to the previous (if applicable.)

`Context.Become` takes a `Receive` delegate, which is really any method with the following signature:

```csharp
void MethodName(object someParameterName);
```

Aside from those syntactical differences, behavior switching works exactly the same way across both `UntypedActor` and `ReceiveActor`.

Now let's put behavior switching to work for us!

## Exercise
In this lesson we're going to add the ability to pause and resume live updates to the `ChartingActor` via switchable actor behaviors.

### Phase 1 - Add a New `Pause / Resume` Button to `Main.cs`

This is the last button you'll have to add, we promise.

Go to the **[Design]** view of `Main.cs` and add a new button with the following text: `PAUSE ||`

![Add a Pause / Resume Button to Main](images/design-pauseresume-button.png)

Got to the **Properties** window in Visual Studio and change the name of this button to `btnPauseResume`.

![Use the Properties window to rename the button to btnPauseResume](images/pauseresume-properties.png)

Double click on the `btnPauseResume` to add a click handler to `Main.cs`.

```csharp
private void btnPauseResume_Click(object sender, EventArgs e)
{

}
```

We'll fill this click handler in shortly.

### Phase 2 - Add Switchable Behavior to `ChartingActor`
We're going to add some dynamic behavior to the `ChartingActor` - but first we need to do a little cleanup.

First, add a `using` reference for the Windows Forms namespace at the top of `Actors/ChartingActor.cs`.

```csharp
// Actors/ChartingActor.cs

using System.Windows.Forms;
```

Next we need to declare a new message type inside the `Messages` region of `ChartingActor`.

```csharp
// Actors/ChartingActor.cs - add inside the Messages region
/// <summary>
/// Toggles the pausing between charts
/// </summary>
public class TogglePause { }
```

Next, add the following field declaration just above the `ChartingActor` constructor declarations:

```csharp
// Actors/ChartingActor.cs - just above ChartingActor's constructors

private readonly Button _pauseButton;
```

Move all of the `Receive<T>` declarations from `ChartingActor`'s main constructor into a new method called `Charting()`.

```csharp
// Actors/ChartingActor.cs - just after ChartingActor's constructors
private void Charting()
{
    Receive<InitializeChart>(ic => HandleInitialize(ic));
    Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
    Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
    Receive<Metric>(metric => HandleMetrics(metric));

	//new receive handler for the TogglePause message type
    Receive<TogglePause>(pause =>
    {
        SetPauseButtonText(true);
        Become(Paused, false);
    });
}
```

Add a new method called `HandleMetricsPaused` to the `ChartingActor`'s `Individual Message Type Handlers` region.

```csharp
// Actors/ChartingActor.cs - inside Individual Message Type Handlers region
private void HandleMetricsPaused(Metric metric)
{
    if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
    {
        var series = _seriesIndex[metric.Series];
        series.Points.AddXY(xPosCounter++, 0.0d); //set the Y value to zero when we're paused
        while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
        SetChartBoundaries();
    }
}
```

Define a new method called `SetPauseButtonText` at the *very* bottom of the `ChartingActor` class:

```csharp
// Actors/ChartingActor.cs - add to the very bottom of the ChartingActor class
private void SetPauseButtonText(bool paused)
    {
        _pauseButton.Text = string.Format("{0}", !paused ? "PAUSE ||" : "RESUME ->");
    }
```

Add a new method called `Paused` just after the `Charting` method inside `ChartingActor`:

```csharp
// Actors/ChartingActor.cs - just after the Charting method
private void Paused()
{
    Receive<Metric>(metric => HandleMetricsPaused(metric));
    Receive<TogglePause>(pause =>
    {
        SetPauseButtonText(false);
        Unbecome();
    });
}
```

And finally, let's **replace both of `ChartingActor`'s constructors**:

```csharp
public ChartingActor(Chart chart, Button pauseButton) : this(chart, new Dictionary<string, Series>(), pauseButton)
{
}

public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex, Button pauseButton)
{
    _chart = chart;
    _seriesIndex = seriesIndex;
    _pauseButton = pauseButton;
    Charting();
}

private void Charting()
{
    Receive<InitializeChart>(ic => HandleInitialize(ic));
    Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
    Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
    Receive<Metric>(metric => HandleMetrics(metric));
    Receive<TogglePause>(pause =>
    {
        SetPauseButtonText(true);
        Become(Paused, false);
    });
}
``` 

### Phase 3 - Update the `Main_Load` and `Pause / Resume` Click Handler in Main.cs
Since we changed the constructor arguments for `ChartingActor` in Phase 2, we need to fix this inside our `Main_Load` event handler.

```csharp
//Main.cs - Main_Load event handler
_chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart, btnPauseResume)), "charting");
```

And finally, we need to update our `btnPauseResume` click event handler to have it tell the `ChartingActor` to pause or resume live updates:

```csharp
//Main.cs - btnPauseResume click handler
private void btnPauseResume_Click(object sender, EventArgs e)
{
    _chartActor.Tell(new ChartingActor.TogglePause());
}
```

### Once you're done
Build and run `SystemCharting.sln` and you should see the following:

![Successful Lesson 4 Output](images/dothis-successful-run4.gif)

Compare your code to the code in the [/Completed/ folder](Completed/) to compare your final output to what the instructors produced.

## Great job!
YEAAAAAAAAAAAAAH! We have a live updating chart that we can pause over time!

But wait a minute, what happens if I toggle a chart on or off when the `ChartingActor` is in a paused state?

![Lesson 4 Output BUgs](images/dothis-fail4.gif)

### DOH!!!!!! It doesn't work!

*This is the problem we're going to solve in the next lesson*, using a message `Stash` to defer processing of messages until we're ready.

**Let's move onto [Lesson 5 - Using a `Stash` to Defer Processing of Messages](../lesson5).**
