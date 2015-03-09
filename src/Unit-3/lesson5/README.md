# Lesson 3.5: How to prevent deadlocks with `ReceiveTimeout`
Wow, look at you! Here we are on our last lesson of Bootcamp together. We want to say thank you for coming on this journey with us, and to give yourself a big pat on the back for your dedication to your craft.

In this lesson, we'll be going over how to handle timeouts within actors and prevent deadlocks, where one actor is waiting for another indefinitely.

This lesson will show you how to prevent deadlocks by using a `RecieveTimeout`.

## Key Concepts / Background
### What is `ReceiveTimeout`?
`ReceiveTimeout` lets you specify what an actor should do when it hasn't received a message for a certain period of time. Once this timeout has been hit, the actor will send itself the `ReceiveTimeout` singleton as a message.

Once set up, the `ReceiveTimeout` stays in effect and will continue firing repeatedly every time the specified interval passes without the actor receiving a message.

### When do I use `ReceiveTimeout`?
You can use `ReceiveTimeout` whenever you want to take some action after a period of inactivity.

Here are some common cases where you may want to use a `ReceiveTimeout`:

- To shut an actor down after it goes a certain amount of time without receiving a message
- To confirm that other actors are doing work and sending in their status messages
- To prevent deadlocks where one actor thinks another is doing work

### How do I set up a `ReceiveTimeout`?
You call `Context.SetReceiveTimeout()` and pass it a `TimeSpan`. If that amount of time passes and the actor hasn't received a message, the actor will send itself the `ReceiveTimeout` singleton as a message, e.g.

```csharp
// send ourselves a ReceiveTimeout message if no message within 3 seconds
Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
```

Then, you just need to handle the `ReceiveTimeout` message and take whatever action is appropriate.

Let's assume you wanted to shut an actor down after a period of inactivity, and inform its parent. Here's one basic way you could do that:

```csharp
// have actor shut down after a long period of inactivity
Receive<ReceiveTimeout>(timeout =>
{
    // inform parent that shutting down
    Context.Parent.Tell(new ImShuttingDown());
    // shut self down
    Context.Stop(Self);
});
```

### How do I cancel a `ReceiveTimeout` that I've set?
You call `Context.SetReceiveTimeout()` and pass it `null`, e.g.

```csharp
// cancel ReceiveTimeout
Context.SetReceiveTimeout(null);
```

### Can I change the timeout value?
Yes, you can set the `ReceiveTimeout` after every message, or as often as you want. Setting a new timeout will cancel the previous timeout and schedule a new one.

### What's the smallest `ReceiveTimeout` interval I can specify?
1 millisecond is the minimum timeout interval.

### Does `ReceiveTimeout` work with all actor types?
Yes, you can use `ReceiveTimeout` with any actor type.

The only difference would be the syntax differences between how you match the message between a `ReceiveActor` and an `UntypedActor`.

### What if another message comes in right before I process the timeout?
`ReceiveTimeout` can create false positives. For example, it's possible for the timeout to occur and another message to arrive in the actors mailbox before the `ReceiveTimeout` message does. In this case, another message would get processed before the `ReceiveTimeout` message, making it invalid.

It is not *guaranteed* that upon reception of the `ReceiveTimeout` that there must have been an idle period beforehand as configured via this method.

This is an edge case, but there are ways to code around it.

## Exercise

### Once you're done

## Great job!
Wow! You made it, awesome!

We're really proud of you, and want to express our gratitude for sticking with us all the way through. Thank you, and kudos to you. Your dedication to your craft inspires us.

## Sharing is caring: [click here to Tweet about Bootcamp!](http://ctt.ec/L_Xe0)

We want to help more people get this knowledge and learn to use Akka.NET. Direct them to the [Bootcamp information page](http://learnakka.net) or to this repo.

If we at Petabridge can be of any help to you whatsoever, [please reach out to us by email](mailto:hi@petabridge.com) or say hello <a href="https://twitter.com/petabridge">on Twitter</a>.

### Want to level up your company or team with Akka.NET?
[Please email us](mailto:hi@petabridge.com) to discuss your situation.

We work with companies all the time to **implement production systems and do advanced Akka.NET training** (Clustering, Remoting, Testing, DevOps, best practices, etc).

We'd love to help you, too.

Gratefully,<br>
Aaron & Andrew<br>
Petabridge co-founders

## Any questions?
**Don't be afraid to ask questions** :).

Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.

