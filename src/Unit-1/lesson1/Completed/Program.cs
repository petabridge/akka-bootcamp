using System;
using Akka.Actor;
using System.Threading.Tasks;

namespace WinTail
{
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

            PrintInstructions();

            // make our first actors!
            IActorRef consoleWriterActor = MyActorSystem.ActorOf(Props.Create(() => new ConsoleWriterActor()),
                "consoleWriterActor");
            IActorRef consoleReaderActor =
                MyActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(consoleWriterActor)),
                    "consoleReaderActor");

            // tell console reader to begin
            consoleReaderActor.Tell("start");

            // blocks the main thread from exiting until the actor system is shut down
            //MyActorSystem.AwaitTermination();
            await MyActorSystem.WhenTerminated;

            // This prevents the app from exiting
            // before the async work is done
            Console.ReadLine();
        }

        private static void PrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.Write("Some lines will appear as");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(" red ");
            Console.ResetColor();
            Console.Write(" and others will appear as");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" green! ");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }
    }
}
