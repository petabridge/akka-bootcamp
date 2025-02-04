namespace AkkaWordCounter2.App;

public class HelloActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private int _helloCounter = 0;
    
    public HelloActor()
    {
        Receive<string>(message =>
        {
           _log.Info("{0} {1}", message, _helloCounter++);
        });
    }
}