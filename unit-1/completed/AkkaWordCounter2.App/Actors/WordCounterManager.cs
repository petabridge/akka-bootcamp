using System.Web;

namespace AkkaWordCounter2.App.Actors;

// Parent actor that manages the lifecycle of DocumentWordCounter actors
public sealed class WordCounterManager : ReceiveActor
{
    public WordCounterManager()
    {
        Receive<IWithDocumentId>(s =>
        {
            string childName = $"word-counter-{HttpUtility.UrlEncode(s.DocumentId.ToString())}";
            IActorRef child = Context.Child(childName);
            if (child.IsNobody())
            {
                // start the child if it doesn't exist
                child = Context.ActorOf(Props.Create(() => new DocumentWordCounter(s.DocumentId)), childName);
            }
            child.Forward(s);
        });
    }
}