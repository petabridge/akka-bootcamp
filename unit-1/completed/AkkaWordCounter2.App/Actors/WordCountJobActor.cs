using System.Collections.Immutable;
using Akka.Hosting;
using static AkkaWordCounter2.App.DocumentCommands;
using static AkkaWordCounter2.App.DocumentQueries;
using static AkkaWordCounter2.App.CollectionUtilities;

namespace AkkaWordCounter2.App.Actors;

/// <summary>
/// Responsible for processing a batch of documents.
/// </summary>
public sealed class WordCountJobActor : UntypedActor, IWithStash, IWithTimers
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IRequiredActor<WordCounterManager> _wordCounterManager;
    private readonly IRequiredActor<ParserActor> _parserActor;
    
    public IStash Stash { get; set; } = null!;
    public ITimerScheduler Timers { get; set; } = null!;
    
    private readonly HashSet<IActorRef> _subscribers = new();
    private readonly Dictionary<AbsoluteUri, ProcessingStatus> _documentsToProcess = new();
    private readonly Dictionary<AbsoluteUri, ImmutableDictionary<string, int>> _wordCounts = new();
    
    public enum ProcessingStatus
    {
        Processing = 0,
        Completed = 1,
        FailedError = 2,
        FailedTimeout = 3
    }

    public sealed class JobTimeout
    {
        public static readonly JobTimeout Instance = new();
        private JobTimeout(){ }
    }
    
    public WordCountJobActor(
        IRequiredActor<WordCounterManager> wordCounterManager,
        IRequiredActor<ParserActor> parserActor)
    {
        _wordCounterManager = wordCounterManager;
        _parserActor = parserActor;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case ScanDocuments scan:
                _log.Info("Received scan request for {0}", scan.DocumentIds.Count);
                foreach (var document in scan.DocumentIds)
                {
                    _documentsToProcess[document] = ProcessingStatus.Processing;

                    // begin processing
                    _parserActor.ActorRef.Tell(new ScanDocument(document));

                    // get back to us once processing is completed
                    _wordCounterManager.ActorRef.Tell(new FetchCounts(document));
                }

                Become(Running);
                Timers.StartSingleTimer("job-timeout", JobTimeout.Instance, TimeSpan.FromSeconds(30));
                Stash.UnstashAll();
                break;
            default:
            {
                // buffer any other messages until the job starts
                Stash.Stash();
                break;
            }
        }
    }

    private void Running(object message)
    {
        switch (message)
        {
            case DocumentEvents.WordsFound found:
                _wordCounterManager.ActorRef.Forward(found);
                break;
            case DocumentEvents.EndOfDocumentReached eof:
                _wordCounterManager.ActorRef.Forward(eof);
                break;
            case DocumentEvents.CountsTabulatedForDocument counts:
                _log.Info("Received word counts for {0}", counts.DocumentId);
                _wordCounts[counts.DocumentId] = counts.WordFrequencies;
                _documentsToProcess[counts.DocumentId] = ProcessingStatus.Completed;
                HandleJobCompletedMaybe();
                break;
            case DocumentEvents.DocumentScanFailed failed:
                _log.Error("Document scan failed for {0}: {1}", failed.DocumentId, failed.Reason);
                _documentsToProcess[failed.DocumentId] = ProcessingStatus.FailedError;
                HandleJobCompletedMaybe();
                break;
            case JobTimeout _:
                _log.Error("Job timed out");
                
                // Set all documents that haven't been processed yet to timed out
                foreach (var (document, status) in _documentsToProcess)
                {
                    if (status == ProcessingStatus.Processing)
                    {
                        _documentsToProcess[document] = ProcessingStatus.FailedTimeout;
                    }
                }
                
                HandleJobCompletedMaybe(true);
                break;
            case SubscribeToAllCounts:
                _subscribers.Add(Sender);
                break;
            default:
                Unhandled(message);
                break;
        }
    }

    private void HandleJobCompletedMaybe(bool force = false)
    {
        if (!IsJobCompleted() && !force) return;
        
        // log statuses of each page
        foreach (var (document, status) in _documentsToProcess)
        {
            _log.Info("Document {0} status: {1}, total words: {2}", document, status,
                _wordCounts[document].Values.Sum());
        }
            
        // need to merge all the word counts
        var mergedCounts = MergeWordCounts(_wordCounts.Values);
        var finalOutput =
            new DocumentEvents.
                CountsTabulatedForDocuments(_documentsToProcess.Keys.ToList(), mergedCounts);
            
        foreach (var subscriber in _subscribers)
        {
            subscriber.Tell(finalOutput);
        }

        Context.Stop(Self);
    }

    private bool IsJobCompleted()
    {
        return _documentsToProcess.Values.All(x => x > ProcessingStatus.Processing);
    }
}