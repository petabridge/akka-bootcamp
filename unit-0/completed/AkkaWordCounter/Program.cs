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

await myActorSystem.Terminate();