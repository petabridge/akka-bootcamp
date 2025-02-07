using HtmlAgilityPack;
using static AkkaWordCounter2.App.DocumentCommands;
using static AkkaWordCounter2.App.DocumentEvents;

namespace AkkaWordCounter2.App.Actors;

public sealed class ParserActor : UntypedActor
{
    public const int ChunkSize = 20;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CancellationTokenSource _shutdownCts = new();

    public ParserActor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case ScanDocument document:
            {
                RunTask(async () =>
                {
                    try
                    {
                        var textFeatures = await HandleDocument(document.DocumentId);
                        foreach(var f in textFeatures)
                        {
                            Sender.Tell(new WordsFound(document.DocumentId, f));
                        }
                        Sender.Tell(new EndOfDocumentReached(document.DocumentId));
                    }
                    catch(Exception ex)
                    {
                        _log.Error(ex, "Error processing document {0}", document.DocumentId);
                        Sender.Tell(new DocumentScanFailed(document.DocumentId, ex.Message));
                    }
                });
                break;
            }
        }
    }

    private async Task<IEnumerable<string[]>> HandleDocument(AbsoluteUri uri)
    {
        using var requestToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedToken = CancellationTokenSource
            .CreateLinkedTokenSource(requestToken.Token, 
                _shutdownCts.Token);
        
        using var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(uri.Value, linkedToken.Token);
        var content = await response.Content.ReadAsStringAsync(linkedToken.Token);
        var document = new HtmlDocument();
        document.LoadHtml(content);

        // extract all text features
        var text = TextExtractor.ExtractText(document);
        return text.SelectMany(TextExtractor.ExtractTokens).Chunk(ChunkSize);
    }
}