# Akka.NET Bootcamp - Lesson 1.0
## Sending Messages between Actors

We're just getting our feet into the water, exploring actors and actor systems. 

In this sample we're not going to worry too much about all of the Akka.NET-specific plumbing. Instead, we're going to start getting ourselves acquainted with the `Actor.Tell` method and explore how [Akka.NET actors](http://akkadotnet.github.io/wiki/Actors "What are actors? - Akka.NET Documentation") pass messages to each other.

### Step 1 - Launching the Fill-in-the-Blank Sample
Go to the [/DoThis/](/DoThis/) folder and open `ActorsSendingMessages.sln` in Visual Studio. The solution consists of a simple console application and only one Visual Studio project file.

### Step 2 - Install the Latest Akka.NET NuGet Package
In the Package Manager Console, type the following command:

    Install-Package Akka

This will install the latest [Akka.NET binaries](https://github.com/akkadotnet/akka.net "Akka.NET on Github"), which you will need in order to compile this sample.

### Step 3 - Have `ConsoleReaderActor` Send a Message to `ConsoleWriterActor`
Within the sample code there are clearly marked **YOU NEED TO FILL THIS IN** - find those regions of code and begin filling them in with the appropriate methods in order to complete your goals.

You will need to do the following:

1. Have `ConsoleReaderActor` send a message to `ConsoleWriterActor` containing the content that it just read from the console.
2. Have `ConsoleReaderActor` send a message to *itself* after sending a message to `ConsoleWriterActor`; this is what keeps the read loop going.
3. Send an initial message to `ConsoleReaderActor` in order to get it to start reading from the console.

You might want to [read the Akka.NET "Hello World" documentation](http://akkadotnet.github.io/wiki/The%20Obligatory%20Hello%20World) for some clues.

### Step 4 - Compile and Run the Sample
Once you've made your edits, press `F5` to compile and run the sample in Visual Studio. 

You should see something like this:

![Petabridge Akka Bootcamp Lesson 1.0 Correct Output](~/Images/correct-console-output.png)

### Once You're Finished 

You can compare your code to what's inside the [/Completed/](/Completed/) folder to see what the instructors included in their samples.
