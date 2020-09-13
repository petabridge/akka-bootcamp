using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";
        private readonly IActorRef _consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            var read = Console.ReadLine();
            do
            {
                _consoleWriterActor.Tell(read);
                read = Console.ReadLine();

            } while (!string.IsNullOrEmpty(read) &&
                     !string.Equals(read, ExitCommand, StringComparison.OrdinalIgnoreCase));

            // shut down the system (acquire handle to system via
            // this actors context)
            Context.System.Terminate();

        }

    }
}