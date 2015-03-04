# Lesson 3.1: Using `Group` routers to divide work among your actors
Welcome to Unit 3! As you know, this unit focuses on making our actor systems more scalable, resilient, and parallelized.

Let's start with a real world example: imagine that you had a system that needed to process events on a per-user level. Maybe you have a system that has to track analytics and events across tens of thousands of app installs across Android and iOS. How could you do that?

The first concept we need to understand to create a system like that possible is `Router`s ([docs](http://getakka.net/wiki/Routing)). Let's get to it.

## Key Concepts / Background
### `Router`s
#### What is a `Router`?
A `Router` is a special kind of actor that acts as a messaging hub to a group of other actors. The purpose of a `Router` is to divide and balance work (represented as message streams) across some group of other actors, which will actually do the work.

#### What's special about a `Router`?
A `Router` actually *is* an actor, but with one critical difference: it can process more than one message at a time.

**Wait, what? I thought all actors only processed one message at a time?!**
That is true for 99.999% of cases. But remember: *The purpose of a `Router` is to route messages onward, NOT to process them.*

Since a `Router` does not need to actually process messages and take any significant action on the message—it just has to forward the message on to another actor—it can break the "one message at a time" rule and everything is fine. In fact, it's better that way. Why?

#### What do `Router`s give me?
Fundamentally, what `Router`s give you is massive message throughput. `Router`s are the critical component that breaks down large event streams into easily managed chunks of code. `Router`s allow you to divide up a huge amount of work across a group of actors.

To illustrate, let's go back to our example system that needs to track per-user metrics. The simplistic design solution for this is to have many per-user actors that actually compute the stats for a given user, with one stats actor per user. Then, we can have our event stream flow through a router that routes the events to the stats actor responsible for user who performed that event (for example, routing based on the UUID of the user who generated the event).

While this sounds simple, it's actually highly scalable because actors are so cheap. One router can easily handle millions of messages coming through it and route them on to the actor that is aggregating stats for that user.

[add diagram of routers system for stats based on type ]

#### How does a `Router` get its throughput?
On the surface, `Router`s look like normal actors, but they are actually implemented differently. Routers are designed to be extremely efficient at one thing: receiving messages and passing them quickly on to routees.

A normal actor *can* be used for routing messages, but a normal actor's single-threaded processing can become a bottleneck. Routers can achieve much higher throughput by changing the usual message-processing pipeline to allow concurrent message routing. This is achieved by embedding the routing logic of a `Router` directly in their `ActorRef` itself, rather than in the receive loop / message-handling code of the router actor. This way, messages sent to a router's `ActorRef` are immediately routed to the routee, bypassing the single-threaded router actor message-handling code entirely.

The cost to this is, of course, that the internals of routing code are more complicated than if routers were implemented with normal actors. Fortunately all of this complexity is invisible to consumers of the routing API. However, it is something to be aware of when implementing your own routers.

#### What kinds of `Router`s are there?
There are two types of `Router`s: group routers, and pool routers.

##### Group Routers
Group routers are the simpler of the two. A group router is a router that does not create/manage its routees, it only forwards them messages. You specify the routees when creating the group router by passing the router the `ActorPath`s for each routee.

##### Pool Routers
A "Pool" router is a `Router` that creates and manages its worker actors ("routees"). You provide a `NrOfInstances` to the router and the router will handle routee creation (and supervision) by itself.

#### What's the difference between Group and Pool `Router`s?
*The key difference is that pool routers create and manage their routees, whereas group routers do not.*

Other than that, you should know that group routers send messages to their routees via an `ActorSelection` while pool routers send messages to their routees via an `ActorRef`. This is because group routers are given their routees rather than creating them.

#### How do I configure a `Router`?
In short, with HOCON. We'll be covering this in depth in Lesson 3.3.

#### How does a `Router` know where to forward a message?
routing strategy

### Routing Strategies
#### What is a Routing Strategy?
#### What are the routing strategies available? how do they each work?
    + wont cover all here as we'll spread over the course of the unit
    + broadcast
    + roundrobin
    + Consistent hash

- can i message all routees at once? (`Broadcast` message)


### When should I use each kind of router?
+ do i ever need to configure my own routers?

### `Router`s & Supervision
How routees are supervised depends on whether you're using a group or pool router.

#### How does supervision work with Group routers?
*A group router does not supervise its routees.*

Recall that group routers do not create their routees, but instead are passed the `ActorPath`s of their routees. This means that those routees exist somewhere else in the hierarchy, and are managed by whatever other parent actors created them.

Practically, this means that a group router may not always know that its routees have died. A group router will attempt to [`DeathWatch`](http://getakka.net/wiki/Supervision#what-lifecycle-monitoring-means) its routees, but it doesn't always succeed in subscribing.

[diagram of the hierarchy diff where router has routee far away]

#### How does supervision work with Pool routers?
*A pool router supervises its routees.*

Recall that a pool router creates its routees as direct child actors of the router. This means that a pool router automatically supervises (and `DeathWatch`es) its routees.

Also recall that `Router`s are just a special type of actor. Since `Router`s are actors, they have a [`SupervisionStrategy`](https://github.com/petabridge/akka-bootcamp/tree/master/src/Unit-1/lesson4#supervision-directives) and can help their children (routees) recover from errors.

The supervision strategy of the router actor can be configured with the `supervisorStrategy` property of the Pool. If no configuration is provided, routers default to a strategy of “always escalate”. This means that errors are passed up to the router's supervisor for resolution.

In this case, the router's supervising actor will treat the error as an error with the router itself. Therefore, a stop/restart directive issued would cause the router itself to stop/restart. The router, in turn, will cause its child routees to stop/restart (but it will maintain the number of routees in the pool).

##### What this means in practice
*This is about default behavior in the case of an error.*

Here's what will happen, by default, if you haven't specified a `supervisorStrategy` on the pool router:

1. A failure in a routee will bubble up to the parent of the router
2. The parent of the router will issue a `restart` directive to the router
3. The router will restart itself, and then restart its child routees

The reason is to make the default behavior such that adding `withRouter` to a child actor definition does not change the supervision strategy applied to the child. Of course, you can change this by specifying the strategy when defining the parent router.

[diagram of the hierarchy diff where router has routees as children]

#### Isn't it bad that group routers usually don't know their routees have died?

## Exercise

### Once you're done

## Great job!
