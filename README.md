# Akka.NET Bootcamp

Welcome to [Akka.NET](http://getakka.net/ "Akka.NET - Distributed actor model in C# and F#") Bootcamp! This is a free, self-directed learning course brought to you by the folks at [Petabridge](http://petabridge.com/ "Petabridge - Akka.NET Training, Consulting, and Support").

[![Get Akka.NET training material & updates at https://petabridge.com/bootcamp/signup](images/grok.png)](https://petabridge.com/bootcamp/signup)

[![Join the chat at https://gitter.im/petabridge/akka-bootcamp](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/petabridge/akka-bootcamp?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Over the three units of this bootcamp you will learn how to create fully-functional, real-world programs using Akka.NET actors and many other parts of the core Akka.NET framework!

We will start with some basic actors and have you progressively work your way up to larger, more sophisticated examples.

The course is self-directed learning. You can do it at whatever pace you wish. You can [sign up here to have one Akka.NET Bootcamp lesson emailed to you daily](http://learnakka.net/ "Learn Akka.NET with Akka.NET Bootcamp") if you'd like a little help pacing yourself throughout the course.

> NOTE: F# support is in progress (see the [FSharp branch](https://github.com/petabridge/akka-bootcamp/tree/FSharp)). We will happily accept F# pull requests. Feel free to send them in.

## What will you learn?
In Akka.NET Bootcamp you will learn how to use Akka.NET actors to build reactive, concurrent applications.

You will learn how to build types of applications that may have seemed impossible or really, really hard to make prior to learning Akka.NET. You will walk away from this bootcamp with the confidence to handle bigger and harder problems than ever before!

### Unit 1
In Unit 1, we will learn the fundamentals of how the actor model and Akka.NET work.

*NIX systems have the `tail` command built-in to monitor changes to a file (such as tailing log files), whereas Windows does not. We will recreate `tail` for Windows and use the process to learn the fundamentals.

In Unit 1 you will learn:

1. How to create your own `ActorSystem` and actors;
2. How to send messages actors and how to handle different types of messages;
3. How to use `Props` and `IActorRef`s to build loosely coupled systems.
4. How to use actor paths, addresses, and `ActorSelection` to send messages to actors.
5. How to create child actors and actor hierarchies, and how to supervise children with `SupervisionStrategy`.
6. How to use the Actor lifecycle to control actor startup, shutdown, and restart behavior.

**[Begin Unit 1](src/Unit-1/README.md)**.

### Unit 2
In Unit 2, we're going to get into some more of the intermediate Akka.NET features to build a more sophisticated application than what we accomplished at the end of unit 1.

In Unit 2 you will learn:

1. How to use [HOCON configuration](http://getakka.net/articles/concepts/configuration.html#what-is-hocon "Akka.NET HOCON Configurations") to configure your actors via App.config and Web.config;
1. How to configure your actor's [Dispatcher](http://getakka.net/articles/actors/dispatchers.html) to run on the Windows Forms UI thread, so actors can make operations directly on UI elements without needing to change contexts;
1. How to handle more sophisticated types of pattern matching using `ReceiveActor`;
1. How to use the `Scheduler` to send recurring messages to actors;
1. How to use the [Publish-subscribe (pub-sub) pattern](http://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) between actors;
1. How and why to switch actor's behavior at run-time; and
2. How to `Stash` messages for deferred processing.

**[Begin Unit 2](src/Unit-2/README.md)**.

### Unit 3
In Unit 3, we will learn how to use actors for parallelism and scale-out using [Octokit](http://octokit.github.io/) and data from Github repos!

In Unit 3 you will learn:

1. How to perform work asynchronously inside your actors using `PipeTo`;
2. How to use `Ask` to wait inline for actors to respond to your messages;
2. How to use `ReceiveTimeout` to time out replies from other actors;
4. How to use `Group` routers to divide work among your actors;
5. How to use `Pool` routers to automatically create and manage pools of actors; and
6. How to use HOCON to configure your routers.

**[Begin Unit 3](src/Unit-3/README.md)**.

## How to get started

Here's how Akka.NET bootcamp works!

### Use Github to Make Life Easy

This Github repository contains Visual Studio solution files and other assets you will need to complete the bootcamp.

Thus, if you want to follow the bootcamp we recommend doing the following:

1. Sign up for [Github](https://github.com/), if you haven't already.
2. [Fork this repository](https://github.com/petabridge/akka-bootcamp/fork) and clone your fork to your local machine.
3. As you go through the project, keep a web browser tab open to the [Akka.NET Bootcamp ReadMe](https://github.com/petabridge/akka-bootcamp/) so you can read all of the instructions clearly and easily.

### Bootcamp Structure

Akka.NET Bootcamp consists of three modules:

* **Unit 1 - Beginning Akka.NET**
* **Unit 2 - Intermediate Akka.NET**
* **Unit 3 - Advanced Akka.NET**

Each module contains the following structure (using **Unit 1** as an example:)

````
src\Unit1\README.MD - table of contents and instructions for the module
src\Unit1\DoThis\ - contains the .SLN and project files that you will use through all lessons
-- lesson 1
src\Unit1\Lesson1\README.MD - README explaining lesson1
src\Unit1\Lesson1\DoThis\ - C# classes, etc...
src\Unit1\Lesson1\Completed\ - "Expected" output after completing lesson
-- repeat for all lessons
````

Start with the first lesson in each unit and follow the links through their README files on Github. We're going to begin with **[Unit 1, Lesson 1](src/Unit-1/lesson1/README.md)**.

### Lesson Layout
Each Akka.NET Bootcamp lesson contains a README which explains the following:

1. The Akka.NET concepts and tools you will be applying in the lesson, along with links to any relevant documentation or examples
2. Step-by-step instructions on how to modify the .NET project inside the `Unit-[Num]/DoThis/` to match the expected output at the end of each lesson.
3. If you get stuck following the step-by-step instructions, each lesson contains its own `/Completed/` folder that shows the full source code that will produce the expected output. You can compare this against your own code and see what you need to do differently.

#### When you're doing the lessons...

A few things to bear in mind when you're following the step-by-step instructions:

1. **Don't just copy and paste the code shown in the lesson's README**. You'll retain and learn all of the built-in Akka.NET functions if you type out the code as it's shown. 
2. **You might be required to fill in some blanks during individual lessons.** Part of helping you learn Akka.NET involves leaving some parts of the exercise up to you - if you ever feel lost, always check the contents of the `/Completed` folder for that lesson.
3. **Don't be afraid to ask questions**. You can [reach the Petabridge team and other Akka.NET users in our Gitter chat](https://gitter.im/petabridge/akka-bootcamp) here.


## Docs
We will provide explanations of all key concepts throughout each lesson, but of course, you should bookmark (and feel free to use!) the [Akka.NET docs](http://getakka.net/).

## Tools / prerequisites
This course expects the following:

- You have some programming experience and familiarity with C#
- A Github account and basic knowledge of Git.
- You are using a version of Visual Studio ([it's free now!](http://www.visualstudio.com/))
  - We haven't had a chance to test these in Xamarin / on Mono yet, but that will be coming soon. If you try them there, please let us know how it goes! We are planning on having everything on all platforms ASAP.


## Enough talk, let's go!
[Let's begin!](src/Unit-1/lesson1/README.md)


## About Petabridge
![Petabridge logo](images/petabridge_logo.png)

[Petabridge](http://petabridge.com/) is a company dedicated to making it easier for .NET developers to build distributed applications.

**[Petabridge also offers Akka.NET consulting and training](http://petabridge.com/ "Petabridge Akka.NET consulting and training")** - so please [sign up for our mailing list](http://eepurl.com/bSlGWr)!

---
Copyright 2015-2017 Petabridge, LLC
