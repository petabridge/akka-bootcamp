using Akka.Hosting;
using Akka.Routing;
using AkkaWordCounter2.App.Actors;

namespace AkkaWordCounter2.App.Config;

public static class ActorConfigurations
{
    public static AkkaConfigurationBuilder AddWordCounterActor(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry, _) =>
        {
            var props = Props.Create(() => new WordCounterManager());
            var actor = system.ActorOf(props, "wordcounts");
            registry.Register<WordCounterManager>(actor);
        });
    }
    
    public static AkkaConfigurationBuilder AddParserActors(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry, resolver) =>
        {
            // ParserActor has DI'd dependencies
            var props = resolver.Props<ParserActor>()
                // create a round-robin pool of 5
                .WithRouter(new RoundRobinPool(5));
            
            var actor = system.ActorOf(props, "parsers");
            registry.Register<ParserActor>(actor);
        });
    }
    
    public static AkkaConfigurationBuilder AddJobActor(this AkkaConfigurationBuilder builder)
    {
        return builder.WithActors((system, registry, resolver) =>
        {
            var props = resolver.Props<WordCountJobActor>();
            var actor = system.ActorOf(props, "job");
            registry.Register<WordCountJobActor>(actor);
        });
    }
    
    public static AkkaConfigurationBuilder AddApplicationActors(this AkkaConfigurationBuilder builder)
    {
        return builder
            .AddWordCounterActor()
            .AddParserActors()
            .AddJobActor();
    }
}