using Akka.Actor;
using Akka.TestKit.Xunit2;
using AkkaWordCounter2.App.Actors;
using Xunit.Abstractions;

namespace AkkaWordCounter2.App.Tests;

public class DocumentWordCounterSpecs : TestKit
{
    public static readonly Akka.Configuration.Config Config = "akka.loglevel=DEBUG";
    
    public DocumentWordCounterSpecs(ITestOutputHelper output) : base(output: output, config: Config)
    {
        
    }
    
    public static readonly AbsoluteUri TestDocumentUri = new(new Uri("http://example.com/test"));

    [Fact]
    public async Task ShouldProcessWordCountsCorrectly()
    {
        // arrange
        var props = Props.Create(() => new DocumentWordCounter(TestDocumentUri));
        var actor = Sys.ActorOf(props);

        IReadOnlyList<IWithDocumentId> messages = [
            new DocumentEvents.WordsFound(TestDocumentUri, ["hello", "world"]),
            new DocumentEvents.WordsFound(TestDocumentUri, ["bar", "foo"]),
            new DocumentEvents.WordsFound(TestDocumentUri, ["HeLlo", "wOrld"]),
            new DocumentEvents.EndOfDocumentReached(TestDocumentUri)
        ];
        
        // have the TestActor subscribe to updates
        actor.Tell(new DocumentQueries.FetchCounts(TestDocumentUri), TestActor);
        
        // act
        foreach (var message in messages)
        {
            actor.Tell(message);
        }
        
        // assert
        var response = await ExpectMsgAsync<DocumentEvents.CountsTabulatedForDocument>();
        Assert.Equal(6, response.WordFrequencies.Count); // words are case sensitive
    }
}