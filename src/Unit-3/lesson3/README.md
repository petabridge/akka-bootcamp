# Lesson 3.3: How to use HOCON to configure your routers
Awesome, look at you go! By now, you understand the massive increases in throughput that routers can give you, and what the different types of routers are.

Now we need to show you how to configure and deploy them.

## Key Concepts / Background
### HOCON for `Router`s
#### Quick review of HOCON
We first learned about HOCON in [Lesson 2.1](../../Unit-2/lesson1/README.md).

To review, [HOCON (Human-Optimized Config Object Notation)](https://getakka.net/articles/concepts/configuration.html#what-is-hocon) is a flexible and extensible configuration format. It will allow you to configure everything from Akka.NET's `IActorRefProvider` implementation, logging, network transports, and more commonly - how individual actors are deployed.

It's this last feature that we'll be using here to configure how our router actors are deployed. An actor is "deployed" when it is instantiated and put into service within the `ActorSystem` somewhere.

#### Why use HOCON to configure the routers?
There are three key reasons that we prefer using HOCON to configure our routers.

First, using HOCON keeps your code cleaner. By using HOCON you can keep configuration details out of your application code and keep a nice separation of concerns there. Helps readability a lot, too.

Second, like any actor, a router can be remotely deployed into another process. So if you want to remotely deploy a router (which you will), using HOCON makes that easier.

But most importantly, ***using HOCON means that you can change the behavior of actors dramatically without having to actually touch the actor code itself, just by changing config settings.***

#### What configuration flags are usually specified?
What specific flags you need to specify will depend on the type of router you're using (e.g. you will need a `duration` with a `ScatterGatherFirstCompletedRouter`), but here are the things you'll be configuring the most.

##### Type of `Router`
The most common thing you'll specify is what the type of router is.

Here are the mappings between ´deployment.router' short names to fully qualified class names. You'll use these short names in `App.config`:

```xml
router.type-mapping {
  from-code = "Akka.Routing.NoRouter"
  round-robin-pool = "Akka.Routing.RoundRobinPool"
  round-robin-group = "Akka.Routing.RoundRobinGroup"
  random-pool = "Akka.Routing.RandomPool"
  random-group = "Akka.Routing.RandomGroup"
  balancing-pool = "Akka.Routing.BalancingPool"
  smallest-mailbox-pool = "Akka.Routing.SmallestMailboxPool"
  broadcast-pool = "Akka.Routing.BroadcastPool"
  broadcast-group = "Akka.Routing.BroadcastGroup"
  scatter-gather-pool = "Akka.Routing.ScatterGatherFirstCompletedPool"
  scatter-gather-group = "Akka.Routing.ScatterGatherFirstCompletedGroup"
  consistent-hashing-pool = "Akka.Routing.ConsistentHashingPool"
  consistent-hashing-group = "Akka.Routing.ConsistentHashingGroup"
}
```

##### Number of routees
The second most common flag you'll specify in HOCON is the number of routee instances to place under the router.

You do this with the `nr-of-instances` flag, like so:

```xml
akka {
	actor{
	  deployment{
	   /myRouter{
	      router = broadcast-pool
	      nr-of-instances = 3
	    }
	  }
   }
}
```

##### Resizer
To use a `ResizablePoolRouter` ("auto scaling router"), a `Resizer` component is required. This is the component that does the monitoring of routee mailbox load and compares that to the thresholds it has calculated.

Out of the box, there is only the default `Resizer`. You can configure your own if you want, but be forewarned, it's complicated. Which `Resizer` to use is commonly specified in HOCON, like so:

```xml
akka.actor.deployment {
/router1 {
    router = round-robin-pool
        resizer {
            enabled = on
            lower-bound = 2
            upper-bound = 3
        }
    }
}
```

#### What should I specify procedurally vs with HOCON?
The only thing we can think of that MUST be configured procedurally is the `HashMap` function given to a `ConsistentHashRouter`.

Everything else we can think of can be configured either way, but we prefer to do all our configuration via HOCON.

#### Which configuration wins: procedural, or HOCON?
HOCON wins. This is true for all actors, not just routers.

For example, if you procedurally specify config for a router and also configure the router in `App.config`, then the values specified in HOCON win.

Suppose you defined the following router via configuration:

```xml
/router1 {
    router = consistent-hashing-pool
    nr-of-instances = 3
    virtual-nodes-factor = 17
}
```

But when it came time to create router1, you gave `Props` the following procedural router definition:

```csharp
var router1 = MyActorSystem.ActorOf(Props.Create(() =>
  new FooActor()).WithRouter(new RoundRobinPool(10)), "router1");
```

You'd still get a `ConsistentHashingPool` with 3 instances of `FooActor` instead of a `RoundRobinPool` with 10 instances of `FooActor`.

#### Forcing Akka.NET to load router definitions from configuration using `FromConfig`
Akka.NET won't create an actor with a router unless you explicitly call `WithRouter` during the time you create an actor who needs to be a router.

So, if we want to rely on the router definition we've supplied via configuration, we can use the `FromConfig` class to tell Akka.NET "hey, look inside our configuration for a router specification for this actor."

Here's an example:

```xml
/router1 {
    router = consistent-hashing-pool
    nr-of-instances = 3
    virtual-nodes-factor = 17
}
```

And if we make the following call to `ActorOf`:

```csharp
var router1 = MyActorSystem.ActorOf(Props.Create(() =>
  new FooActor()).WithRouter(FromConfig.Instance), "router1");
```

Then we'll get a `ConsistentHashingPool` router.

Otherwise, if we just called this:

```csharp
var router1 = MyActorSystem.ActorOf(Props.Create(() => new FooActor()), "router1");
```

Then we'd only get a single instance of `FooActor` in return. Use `FromConfig` whenever you need to use a router defined in configuration.

### `Ask`
Bonus concept! We're also going to teach you to use `Ask` in addition to HOCON.

#### What is `Ask`?
`Ask` is how one actor can ask another actor for some information and wait for a reply.

#### When do I use `Ask`?
Whenever you want one actor to retrieve information from another and wait for a response. It isn't used that often—certainly not compared to `Tell()`—but there are places where it is ***exactly*** what you need.

Great! Let's put `Ask` and HOCON to work with our routers!

## Exercise
We're not going to change the actor hierarchy much in this lesson, but we are going to replace the programmaticly defined `BroadcastGroup` router we created in lesson 1 with a `BroadcastPool` defined via HOCON configuration in `App.config`.

### Phase 1 - Add new deployment to `App.config`
We'll add our configuration section first, before we modify the code inside `GithubCommanderActor`.

Open `App.config` and add the following inside the `akka.actor.deployment` section:

```xml
<!-- inside App.config, in the akka.actor.deployment 
      section with all of the other HOCON -->
<!-- you can add this immediately after the 
      /authenticator deployment specification -->
/commander/coordinator{
  router = broadcast-pool
  nr-of-instances = 3
}
```

### Phase 2 - Modify `GithubCommanderActor` to use this new configuration setting
Open up `Actors/GithubCommanderActor.cs` and do the following:

Add the following import to the top of the file:

```csharp
// add to the top of Actors/GithubCommanderActor.cs
using System.Linq;
```
Replace the `GithubCommanderActor`'s `BecomeAsking method to look like this:

```csharp
// GithubCommanderActor's BecomeAsking method - replace it with this
private void BecomeAsking()
{
    _canAcceptJobSender = Sender;
    // block, but ask the router for the number of routees. Avoids magic numbers.
    pendingJobReplies = _coordinator.Ask<Routees>(new GetRoutees())
      .Result.Members.Count();
    Become(Asking);
}
```

Since the number of routees underneath `_coordinator` is now defined via configuration, we're going to `Ask<T>` the router using a built-in `GetRoutees` message to determine how many replies we need (1 per routee) before we can accept a new job. This is a special message that tells the router to return the full list of all of its current `Routees` back to the sender.

`Ask` is usually an asynchronous operation, but in this case we're going to block and wait for the result - because the `GithubCommander` can't execute its next behavior until it knows how many parallel jobs can be run at once, which is determined by the number of routees.

> **NOTE: Blocking is not evil**. In the wake of `async` / `await`, many .NET developers have come to the conclusion that blocking is an anti-pattern or generally evil. This is ludicrous. It depends entirely on the context. Blocking is absolutely the right thing to do if your application can't proceed until the operation you're waiting on finishes, and that's the case here.

Finally, replace the `GithubCommanderActor`'s `PreStart` method with the following:

```csharp
// replace GithubCommanderActor's PreStart method with this
protected override void PreStart()
{
    // create a broadcast router who will ask all 
    // of them if they're available for work
    _coordinator =
        Context.ActorOf(Props.Create(() => new GithubCoordinatorActor())
          .WithRouter(FromConfig.Instance),
          ActorPaths.GithubCoordinatorActor.Name);
    base.PreStart();
}
```

And that's it!

### Once you're done
Build and run `GithubActors.sln` - you'll notice now that everything runs the same as it was before, *but* if you modify the `nr-of-instances` value in the deployment configuration for `/commander/coordinator` then it will directly control the number of parallel jobs you can run at once.

Effectively you've just made the number of concurrent jobs `GithubActors.sln` can run at once a configuration detail - cool!

## Great job!
We've been able to leverage routers for parallelism both via explicit programmatic deployments and via configuration.

**And now it's time to achieve maximum parallelism using the TPL in the next lesson: [Lesson 4 - How to perform work asynchronously inside your actors using `PipeTo`](../lesson4/README.md)**

## Any questions?
Come ask any questions you have, big or small, [in this ongoing Bootcamp chat with the Petabridge & Akka.NET teams](https://gitter.im/petabridge/akka-bootcamp).

### Problems with the code?
If there is a problem with the code running, or something else that needs to be fixed in this lesson, please [create an issue](https://github.com/petabridge/akka-bootcamp/issues) and we'll get right on it. This will benefit everyone going through Bootcamp.
