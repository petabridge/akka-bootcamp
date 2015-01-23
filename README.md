# akka-bootcamp
## Akka.NET from Soup to Nuts code samples and tutorials

This bootcamp will teach you all the basics of Akka.NET and actor systems. You will get a blend of conceptual and practical guidance throughout, and be coding at each step along the way. 

### Fundamental concept: what is an actor?
What is an actor? The full docs are [here](https://akkadotnet.github.io/wiki/Actors), but for now, just think of an actor as an object that does whatever you tell it to. Every actor is made of these parts:

- some internal state
- a mailbox
- a supervisor, and
- optionally, child actor(s)

All such actors live within an encapsulating actor system. 

***What are the most important things to know about my actors?***

1. **You never talk to an actor directly.**

	When you make an actor in the system, you don't get to talk directly to that actor. 

	You talk to the actor via the actor system, which manages the communication between your program and the actor. What you actually get back when you make an actor is a reference to, or proxy for that actor. 

	This proxy you do get to talk to is called an `ActorRef`. It's a channel to talk to the actual actor via the surrounding actor system.

1. **Actors are very private. (At least, they should be.)**

	The rest of the system can't see into an actor and know its internal state.
	
	If you need to know what the state of an actor is, you need to ask that actor. That is, unless the actor wears its heart on its sleeve and the outside world can see everything going on inside (not recommended, for reasons we'll go over later).

1. **Actors are nomads.**

	You can pass an `ActorRef` object around freely without restriction in your program. You don't actually know or care where the actor lives in the system, just that the actor exists. This is helpful because fill in here.

1. **Actors are chatty, but they only talk by sending messages. (Kind of like email.)**

	Just because they're nomadic doesn't mean actors don't talk. Actors are constantly sending messages back and forth to each other.

	*The key thing to know is that all communication within an actor system happens by sending messages to the mailboxes of actors*. 

	It's kind of like email: you can send someone a message, but it's up to them to handle the message and decide  whether or not to respond. Once messages are delivered by the system, it's up to each actor to process their own messages.

	Fun fact: even actors don't get to talk directly to each other. They use `ActorRef`s too.

1. **Actors are hierarchical.**

	*All* actors have a supervisor. And every actor can supervise other actors, but doesn't have to. We'll talk about this more later.
	


## [Now, go dive in!](1 - intro/0 - sending messages/)
