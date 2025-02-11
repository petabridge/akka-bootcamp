using System.Collections.Immutable;
using static AkkaWordCounter2.App.DocumentEvents;
using static AkkaWordCounter2.App.DocumentQueries;
namespace AkkaWordCounter2.App.Actors;

public sealed class DocumentWordCounter : UntypedActor
{
    private readonly AbsoluteUri _documentId;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    
    private readonly Dictionary<string, int> _wordCounts = new();
    private readonly HashSet<IActorRef> _subscribers = new();

    public DocumentWordCounter(AbsoluteUri documentId)
    {
        _documentId = documentId;
    }
    
    // Our default behavior when we're running
    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case WordsFound wordsFound when wordsFound.DocumentId == _documentId:
                _log.Debug("Found {0} words in document {1}", wordsFound.Tokens.Count, _documentId);
                foreach (var word in wordsFound.Tokens)
                {
                    if (!_wordCounts.TryAdd(word, 1))
                    {
                        _wordCounts[word]++;
                    }
                }
                break;
            case FetchCounts subscribe when subscribe.DocumentId == _documentId:
                _subscribers.Add(Sender);
                break;
            case EndOfDocumentReached endOfDocumentReached when endOfDocumentReached.DocumentId == _documentId:
                var output = new CountsTabulatedForDocument(_documentId, _wordCounts.ToImmutableDictionary(x => x.Key, x => x.Value));
                foreach (var subscriber in _subscribers)
                {
                    subscriber.Tell(output);
                }
                _subscribers.Clear();
                
                // processing is done - change behaviors
                Become(Complete);
                break;
            case IWithDocumentId withDocumentId when withDocumentId.DocumentId != _documentId:
                _log.Warning("Received message for document {0} but I am responsible for document {1}", withDocumentId.DocumentId, _documentId);
                break;
            case ReceiveTimeout:
                _log.Warning("Document {0} timed out", _documentId);
                Context.Stop(Self);
                break;
            default:
                Unhandled(message);
                break;
        }
    }

    private void Complete(object message)
    {
        switch (message)
        {
            case FetchCounts:
                Sender.Tell(new CountsTabulatedForDocument(_documentId, _wordCounts.ToImmutableDictionary(x => x.Key, x => x.Value)));
                break;
            case IWithDocumentId withDocumentId when withDocumentId.DocumentId == _documentId:
                _log.Warning("Received message for document {0} but I have already completed processing", withDocumentId.DocumentId);
                break;
            case IWithDocumentId withDocumentId when withDocumentId.DocumentId != _documentId:
                _log.Warning("Received message for document {0} but I am responsible for document {1}", withDocumentId.DocumentId, _documentId);
                break;
            case ReceiveTimeout:
                // no need for warning here
                Context.Stop(Self);
                break;
            default:
                Unhandled(message);
                break;
        }
    }

    protected override void PreStart()
    {
        SetReceiveTimeout(TimeSpan.FromMinutes(2));
    }
}