using Akka.Hosting;

namespace AkkaWordCounter2.App;

public class TimerActor : ReceiveActor, IWithTimers
{
    private readonly IActorRef _helloActor;

    public TimerActor(IRequiredActor<HelloActor> helloActor)
    {
        _helloActor = helloActor.ActorRef;
        Receive<string>(message =>
        {
            _helloActor.Tell(message);
        });
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("hello-key", "hello", TimeSpan.FromSeconds(1));
    }

    public ITimerScheduler Timers { get; set; } = null!; // gets set by Akka.NET
}