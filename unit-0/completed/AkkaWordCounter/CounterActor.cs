using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;

namespace AkkaWordCounter;

using static CounterQueries;
using static CounterCommands; // make message handlers less verbose

public sealed class CounterActor : UntypedActor{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Dictionary<string, int> _tokenCounts = new();
    private bool _doneCounting = false;

    // for actors who sent us a FetchCounts before we were done counting
    private readonly HashSet<IActorRef> _subscribers = new();

    protected override void OnReceive(object message){
        switch(message){
            case CountTokens tokens:
            {
                foreach(var t in tokens.Tokens){
                    if(!_tokenCounts.TryAdd(t, 1)){
                        _tokenCounts[t] += 1;
                    }
                }
                break;
            }
            case ExpectNoMoreTokens:
            {
                _doneCounting = true;

                _log.Info("Completed counting tokens - found [{0}] unique tokens", _tokenCounts.Count);

                // ensure the output is immutable
                // cheaper to do this once at the end versus every time we count
                var totals = _tokenCounts.ToImmutableDictionary();
                foreach(var s in _subscribers)
                {
                    s.Tell(totals);
                }

                // don't need to track subscribers anymore
                _subscribers.Clear();
                break;
            }
            case FetchCounts fetchCounts when _doneCounting:
            {
                // instantly reply with the results
                fetchCounts.Subscriber.Tell(_tokenCounts.ToImmutableDictionary());
                break;
            }
            case FetchCounts fetch:
            {
                _subscribers.Add(fetch.Subscriber);
                break;
            }
            default:
                Unhandled(message);
                break;
        }
    }
}