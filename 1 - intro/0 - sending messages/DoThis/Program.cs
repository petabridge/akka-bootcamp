using System;
using Akka.Actor;

namespace ActorsSendingMessages
{
    #region Actors

    /// <summary>
    /// Actor responsilble for writing TO the console
    /// </summary>
    public class ConsoleWriterActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            var str = message as string; //cast the message back into a string
            if (string.IsNullOrEmpty(str)) //the message we received was not a string!
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("ERROR! Did not receive valid string from ConsoleReaderActor!");
                return;
            }

            //set foreground color to red if length of the str is even numbered, green if odd
            Console.ForegroundColor = str.Length % 2 == 0 ? ConsoleColor.DarkRed : ConsoleColor.Green;
            Console.WriteLine(str);
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Actor responsible for reading FROM the console
    /// </summary>
    public class ConsoleReaderActor : UntypedActor
    {
        private readonly ActorRef _consoleWriterActor;

        /// <summary>
        /// The ConsoleReaderActor must have an <see cref="ActorRef"/> reference to the
        /// <see cref="ConsoleWriterActor"/> in order to do its job.
        /// </summary>
        /// <param name="consoleWriterActor">an <see cref="ActorRef"/> to a pre-created <see cref="ConsoleWriterActor"/> instance</param>
        public ConsoleReaderActor(ActorRef consoleWriterActor)
        {
            _consoleWriterActor = consoleWriterActor;
        }

        public const string EscapeString = "exit";

        protected override void OnReceive(object message)
        {
            var read = Console.ReadLine();

            //see if the user typed "exit"
            if (!string.IsNullOrEmpty(read) &&
                read.ToLowerInvariant().Equals(EscapeString))
            {
                Console.WriteLine("Exiting!");
                // shut down the entire actor system via the ActorContext
                // causes MyActorSystem.AwaitTermination(); to stop blocking the current thread
                // and allows the application to exit.
                Context.System.Shutdown();
                return;
            }

            //tell the ConsoleWriterActor what we just read from the console
            #region YOU NEED TO FILL THIS IN
            #endregion

            //tell ourself to "READ FROM CONSOLE AGAIN"
            #region YOU NEED TO FILL THIS IN
            #endregion
        }
    }

    #endregion

    #region Program

    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            MyActorSystem = ActorSystem.Create("MyFirstActorSystem");
            PrintInstructions();

            //Create some actors!
            ActorRef consoleWriter = MyActorSystem.ActorOf(Props.Create<ConsoleWriterActor>(), "consoleWriter");
            ActorRef consoleReader = MyActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(consoleWriter)), "consoleReader");

            //Tell the console reader to "START"
            #region YOU NEED TO FILL THIS IN
            #endregion

            // This blocks the current thread from exiting until MyActorSystem is shut down
            // The ConsoleReaderActor will shut down the ActorSystem once it receives an 
            // "exit" command from the user
            MyActorSystem.AwaitTermination();
        }

        /// <summary>
        /// Prints intructions on the console - no need to worry about this
        /// </summary>
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
            Console.WriteLine("Type 'exit' to quit this application at any time.");
        }
    }

    #endregion
}
