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

### Once you're done
Compare your code to the code in the /Completed/ folder to see what the instructors included in their samples.

## Great job!
Awesome work! Well done on completing your first lesson.
