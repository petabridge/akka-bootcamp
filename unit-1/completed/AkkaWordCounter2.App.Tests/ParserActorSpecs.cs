using Akka.Hosting;
using AkkaWordCounter2.App.Actors;
using AkkaWordCounter2.App.Config;
using Xunit.Abstractions;

namespace AkkaWordCounter2.App.Tests;

public class ParserActorSpecs : Akka.Hosting.TestKit.TestKit
{
    public ParserActorSpecs(ITestOutputHelper output) : base(output: output)
    {
    }
    
    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder
            .ConfigureLoggers(configBuilder =>
            {
                configBuilder.LogLevel = Akka.Event.LogLevel.DebugLevel;
            })
            .AddParserActors();
    }
    
    public static readonly AbsoluteUri ParserActorUri = new(new Uri("https://getakka.net/"));
    
    [Fact]
    public async Task ShouldParseWords()
    {
        // arrange
        var parserActor = await ActorRegistry.GetAsync<ParserActor>();
        var expectResultsProbe = CreateTestProbe();
        
        // act
        parserActor.Tell(new DocumentCommands.ScanDocument(ParserActorUri), expectResultsProbe);
        
        // assert
        ExpectMsg<WordCountsMessage>(msg =>
        {
            Assert.Equal(2, msg.WordCounts["hello"]);
            Assert.Equal(1, msg.WordCounts["world"]);
        });
    }
}