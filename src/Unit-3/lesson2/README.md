# Lesson 3.2: Using `Pool` routers to automatically create and manage pools of actors

## Key Concepts / Background
### What is a Pool router?

### What is special about a Pool router?

#### What's the difference between Group and Pool `Router`s?
*The key difference is that pool routers create and manage their routees, whereas group routers do not.*

{% comment %}
pool routers can grow/shrink their routee pool
pool router cant control names of the children
{% endcomment %}

Other than that, you should know that group routers send messages to their routees via an `ActorSelection` while pool routers send messages to their routees via an `ActorRef`. This is because group routers are given their routees rather than creating them.

### When should I use Group routers vs Pool routers?
by default, use a pool if you can, pools are safe
group at risk to rsome extent

1) wildcard selection to broadcast to a group
2) group when for some reason, need to route to actors that the router cant be responsible for (e.g. wouldnt work to have the pool router creating those routees). no good use cases here, yuo'll know it when yuo see it. not feasible (maybe other stuff depends on actors being in a specific location)

### Pool router-specific routing strategies
mentioned in the last lesson....

 to automatically load balance messages across the routees.

*only works with pool routers*

The selection is done in this order:

1. Pick any idle routee (not currently processing a message) with an empty mailbox
1. Pick any routee with an empty mailbox
1. Pick the routee with the fewest pending messages in mailbox
1. Pick any remote routee, remote actors are consider lowest priority, since their mailbox size is unknown

Here's what the `SmallestMailbox` `RoutingStrategy` looks like:

![SmallestMailbox RoutingStrategy](images/SmallestMailbox.png)

{% comment %}
- what is a suspended routee? how would a group router know?
- how does a router know how many messages its routees have?
- doesn't checking all this slow things down?
{% endcomment %}

#### 'ResizableRouter'
can only work w/ pool routers
detects pressure on router mailbox and figure out if it needs to expand / collapse
can write own resizer strategy
can define cut/ breach theshold (not going into the math behind this)
30 million messages / sec => computes pressure of routees
autoscaling

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

[diagram of the hierarchy diff where router has routees as children]

## Exercise

### Once you're done

## Great job!
