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

            // this is here to show you what NOT to do
            // this approach to props has no type safety
            // it will compile, but can easily blow up in your face at runtime :(
            // UNCOMMENT THE BELOW TWO LINES, BUILD THE SOLUTION, AND THEN TRY TO RUN IT TO SEE
            //Props fakeActorProps = Props.Create(typeof(FakeActor));
            //IActorRef fakeActor = MyActorSystem.ActorOf(fakeActorProps, "fakeActor");

            // set up actors, using props (split props onto own line so easier to read)
            Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
            IActorRef consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

            Props validationActorProps = Props.Create(() => new ValidationActor(consoleWriterActor));
            IActorRef validationActor = MyActorSystem.ActorOf(validationActorProps, "validationActor");

            Props consoleReaderProps = Props.Create<ConsoleReaderActor>(validationActor);
            IActorRef consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            await MyActorSystem.WhenTerminated;

            // This prevents the app from exiting
            // before the async work is done
            Console.ReadLine();
        }

        /// <summary>
        /// Fake actor / marker class. Does nothing at all, and not even an actor actually. 
        /// Here to show why you shouldn't use typeof approach to Props.
        /// </summary>
        public class FakeActor { }

    }
}
