# Akka.NET Bootcamp - Unit 3: Advanced Akka.NET

![Akka.NET logo](../../images/akka_net_logo.png)

In **[Unit 1](../Unit-1)**, we learned some of the fundamentals of Akka.NET and the actor model.

In **[Unit 2](../Unit-2)** we learned some of the more sophisticated concepts behind Akka.NET, such as pattern matching, basic Akka.NET configuration, scheduled messages, and more!

Here in Unit 3, we'll learn how to make our actor system more scalable, resilient, and parallelized.

## Concepts you'll learn
In Unit 3, you're going to build your own GitHub scraper that can simultaneously retrieve data from multiple GitHub repos at once.

In Unit 3 you will learn:

1. How to use `Group` routers to divide work among your actors
2. How to use `Pool` routers to automatically create and manage pools of actors
3. How to use HOCON to configure your routers
4. How to use `Ask` to wait inline for actors to respond to your messages
5. How to perform work asynchronously inside your actors using `PipeTo`
6. Stateful vs Stateless Routing


## Table of Contents

1. **[Lesson 1:  Using `Group` routers to divide work among your actors](lesson1/)**
2. **[Lesson 2: Using `Pool` routers to automatically create and manage pools of actors](lesson2/)**
3. **[Lesson 3: How to use HOCON to configure your routers](lesson3/)**
4. **[Lesson 4: How to use `Ask` to wait inline for actors to respond to your messages](lesson4/)**
5. **[Lesson 5: How to perform work asynchronously inside your actors using `PipeTo`](lesson5/)**

## Get Started
To get started, [go to the /DoThis/ folder](DoThis/) and open `GithubActors.sln`.

And then go to [Lesson 1](lesson1/).