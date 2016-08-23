using System;
using Akka.Actor;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // make an actor system 
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // make our first actors!
            IActorRef consoleWriterActor = MyActorSystem.ActorOf(Props.Create(() => new ConsoleWriterActor()),
                "consoleWriterActor");
            IActorRef consoleReaderActor =
                MyActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(consoleWriterActor)),
                    "consoleReaderActor");

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }

    }
    #endregion
}
