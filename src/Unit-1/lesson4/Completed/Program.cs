using Akka.Actor;

namespace WinTail
{
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // make actor system 
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // create top-level actors within the actor system
            Props consoleWriterProps = Props.Create<ConsoleWriterActor>();
            IActorRef consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");

            Props tailCoordinatorProps = Props.Create(() => new TailCoordinatorActor());
            IActorRef tailCoordinatorActor = MyActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

            Props fileValidatorActorProps = Props.Create(() => new FileValidatorActor(consoleWriterActor, tailCoordinatorActor));
            IActorRef fileValidatorActor = MyActorSystem.ActorOf(fileValidatorActorProps, "validationActor");
            
            Props consoleReaderProps = Props.Create<ConsoleReaderActor>(fileValidatorActor);
            IActorRef consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");

            // begin processing
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }

    }
}
