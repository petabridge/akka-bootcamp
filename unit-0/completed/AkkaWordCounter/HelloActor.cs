using Akka.Actor;
using Akka.Event;

namespace AkkaWordCounter;

// basic actor that just logs whatever it receives and replies back
public class HelloActor : UntypedActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case string msg:
                _log.Info("Received message: {0}", msg);
                Sender.Tell($"{msg} reply");
                break;
            default:
                Unhandled(message);
                break;
        }
    }
}