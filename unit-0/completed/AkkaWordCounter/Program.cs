using Akka.Actor;
using Akka.Event;

ActorSystem myActorSystem = ActorSystem.Create("LocalSystem");
myActorSystem.Log.Info("Hello from the ActorSystem");
await myActorSystem.Terminate();