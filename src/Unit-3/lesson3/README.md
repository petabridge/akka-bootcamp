# Lesson 3.3: How to use HOCON to configure your routers
Awesome, look at you go! By now, you understand the massive increases in throughput that routers can give you, and what the different types of routers are.

Now we need to show you how to configure and deploy them :)

## Key Concepts / Background
### HOCON for `Router`s
#### Quick review of HOCON
We first learned about HOCON in [Lesson 2.1](../../Unit-2/lesson1/).

To review, [HOCON (Human-Optimized Config Object Notation)](http://getakka.net/wiki/HOCON) is a flexible and extensible configuration format. It will allow you to configure everything from Akka.NET's `ActorRefProvider` implementation, logging, network transports, and more commonly - how individual actors are deployed.

It's this last feature that we'll be using here to configure how our router actors are deployed. An actor is "deployed" when it is instantiated and put into service within the `ActorSystem` somewhere.

#### Why use HOCON to configure the routers?
There are three key reasons that we prefer using HOCON to configure our routers.

First, using HOCON keeps your code cleaner. By using HOCON you can keep configuration details out of your application code and keep a nice separation of concerns there. Helps readability a lot, too.

Second, like any actor, a router can be remotely deployed into another process. So if you want to remotely deploy a router (which you will), using HOCON makes that easier.

But most importantly, ***using HOCON means that you can change the behavior of actors dramatically without having to actually touch the actor code itself, just by changing config settings.***

#### What configuration flags usually specified?
What specific flags you need to specify will depend on the type of router you're using (e.g. you will need a `duration` with a `ScatterGatherFirstCompletedRouter`), but here are the things you'll be configuring the most.

##### What type of router is this?
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
// add example here
```

##### Resizer
To use a `ResizablePoolRouter` ("auto scaling router"), a `Resizer` component is required. This is the component that does the monitoring of routee mailbox load and compares that to the thresholds it has calculated.

Out of the box, there is only the default `Resizer`. You can configure your own if you want, but be forewarned, it's complicated. Which `Resizer` to use is commonly specified in HOCON, like so:

```xml
// add example here
```

#### What should I specify procedurally vs with HOCON?
The only thing we can think of that MUST be configured procedurally is the `HashMap` function given to a `ConsistentHashRouter`.

Everything else we can think of can be configured either way, but we prefer to do all our configuration via HOCON.

#### How do I use the HOCON config?
setting up router needs a router config
as long as the router config passed isn't "no router" then it will

#### Which configuration wins: procedural, or HOCON?
HOCON wins. This is true for all actors, not just routers.

For example, if you procedurally specify config for a router and also configure the router in `App.config`, then the values specified in HOCON win.

// add example

### `Ask`
Bonus concept! We're also going to teach you to use `Ask` in addition to HOCON.

#### What is `Ask`?
`Ask` is how one actor can ask another actor for some information and wait for a reply. `Ask` is a blocking, synchronous operation.

#### When do I use `Ask`?
Whenever you want one actor to retrieve information from another and wait for a response. It isn't used that often—certainly not compared to `Tell()`—but there are places where it is ***exactly*** what you need.

#### `Ask` and `await`




## Exercise

### Once you're done

## Great job!

