using System;
using Akka.Actor;
using System.Threading.Tasks;

namespace WinTail
{
    #region Program

    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            AsyncMain().Wait();
        }

        private static async Task AsyncMain()
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
            await MyActorSystem.WhenTerminated;

            // This prevents the app from exiting
            // before the async work is done
            Console.ReadLine();
        }

    }
    #endregion
}
