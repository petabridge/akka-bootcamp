using System;
﻿using Akka.Actor;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            var consoleWriterActor = MyActorSystem.ActorOf(Props.Create(() => new ConsoleWriterActor()));
            var consoleReaderActor = MyActorSystem.ActorOf(Props.Create(() => new ConsoleReaderActor(consoleWriterActor)));

            // tell console reader to begin
            consoleWriterActor.Tell(null);
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);
            
            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.AwaitTermination();
        }
    }
    #endregion
}
