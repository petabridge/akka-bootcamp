using Akka.Actor;
using Akka.Event;
using AkkaWordCounter;

ActorSystem myActorSystem = ActorSystem.Create("LocalSystem");
myActorSystem.Log.Info("Hello from the ActorSystem");

// Props == formula for creating an actor
Props myProps = Props.Create<HelloActor>();

// IActorRef == handle for messaging an actor
// Survives actor restarts, is serializable
IActorRef myActor = myActorSystem.ActorOf(myProps, "MyActor");

// tell my actor to display a message via Fire-and-Forget messaging
myActor.Tell("Hello, World!");

// use Ask<T> to do request-response messaging
string whatsUp = await myActor.Ask<string>("What's up?");
Console.WriteLine(whatsUp);

var counterActor = myActorSystem.ActorOf(Props.Create<CounterActor>(), 
    "CounterActor");
var parserActor = myActorSystem.ActorOf(Props.Create(() => new ParserActor(counterActor)), 
    "ParserActor");

Task<IDictionary<string, int>> completionPromise = counterActor
    .Ask<IDictionary<string, int>>(@ref => new CounterQueries.FetchCounts(@ref), null, 
    CancellationToken.None);

parserActor.Tell(new DocumentCommands.ProcessDocument(
"""
        This is a test of the Akka.NET Word Counter.
        I would go
        """
    ));

IDictionary<string, int> counts = await completionPromise;
foreach(var kvp in counts)
{
    // going to use string interpolation here because we don't care about perf
    myActorSystem.Log.Info($"{kvp.Key}: {kvp.Value} instances");
}

await myActorSystem.Terminate();