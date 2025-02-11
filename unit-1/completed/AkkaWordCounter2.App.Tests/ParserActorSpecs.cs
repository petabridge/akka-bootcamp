using Akka.Hosting;
using AkkaWordCounter2.App.Actors;
using AkkaWordCounter2.App.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace AkkaWordCounter2.App.Tests;

public class ParserActorSpecs : Akka.Hosting.TestKit.TestKit
{
    public ParserActorSpecs(ITestOutputHelper output) : base(output: output)
    {
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddHttpClient();
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
        await expectResultsProbe.ExpectMsgAsync<DocumentEvents.WordsFound>(); // should get at least 1 WordsFound
        await expectResultsProbe.FishForMessageAsync(m => m is DocumentEvents.EndOfDocumentReached); // should get EndOfDocumentReached
    }
}