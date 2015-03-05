# Lesson 3.2: Using `Pool` routers to automatically create and manage pools of actors

In this lesson, we're going to build on the foundation we laid in the last lesson around routers and introduce you to a more powerful, flexible kind of router.

## Key Concepts / Background
### What is a Pool router?
A "Pool" router is a `Router` that creates and manages its worker actors ("routees"). You provide a `NrOfInstances` to the router and the router will handle routee creation (and supervision) for you.

#### What's the difference between Group and Pool `Router`s?
*The key difference is that pool routers create and manage their routees, whereas group routers do not.*

Pool routers are safer to use than group routers. As touched on at the end of Lesson 3.1, group routers usually don't know when their routees are no longer available. This makes them less dependable and not ideal for common routing use.

This is because group routers are given the `ActorPath`s of their routees and in turn, communicate with their routees by sending messages to `ActorSelection`s. In contrast, because a pool router creates its routees, it is their parent, communicates directly with the `ActorRef` of a routee, and knows much more information about routees.

Additionally, pool routers can grow/shrink their routee pool whereas the routee pool for a group router is fixed once set. Also be aware that pool routers don't let you control the names of its routee children, so you have to talk to those routees via the router.

### When should I use Group routers vs Pool routers?
***You should use a pool router if you can.***

We recommend using a pool router unless you have a unique situation that falls into one of these categories:

1. You need your router to be able to send messages to a group of actors via wildcard `ActorSelection`.
2. For some reason, you need to route messages to actors that the router cannot be responsible for. For example, there could be a situation where you need to front some actors with a router, but it just doesn't make sense to have those routees as children of the router. Perhaps if other critical components in your system depend on these actors living at a certain location.

Frankly, we haven't come across many good use cases that call for a group router. You'll probably know it when you see it.

***Our advice: stick with pool routers unless you have a very good reason to use a group router.***

### Supervision & Pool Routers
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

## Exercise

### Once you're done

## Great job!
