# Akka.NET Bootcamp - Unit 2: Intermediate Akka.NET

In **[Unit 1](../Unit-1/README.md)**, we learned some of the fundamentals of Akka.NET and the Actor Model.

In Unit 2 we will learn some of the more sophisticated concepts behind Akka.NET, such as pattern matching, basic Akka.NET configuration, scheduled messages, and much more!

## Concepts you'll learn

In Unit 2 you're going to build your own version of Resource Monitor using Windows Forms, data visualization tools built into .NET, and [Performance Counters](https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.aspx "PerformanceCounter Class - C#").

In fact, here's what the final output from lesson 5 looks like:

![Akka.NET Bootcamp Unit 2 Output](lesson5/images/syncharting-complete-output.gif)

> NOTE: If you're following along using the eBook / .ePub, you won't see the animation. [Click here to see it](https://github.com/petabridge/akka-bootcamp/blob/master/src/Unit-2/lesson5/images/syncharting-complete-output.gif).

**You're going to build this whole thing using actors**, and you'll be surprised at how small your code footprint is when we're done.

In Unit 2 you will learn:

1. How to use [HOCON configuration](https://getakka.net/articles/concepts/configuration.html#what-is-hocon "Akka.NET HOCON Configurations") to configure your actors via App.config and Web.config;
1. How to configure your actor's [Dispatcher](https://getakka.net/articles/actors/dispatchers.html) to run on the Windows Forms UI thread, so actors can make operations directly on UI elements without needing to change contexts;
1. How to handle more sophisticated types of pattern matching using `ReceiveActor`;
1. How to use the `Scheduler` to send recurring messages to actors;
1. How to use the [Publish-subscribe (pub-sub) pattern](http://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) between actors;
1. How and why to switch actor's behavior at run-time; and
2. How to `Stash` messages for deferred processing.

## Table of Contents

1. **[Lesson 1: `Config` and Deploying Actors via App.Config](lesson1/README.md)**
2. **[Lesson 2: Using `ReceiveActor` for Better Message Handling](lesson2/README.md)**
3. **[Lesson 3: Using the `Scheduler` to Send Recurring Messages](lesson3/README.md)**
4. **[Lesson 4: Switching Actor Behavior at Run-time with `BecomeStacked` and `UnbecomeStacked`](lesson4/README.md)**
5. **[Lesson 5: Using a `Stash` to Defer Processing of Messages](lesson5/README.md)**

## Get Started

To get started, [go to the /DoThis/ folder](DoThis/) and open `SystemCharting.sln`.

And then go to [Lesson 1](lesson1/README.md).
