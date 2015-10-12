# Akka.NET Bootcamp - Unit 2: Intermediate Akka.NET

![Akka.NET logo](../../images/akka_net_logo.png)

In **[Unit 1](../../FSharp/Unit-1)**, we learned some of the fundamentals of Akka.NET and the actor model.

In Unit 2 we will learn some of the more sophisticated concepts behind Akka.NET, such as pattern matching, basic Akka.NET configuration, scheduled messages, and more!

## Concepts you'll learn

In Unit 2 you're going to build your own version of Resource Monitor using Windows Forms, some of the data visualization tools built into .NET, and [Performance Counters](https://msdn.microsoft.com/en-us/library/system.diagnostics.performancecounter.aspx?cs-save-lang=1&cs-lang=fsharp "PerformanceCounter Class - F#").

In fact, here's what the final output from lesson 4 looks like:

![Akka.NET Bootcamp Unit 2 Output](lesson4/images/syncharting-complete-output.gif)

**You're going to build this whole thing using actors**, and you'll be surprised at how small your code footprint is when we're finished.

In Unit 2 you will learn:

1. How to use [HOCON configuration](http://getakka.net/wiki/Configuration "Akka.NET HOCON Configurations") to configure your actors via App.config and Web.config;
2. How to configure your actor's [Dispatcher](http://getakka.net/wiki/Dispatchers) to run on the Windows Forms UI thread, so actors can make operations directly on UI elements without needing to change contexts;
3. How to use the `Scheduler` to send recurring messages to actors;
4. How to use the [Publish-subscribe (pub-sub) pattern](http://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) between actors;
5. How and why to switch actor's behavior at run-time; and
6. How to `Stash` messages for deferred processing.

## Table of Contents

1. **[Lesson 1: `Config` and Deploying Actors via App.Config](lesson1/)**
2. **[Lesson 2: Using the `Scheduler` to Send Recurring Messages](lesson2/)**
3. **[Lesson 3: Switching Actor Behavior at Run-time](lesson3/)**
4. **[Lesson 4: Using a `Stash` to Defer Processing of Messages](lesson4/)**

## Get Started

To get started, [go to the /DoThis/ folder](DoThis/) and open `SystemCharting.sln`.

And then go to [Lesson 1](lesson1/).
